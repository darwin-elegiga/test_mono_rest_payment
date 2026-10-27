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
using VPay.DocSys.Parsing.Common.Interfaces;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    /// <summary>
    /// Tests for the <see cref="StpPaymentService"/> class.
    /// </summary>
    public class StpPaymentServiceTests
    {
        private readonly IStpPaymentRepository _repository = new Mock<IStpPaymentRepository>(MockBehavior.Strict).Object;
        private readonly IStatusFileTracker _tracker = new Mock<IStatusFileTracker>(MockBehavior.Strict).Object;
        private readonly ILogger<StpPaymentService> _logger = new NullLogger<StpPaymentService>();
        private readonly StpPaymentService _service;

        public StpPaymentServiceTests()
        {
            _service = new StpPaymentService(_repository, _tracker, _logger);
        }

        public class Constructor : StpPaymentServiceTests
        {

            [Fact]
            public void ThrowsOnNullRepository()
            {
                Assert.Throws<ArgumentNullException>(() => new StpPaymentService(null, _tracker, _logger));
            }

            [Fact]
            public void ThrowsOnNullTracker()
            {
                Assert.Throws<ArgumentNullException>(() => new StpPaymentService(_repository, null, _logger));
            }

            [Fact]
            public void ThrowsOnNullLogger()
            {
                Assert.Throws<ArgumentNullException>(() => new StpPaymentService(_repository, _tracker, null));
            }
        }

        public class GetStpPayment : StpPaymentServiceTests
        {
            [Fact]
            public async Task ReturnsItem_WhenCallingTracker()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_tracker).Setup(r => r.GetStpPayment(It.IsAny<long>()))
                    .Returns(stpPayment)
                    .Verifiable();

                var result = await _service.GetStpPayment(123, false, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public async Task ReturnsNull_WhenNotFoundInTracker()
            {
                Mock.Get(_tracker).Setup(r => r.GetStpPayment(It.IsAny<long>()))
                    .Returns((StpPayment)null)
                    .Verifiable();

                var result = await _service.GetStpPayment(123, false, CancellationToken.None);

                Assert.Null(result);
                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public void Throws_OnTrackerError()
            {
                Mock.Get(_tracker).Setup(r => r.GetStpPayment(It.IsAny<long>()))
                    .Throws(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.GetStpPayment(123, false, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public void ReturnsItem_WhenCallingRepository()
            {
                var stpPayment = MockStpPayment(123);
                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayment)
                    .Verifiable();

                var result = _service.GetStpPayment(123, true, CancellationToken.None).Result;

                Assert.NotNull(result);
                Assert.Equivalent(stpPayment, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void ReturnsNull_WhenNotFoundInRepository()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((StpPayment)null)
                    .Verifiable();

                var result = _service.GetStpPayment(123, true, CancellationToken.None).Result;

                Assert.Null(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void Throws_OnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayment(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var ex = Assert.Throws<AggregateException>(() => _service.GetStpPayment(123, true, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_repository).VerifyAll();
            }
        }

        public class GetStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public async Task ReturnsItemList_WhenCallingTracker()
            {
                var stpPayments = MockStpPayments(3);
                Mock.Get(_tracker).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, false, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public async Task ReturnsEmptyList_WhenNotFoundInTracker()
            {
                var stpPayments = Enumerable.Empty<StpPayment>();
                Mock.Get(_tracker).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, false, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Assert.Empty(result);
                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public void Throws_OnTrackerError()
            {
                Mock.Get(_tracker).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Throws(new Exception("Test exception"))
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var ex = Assert.Throws<AggregateException>(() => _service.GetStpPayments(stpPaymentRequest, false, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
                Mock.Get(_tracker).VerifyAll();
            }
            [Fact]
            public async Task ReturnsItemList_WhenCallingRepository()
            {
                var stpPayments = MockStpPayments(3);
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, true, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public async Task ReturnsEmptyList_WhenNotFoundInRepository()
            {
                var stpPayments = Enumerable.Empty<StpPayment>();
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(stpPayments)
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var result = await _service.GetStpPayments(stpPaymentRequest, true, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equivalent(stpPayments, result);

                Assert.Empty(result);
                Mock.Get(_repository).VerifyAll();
            }

            [Fact]
            public void Throws_OnRepositoryError()
            {
                Mock.Get(_repository).Setup(r => r.GetStpPayments(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Test exception"))
                    .Verifiable();

                var stpPaymentRequest = new List<long> { 1, 2, 3 };
                var ex = Assert.Throws<AggregateException>(() => _service.GetStpPayments(stpPaymentRequest, true, CancellationToken.None).Wait());

                Assert.NotNull(ex);
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("Test exception", ex.InnerException.Message);
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

        public class InsertStpPayments : StpPaymentServiceTests
        {
            [Fact]
            public async Task CallsBulkInsertWithAllNewTransactionIdsAsync()
            {
                var transactionIds = new List<long> { 1, 2, 3 };
                var existingPayments = new List<StpPayment>();
                // None exists, so 1, 2 and 3 should be inserted
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments)
                    .Verifiable();

                Mock.Get(_tracker)
                    .Setup(t => t.BulkInsertStpPayments(
                        It.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new[] { 1L, 2L, 3L })),
                        "TestNetwork", "B2B", "SDS", false, false, false, false))
                    .Verifiable();

                var count =  await _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS");
                Assert.Equal(3, count);

                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public async Task CallsBulkInsertWithOnlyNewTransactionIds()
            {
                var transactionIds = new List<long> { 1, 2, 3 };
                var existingPayments = new List<StpPayment>
                {
                    MockStpPayment(2)
                };
                // Only 2 exists, so 1 and 3 should be inserted
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments)
                    .Verifiable();

                Mock.Get(_tracker)
                    .Setup(t => t.BulkInsertStpPayments(
                        It.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new[] { 1L, 3L })),
                        "TestNetwork", "B2B", "SDS", false, false, false, false))
                    .Verifiable();

                var count = await _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS");
                Assert.Equal(2, count);

                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public void DoesNotCallBulkInsertIfAllTransactionIdsExist()
            {
                var transactionIds = new List<long> { 1, 2 };
                var existingPayments = new List<StpPayment>
                {
                    MockStpPayment(1),
                    MockStpPayment(2)
                };
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments);

                Mock.Get(_tracker)
                    .Setup(t => t.BulkInsertStpPayments(
                        It.Is<IEnumerable<long>>(ids => !ids.Any()),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()));

                _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS").Wait();

                Mock.Get(_tracker).Verify(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()), Times.Once);
                Mock.Get(_tracker).Verify(t => t.BulkInsertStpPayments(
                    It.Is<IEnumerable<long>>(ids => !ids.Any()),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            }

            [Fact]
            public void HandlesEmptyTransactionIds()
            {
                var transactionIds = new List<long>();
                var existingPayments = new List<StpPayment>();
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments);

                Mock.Get(_tracker)
                    .Setup(t => t.BulkInsertStpPayments(
                        It.Is<IEnumerable<long>>(ids => !ids.Any()),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()));

                _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS").Wait();

                Mock.Get(_tracker).Verify(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()), Times.Once);
                Mock.Get(_tracker).Verify(t => t.BulkInsertStpPayments(
                    It.Is<IEnumerable<long>>(ids => !ids.Any()),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            }

            [Fact]
            public void ThrowsAndLogsOnTrackerError()
            {
                var transactionIds = new List<long> { 1, 2, 3 };
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Throws(new InvalidOperationException("Tracker error"))
                    .Verifiable();

                var ex = Assert.Throws<InvalidOperationException>(() =>
                    _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS").Wait());

                Assert.NotNull(ex);
                Assert.Equal("Tracker error", ex.Message);

                Mock.Get(_tracker).VerifyAll();
            }

            [Fact]
            public void ThrowsAndLogsOnBulkInsertError()
            {
                var transactionIds = new List<long> { 1, 2 };
                var existingPayments = new List<StpPayment>();
                Mock.Get(_tracker)
                    .Setup(t => t.GetStpPayments(It.IsAny<IEnumerable<long>>()))
                    .Returns(existingPayments)
                    .Verifiable();

                Mock.Get(_tracker)
                    .Setup(t => t.BulkInsertStpPayments(
                        It.IsAny<IEnumerable<long>>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Throws(new InvalidOperationException("Bulk insert error"))
                    .Verifiable();

                var ex = Assert.Throws<InvalidOperationException>(() =>
                    _service.InsertStpPayments(transactionIds, "TestNetwork", "B2B", "SDS").Wait());

                Assert.NotNull(ex);
                Assert.Equal("Bulk insert error", ex.Message);

                Mock.Get(_tracker).VerifyAll();
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
