using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Data;
using VPay.DocSys.Data.Abstractions;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    /// <summary>
    /// Tests for the <see cref="StpPaymentService"/> class.
    /// </summary>
    public class StpPaymentServiceTests
    {
        private readonly IStpPaymentRepository _repository = new Mock<IStpPaymentRepository>(MockBehavior.Strict).Object;
        private readonly ILogger<StpPaymentService> _logger = new NullLogger<StpPaymentService>();
        private readonly StpPaymentService _service;

        public StpPaymentServiceTests()
        {
            _service = new StpPaymentService(_repository, _logger);
        }

        public class Constructor : StpPaymentServiceTests
        {

            [Fact]
            public void ThrowsOnNullRepository()
            {
                Assert.Throws<ArgumentNullException>(() => new StpPaymentService(null, _logger));
            }

            [Fact]
            public void ThrowsOnNullLogger()
            {
                Assert.Throws<ArgumentNullException>(() => new StpPaymentService(_repository, null));
            }
        }

        public class GetStpPayment : StpPaymentServiceTests
        {
            [Fact]
            public async Task CallsRepository()
            {
                var stpPayment = MockStpPayment(123);

                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = await _service.GetStpPayment(123, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public async Task ReturnsNullWhenNotFound()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = await _service.GetStpPayment(123, CancellationToken.None);

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public async Task ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = await Assert.ThrowsAsync<Exception>(async () => await _service.GetStpPayment(123, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal("Test exception", ex.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class GetStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public async Task CallsRepository()
            {
                var stpPayments = MockStpPayments(3);
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public async Task ReturnsEmptyWhenNotFound()
            {
                var stpPayments = Enumerable.Empty<StpPayment>();
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Assert.Empty(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public async Task ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var ex = await Assert.ThrowsAsync<Exception>(async () => await _service.GetStpPayments(stpPaymentRequest, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal("Test exception", ex.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class SearchStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public void CallsRepository()
            {
                var stpPayments = MockStpPayments(3);
                Mock.Get(_repository).Setup(r => r.SearchStpPayments(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments.ToList())
                    .Verifiable();

                var result = _service.SearchStpPayments(
                    "RECTANGLE",
                    "B2B",
                    "SDS",
                    null,
                    null,
                    null,
                    null,
                    null,
                    10,
                    CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsEmptyWhenNotFound()
            {
                var stpPayments = Enumerable.Empty<StpPayment>();
                Mock.Get(_repository).Setup(r => r.SearchStpPayments(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments.ToList())
                    .Verifiable();

                var result = _service.SearchStpPayments(
                    "RECTANGLE",
                    "B2B",
                    "SDS",
                    null,
                    null,
                    null,
                    null,
                    null,
                    10,
                    CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Assert.Empty(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.SearchStpPayments(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.SearchStpPayments(
                    "RECTANGLE",
                    "B2B",
                    "SDS",
                    null,
                    null,
                    null,
                    null,
                    null,
                    10,
                    CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class MarkStpPaymentRemitValid : StpPaymentServiceTests
        {
            [Fact]
            public void CallsRepository()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_repository).Setup(r => r.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = _service.MarkRemitValid(123, null, CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsNullWhenNotFound()
            {
                Mock.Get(_repository).Setup(r => r.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = _service.MarkRemitValid(123, null, CancellationToken.None).Result;

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.MarkRemitValid(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.MarkRemitValid(123, null, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class MarkStpPaymentEdi835Avail : StpPaymentServiceTests
        {
            [Fact]
            public void CallsRepository()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_repository).Setup(r => r.MarkEdi835Avail(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = _service.MarkEdi835Avail(123, null, CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsNullWhenNotFound()
            {
                Mock.Get(_repository).Setup(r => r.MarkEdi835Avail(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = _service.MarkEdi835Avail(123, null, CancellationToken.None).Result;

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.MarkEdi835Avail(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.MarkEdi835Avail(123, null, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class MarkStpPaymentPaymentSent : StpPaymentServiceTests
        {
            [Fact]
            public void CallsRepository()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_repository).Setup(r => r.MarkPaymentSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = _service.MarkPaymentSent(123, null, CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsNullWhenNotFound()
            {
                Mock.Get(_repository).Setup(r => r.MarkPaymentSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = _service.MarkPaymentSent(123, null, CancellationToken.None).Result;

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.MarkPaymentSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.MarkPaymentSent(123, null, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class MarkStpPaymentPaymentNotSent : StpPaymentServiceTests
        {
            [Fact]
            public void CallsRepository()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_repository).Setup(r => r.MarkPaymentNotSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = _service.MarkPaymentNotSent(123, null, CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsNullWhenNotFound()
            {
                Mock.Get(_repository).Setup(r => r.MarkPaymentNotSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = _service.MarkPaymentNotSent(123, null, CancellationToken.None).Result;

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ThrowsOnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.MarkPaymentNotSent(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.MarkPaymentNotSent(123, null, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        private static IEnumerable<StpPayment> MockStpPayments(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return MockStpPayment(i);
            }
        }

        private static StpPayment MockStpPayment(
            long transactionId,
            string enrollType = "B2B",
            string edi835Type = "SDS",
            bool remitValid = false,
            bool edi835Avail = false,
            bool paymentSent = false)
        {
            StpPayment stpPayment = new()
            {
                TransactionId = transactionId,
                NetworkName = "TestNetwork",
                EnrollType = enrollType,
                Edi835Type = edi835Type,
                RemitValid = remitValid,
                Edi835Avail = edi835Avail,
                PaymentSent = paymentSent,
                PaymentSentRetries = 0,
                RemitValidDateTime = remitValid ? DateTime.Today : null,
                Edi835AvailDateTime = edi835Avail ? DateTime.Today : null,
                PaymentSentDateTime = paymentSent ? DateTime.Today : null,
                CreatedUser = "UnitTest",
                CreatedDateTime = DateTime.Today,
                LastModifiedUser = "UnitTest",
                LastModifiedDateTime = DateTime.Today
            };

            return stpPayment;
        }
    }
}
