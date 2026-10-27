using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VPay.Optum.Http.ConfigApi;
using VPay.Optum.Http.ConfigApi.Models;
using VPay.Optum.PaymentRouting.ApProcessor.Constants;
using VPay.Optum.PaymentRouting.ApProcessor.Consumers;
using VPay.Optum.PaymentRouting.ApProcessor.Services;
using VPay.Optum.PaymentRouting.Common.RabbitMessages;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.ApProcessor.Tests
{
    public class ApReapVPayToOptumConverterFacts
    {
        private readonly PaymentRoutingTestingContainer _container;
        private readonly ApReapVPayToOptumConverter _converter;

        private readonly Mock<IConfigApi> _configApi = new();

        public ApReapVPayToOptumConverterFacts()
        {
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddMock<ILogger<ConsumeApMessage.Handler>>()
                   .AddSingleton<ConsumeApMessage.Handler>();
            });

            _configApi.Setup(m => m.GetPrsMapping(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PrsMappingDto { OptumPayorId = "", OptumSubPayorId = "" });

            _converter = new(_configApi.Object);
        }

        [Theory]
        [InlineData("CLEFT", "ACH")]
        [InlineData("CLSDA", "ACH")]
        [InlineData("OPACH", "ACH")]
        [InlineData("CLCFT", "ACH")]
        [InlineData("CLCHK", "CHK")]
        [InlineData("CLNPF", "NON")]
        [InlineData("CLVCP", "VCP")]
        [InlineData("CLCCK", "")]
        [InlineData(null, "")]
        public async Task ShouldPopulateRevisedMethodCodeForReapPaymentType(string paymentMethodCode, string expectedRevisedMethodCode)
        {
            var message = _container.Fake<ApReapMessage>(x => x
                .With(y => y.TrnSegment, "1*2*3*4*5")
                .With(y => y.ReapPaymentType, paymentMethodCode)
                .With(y => y.MessageType, MessageTypes.Reap));

            var convertedMessage = await _converter.ConvertApReapMessage(message);

            convertedMessage.Should().NotBeNull();
            convertedMessage.revisedMethodCode.Should().Be(expectedRevisedMethodCode);
        }

        [Theory]
        [InlineData("CLEFT", "ACH")]
        [InlineData("CLSDA", "ACH")]
        [InlineData("OPACH", "ACH")]
        [InlineData("CLCFT", "ACH")]
        [InlineData("CLCHK", "CHK")]
        [InlineData("CLNPF", "NON")]
        [InlineData("CLVCP", "VCP")]
        [InlineData("CLCCK", "")]
        [InlineData(null, "")]
        public void PayMethodCodeConverter_ShouldConvertCorrectly(string paymentType, string expectedValue)
        {
            var actualValue = ApReapVPayToOptumConverter.PayMethodCodeConverter(paymentType);
            actualValue.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("CHK", "EXP")]
        [InlineData("VCP", "EXP")]
        [InlineData("ACH", "R01")]
        [InlineData("ABC", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ReturnReasonCodeConverter_ShouldConvertCorrectly(string paymentMethodCode, string expectedValue)
        {
            var actualValue = ApReapVPayToOptumConverter.ReturnReasonCodeConverter(paymentMethodCode);
            actualValue.Should().Be(expectedValue);
        }

        [Theory]
        [InlineData("AP", "AP")]
        [InlineData("RP", "RP")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("ABC", "")]
        public void PaymentTypeConverter_ShouldConvertCorrectly(string messageType, string expectedValue)
        {
            var actualValue = ApReapVPayToOptumConverter.PaymentTypeConverter(messageType);
            actualValue.Should().Be(expectedValue);
        }
    }
}
