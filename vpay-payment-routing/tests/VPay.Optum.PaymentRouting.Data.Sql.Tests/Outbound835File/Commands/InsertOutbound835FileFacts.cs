using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using VPay.Optum.PaymentRouting.Data.Abstractions;
using VPay.Optum.PaymentRouting.Data.Sql.Helpers;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Commands;
using VPay.Optum.PaymentRouting.TestsCommon.Containers;
using VPay.Optum.PaymentRouting.TestsCommon.Extensions;
using Xunit;

namespace VPay.Optum.PaymentRouting.Data.Sql.Tests
{
    public class InsertOutbound835FileFacts
    {
        private readonly PaymentRoutingTestingContainer _container;

        public InsertOutbound835FileFacts() =>
            _container = new PaymentRoutingTestingContainer(services =>
            {
                services.AddSingleton<InsertOutbound835File.Handler>()
                    .AddMock<ILogger<InsertOutbound835File.Handler>>()
                    .AddMock<IDataConnection<SqlConnection>>();
            });

        [Fact]
        public async Task Should_NotThrowError_When_ValidInputIsGiven()
        {
            var command = new InsertOutbound835File.Command(_container.Fake<string>(), _container.Fake<string>(), _container.Fake<string>(), _container.FakeMany<long>());
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            var expectedCommand = new
            {
                command.PayerAlias,
                command.OutboundRefEo,
                command.CreatedBy,
                Payments = command.paymentIds.ToIdDataTable().AsTableValuedParameter("IdTable")
            };

            connection.GetFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_Outbound835File_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<object>(_ => JsonConvert.SerializeObject(_) == JsonConvert.SerializeObject(expectedCommand)), Arg.Any<CancellationToken>())
                .Returns(1);

            var handler = _container.Get<InsertOutbound835File.Handler>();
            Func<Task<long>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().NotThrowAsync();

            await connection.Received().GetFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_Outbound835File_Insert", StringComparison.CurrentCultureIgnoreCase)),
               Arg.Is<object>(_ => JsonConvert.SerializeObject(_) == JsonConvert.SerializeObject(expectedCommand)), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_ThrowErrorIfSaveFailsWithException()
        {
            var command = new InsertOutbound835File.Command(_container.Fake<string>(), _container.Fake<string>(), _container.Fake<string>(), _container.FakeMany<long>());
            var connection = _container.Get<IDataConnection<SqlConnection>>();

            var expectedCommand = new
            {
                command.PayerAlias,
                command.OutboundRefEo,
                command.CreatedBy,
                Payments = command.paymentIds.ToIdDataTable().AsTableValuedParameter("IdTable")
            };

            connection.When(_ => _.GetFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_Outbound835File_Insert", StringComparison.CurrentCultureIgnoreCase)),
                    Arg.Is<object>(_ => JsonConvert.SerializeObject(_) == JsonConvert.SerializeObject(expectedCommand)), Arg.Any<CancellationToken>()))
                 .Do(_ => throw new Exception());

            var handler = _container.Get<InsertOutbound835File.Handler>();
            Func<Task<long>> act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>();

            await connection.Received().GetFirstOrDefaultAsync<long>(Arg.Is<string>(_ => _.Equals("dbo.usp_Outbound835File_Insert", StringComparison.CurrentCultureIgnoreCase)),
                Arg.Is<object>(_ => JsonConvert.SerializeObject(_) == JsonConvert.SerializeObject(expectedCommand)), Arg.Any<CancellationToken>());
        }
    }
}
