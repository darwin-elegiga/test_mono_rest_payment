using System;
using System.IO;
using System.Threading.Tasks;
using Ardalis.Result;
using FluentAssertions;
using FluentAssertions.Execution;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VPay.Optum.PaymentRouting.Api.Controllers;
using VPay.Optum.PaymentRouting.Api.Controllers.Models;
using VPay.Optum.PaymentRouting.Application.Outbound.Commands;
using VPay.Optum.PaymentRouting.Application.Outbound.Models;
using VPay.Optum.PaymentRouting.Application.Outbound.Queries;
using VPay.Optum.PaymentRouting.Data.Sql.Models;
using VPay.Optum.PaymentRouting.Data.Sql.Outbound835File.Queries;
using Xunit;

namespace VPay.Optum.PaymentRouting.Api.Tests.Controllers
{
    /// <summary>
    /// Tests for <see cref=""/>
    /// </summary>
    public class Outbound835ControllerTests
    {
        private readonly IMediator _mediator;
        private readonly Outbound835Controller _controller;

        public Outbound835ControllerTests()
        {
            _mediator = Mock.Of<IMediator>(MockBehavior.Strict);
            _controller = new Outbound835Controller(_mediator);
        }

        public sealed class Constructor : Outbound835ControllerTests
        {
#pragma warning disable CA1806

            [Fact]
            public void MissingMediator_ThrowsException()
            {
                // ReSharper disable once ObjectCreationAsStatement
                Action ctor = () => new Outbound835Controller(null);
                ctor.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("mediator");
            }

#pragma warning restore CA1806
        }

        public class CanDownloadOutbound835Document : Outbound835ControllerTests
        {
            [Fact]
            public async Task WhenTransactionIdIsInvalid_ThenShouldReturnBadRequest()
            {
                long transactionId = 0;
                var result = await _controller
                    .CanDownloadOutbound835Document(transactionId, default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
                    badResult.Should().NotBeNull();
                    badResult.StatusCode.Should().Be(400);
                }
                Mock.Get(_mediator).Verify(x => x.Send(It.IsAny<GetOutbound835FileDataByTransactionId.Query>(), default), Times.Never);
            }

            [Fact]
            public async Task WhenTransactionIdDoesNotExist_ThenShouldReturnNotFound()
            {
                long transactionId = 1;

                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<GetOutbound835FileDataByTransactionId.Query>(), default))
                    .ReturnsAsync((Outbound835Data)null)
                    .Verifiable("Send Not Called");
                var result = await _controller
                    .CanDownloadOutbound835Document(transactionId, default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var badResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
                    badResult.Should().NotBeNull();
                    badResult.StatusCode.Should().Be(404);
                }
                Mock.Get(_mediator).VerifyAll();
            }

            [Fact]
            public async Task WhenTransactionIdExists_ThenShouldReturnOk()
            {
                long transactionId = 1;
                Outbound835Data edi835Data = new();

                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<GetOutbound835FileDataByTransactionId.Query>(), default))
                    .ReturnsAsync(edi835Data)
                    .Verifiable("Send Not Called");

                var result = await _controller
                    .CanDownloadOutbound835Document(transactionId, default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
                    okResult.Should().NotBeNull();
                    var dto = okResult.Value.Should().BeOfType<Outbound835ExistsDto>().Subject;
                    dto.TransactionId.Should().Be(transactionId);
                    dto.Exists.Should().Be(true);
                    okResult.StatusCode.Should().Be(200);
                }
                Mock.Get(_mediator).VerifyAll();
            }
        }

        public sealed class DownloadOutbound835Document : Outbound835ControllerTests
        {
            [Fact]
            public async Task WhenDownload835DataReturnsNull_ThenShouldReturnNotFound()
            {
                var transactionId = 0;

                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<GetOutbound835Document.Query>(), default))
                    .ReturnsAsync(Result<Outbound835Document>.NotFound())
                    .Verifiable("Send Not Called");

                var result = await _controller
                    .DownloadOutbound835Document(transactionId, default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var badResult = result.Should().BeOfType<NotFoundResult>().Subject;
                    badResult.Should().NotBeNull();
                    badResult.StatusCode.Should().Be(404);
                }

                Mock.Get(_mediator).VerifyAll();
            }

            [Fact]
            public async Task WhenDownload835DataReturnsData_ThenShouldReturn835Document()
            {
                var transactionId = 1;

                var response = new Outbound835Document()
                {
                    FileName = "edi-test",
                    Stream = new MemoryStream(),
                    ContentType = "text/plain"
                };

                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<GetOutbound835Document.Query>(), default))
                    .ReturnsAsync(Result<Outbound835Document>.Success(response))
                    .Verifiable("Send Not Called");

                var result = await _controller
                    .DownloadOutbound835Document(transactionId, default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var docResult = result.Should().BeOfType<FileStreamResult>().Subject;
                    docResult.Should().NotBeNull();
                }

                Mock.Get(_mediator).VerifyAll();
            }
        }

        public sealed class StartOutbound835 : Outbound835ControllerTests
        {
            [Fact]
            public async Task WhenMediatorReturnsCount_ThenShouldReturnAcceptedWithCount()
            {
                var expectedCount = 5;

                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<Queue835Files.Command>(), default))
                    .ReturnsAsync(expectedCount)
                    .Verifiable("Send Not Called");

                var result = await _controller.StartOutbound835(default);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
                    acceptedResult.Should().NotBeNull();
                    acceptedResult.StatusCode.Should().Be(202);
                    acceptedResult.Value.Should().Be(expectedCount);
                }

                Mock.Get(_mediator).VerifyAll();
            }

            [Fact]
            public async Task WhenMediatorThrowsException_ThenShouldPropagateException()
            {
                Mock.Get(_mediator)
                    .Setup(x => x.Send(It.IsAny<Queue835Files.Command>(), default))
                    .ThrowsAsync(new InvalidOperationException("Test exception"))
                    .Verifiable("Send Not Called");
                Func<Task> act = async () => await _controller.StartOutbound835(default);
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Test exception");
                Mock.Get(_mediator).VerifyAll();
            }
        }
    }
}
