using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VPay.DocSys.Storage;
using Xunit;

namespace VPay.Distribution.Tests
{
    public class StorageServiceTests
    {
        private readonly IStorageManager _storageManager;
        private readonly ILogger<StorageService> _logger;
        private readonly StorageService _service;

        public StorageServiceTests()
        {
            _storageManager = Mock.Of<IStorageManager>(MockBehavior.Strict);
            _logger = new NullLogger<StorageService>();
            _service = new StorageService(
                _storageManager,
                _logger
            );
        }

        public class Constructor : StorageServiceTests
        {
            [Fact]
            public void WhenStorageManagerIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StorageService(
                        null!,
                        _logger);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("storageManager");
            }
            [Fact]
            public void WhenLoggerIsNull_ThrowsArgumentNullException()
            {
                Action act = () =>
                {
                    var _ = new StorageService(
                        _storageManager,
                        null!);
                };
                act.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("logger");
            }
        }

        public class GetArchiveFiles : StorageServiceTests
        {
            [Fact]
            public async Task GetArchiveFiles_ReturnsListOfFiles()
            {
                long transactionId = 1234567890L;

                var expectedFiles = new List<IArchiveFile> {
                    Mock.Of<IArchiveFile>(af => af.FileName == "file1.txt"),
                    Mock.Of<IArchiveFile>(af => af.FileName == "file2.pdf"),
                    Mock.Of<IArchiveFile>(af => af.FileName == "file3.json")
                };

                Mock.Get(_storageManager)
                    .Setup(sm => sm.GetArchivesForTransactionId(transactionId, true))
                    .ReturnsAsync(expectedFiles);

                var result = await _service.GetArchiveFiles(transactionId);

                result.Count().Should().Be(expectedFiles.Count);

                Mock.Get(_storageManager).Verify(sm => sm.GetArchivesForTransactionId(transactionId, true), Times.Once);
            }
        }

        public class FormatRemitJson : StorageServiceTests
        {
            //[Fact]
            //public void FormatRemitJson_ReturnsFormattedJson()
            //{
            //    var rawJson = "{\n  \"header\": {\n    \"networkPartner\": \"MY-NETWORK\",\n    \"optumTransactionId\": \"0160793435\"\n  },\n  \"payee\": {\n    \"name\": \"MY HEALTH SYSTEM\",\n    \"tin\": \"987654321\",\n    \"addressLine1\": \"PO BOX 123465\",\n    \"city\": \"ATLANTA\",\n    \"state\": \"GA\",\n    \"zip\": \"30374\"\n  },\n  \"payer\": {\n    \"traceNumber\": \"TRN*1*12977817*1470858534*CREDITCARD~\",\n    \"tpa\": \"RLY\",\n    \"address\": \"123 SOME BLVD.\",\n    \"city\": \"SOMEWHERE\",\n    \"state\": \"UT\",\n    \"zip\": \"84020\"\n  }\n}";
            //    var formattedJson = "{\r\n  \"header\": {\r\n    \"networkPartner\": \"MY-NETWORK\",\r\n    \"optumTransactionId\": \"0160793435\"\r\n  },\r\n  \"payee\": {\r\n    \"name\": \"MY HEALTH SYSTEM\",\r\n    \"tin\": \"987654321\",\r\n    \"addressLine1\": \"PO BOX 123465\",\r\n    \"city\": \"ATLANTA\",\r\n    \"state\": \"GA\",\r\n    \"zip\": \"30374\"\r\n  },\r\n  \"payer\": {\r\n    \"traceNumber\": \"TRN*1*12977817*1470858534*CREDITCARD~\",\r\n    \"tpa\": \"RLY\",\r\n    \"address\": \"123 SOME BLVD.\",\r\n    \"city\": \"SOMEWHERE\",\r\n    \"state\": \"UT\",\r\n    \"zip\": \"84020\"\r\n  }\r\n}";
            //    var result = _service.FormatRemitJson(rawJson);
            //    result.Should().Be(formattedJson);
            //}

            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("  ")]
            public void FormatRemitJson_ReturnsEmpty_WhenInvalidInput(string rawJson)
            {
                var result = _service.FormatRemitJson(rawJson);
                result.Should().Be(string.Empty);
            }
        }
    }
}
