using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using VPay.MassTransit.Abstractions;
using VPay.Optum.PaymentRouting.Common.Serialization;
using VPay.Optum.PaymentRouting.InboundPayment.Consumers.Consumers.PaymentRecord;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests.InboundHeaders.Queries
{
    public class PaymentRecordConsumerFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public PaymentRecordConsumerFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<PaymentRecordConsumer>()
                    .AddMock<ILogger<PaymentRecordConsumer>>()
                    .AddMock<IMediator>()
                    .AddMock<BlobServiceClient>()
                    .AddMock<StorageConfig>();
            });

        [Theory]
        [InlineData(null, "https://www.google.com/1/2/3", "")]
        [InlineData(null, "https://www.google.com", "abc")]
        [InlineData("null", "https://www.google.com/1/2/3", "")]
        [InlineData("null", "https://www.google.com/1/2/3", "abc")]
        [InlineData("ST*123~", null, "")]
        [InlineData("ST*123~", null, "abc")]
        [InlineData("ST*123~", "null", "")]
        [InlineData("ST*123~", "null", "abc")]
        public async Task Should_NotThrowError_When_ValidInputIsGiven(string paymentClob, string blobStorageUri, string sharedAccessSignature)
        {
            var paymentRecord = new PaymentRecord
            {
                BatchId = "1",
                BlobStorageUri = blobStorageUri,
                FileProcId = "2",
                Payer835Id = "1",
                PayerAlias = "abc",
                PayerSubpayerId = "bcd",
                PaymentClob = paymentClob,
                ProviderTin = "123456789",
                TransactionType = "EOF",
                UnconsolidatedPaymentAmount = "12.00",
                UnconsolidatedPaymentNbr = "1233311",
                ZipCode = "75252"
            };

            var storageConfig = _container.Get<StorageConfig>();
            storageConfig.SharedAccessSignature = sharedAccessSignature;
            storageConfig.BlobContainerName = "aabbccdd";

            var consumer = _container.Get<PaymentRecordConsumer>();
            var blobClient = _container.Mock<BlobClient>();
            blobClient.DownloadToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(_container.Mock<Azure.Response>());
            var blobStorageClient = _container.Get<BlobServiceClient>();
            var blobContainerClient = _container.Mock<BlobContainerClient>();
            blobStorageClient.GetBlobContainerClient(Arg.Any<string>()).Returns(blobContainerClient);
            blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
            PaymentRecordConsumer.NewBlobClient = (Uri url) => blobClient;
            Func<Task> act = () => consumer.Consume(paymentRecord, CancellationToken.None);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConsumeWithNoPaymentMessage_ThrowsException()
        {
            var provider = new ServiceCollection()
                .AddLogging(l => l.AddProvider(NullLoggerProvider.Instance))
                .AddMassTransitInMemoryTestHarness(cfg =>
                {
                    cfg.AddConsumer<PaymentRecordConsumer>();
                    cfg.AddConsumerTestHarness<PaymentRecordConsumer>();
                })
                .BuildServiceProvider(true);
            var harness = provider.GetRequiredService<InMemoryTestHarness>();
            try
            {
                await harness.Start();
                var bus = provider.GetRequiredService<IBus>();

                IRequestClient<PaymentRecordConsumer> client = bus.CreateRequestClient<PaymentRecordConsumer>();

                Func<Task> act = () => client.GetResponse<PaymentRecordConsumer>(null);

                await act.Should().ThrowAsync<RequestException>();
            }
            finally
            {
                await harness.Stop();
                await provider.DisposeAsync();
            }
        }
    }
}
