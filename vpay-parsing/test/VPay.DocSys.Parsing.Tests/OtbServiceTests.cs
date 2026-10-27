using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Models;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class OtbServiceTests
    {
        private readonly StatusFileSettings _statusFileSettings;
        private readonly Mock<IOTBParserService> _otbParserService = new Mock<IOTBParserService>();
        private readonly Mock<IOtbDataService> _otbDataService = new Mock<IOtbDataService>();
        private readonly Mock<IFileSystem> _fileSystem = new Mock<IFileSystem>();

        private readonly IOtbService SUT;

        public OtbServiceTests()
        {
            _statusFileSettings = new StatusFileSettings
            {
                PythonHome = @"/python"
            };

            var path = new Mock<IPath>();
            path.Setup(x => x.PathSeparator).Returns('~');
            path.Setup(x => x.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string a, string b, string c) => $"{a}{path.Object.PathSeparator}{b}{path.Object.PathSeparator}{c}");

            _fileSystem.Setup(x => x.Path).Returns(path.Object);

            _otbDataService.Setup(x => x.UseFile).Returns(true);
            _otbDataService.Setup(x => x.UseApi).Returns(true);

            _otbParserService
                .Setup(x => x.ParseOTBFile(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, IDictionary<string, string>>());

            SUT = new OtbService(
                _statusFileSettings,
                _otbParserService.Object,
                _otbDataService.Object,
                _fileSystem.Object,
                new NullLogger<OtbService>());
        }

        [Fact]
        public void ShouldThrowExceptionWhenOtbDataServiceSettingsAreAllFalse()
        {
            _otbDataService.Setup(x => x.UseFile).Returns(false);
            _otbDataService.Setup(x => x.UseApi).Returns(false);

            Action ctor = () => new OtbService(
                 _statusFileSettings,
                 _otbParserService.Object,
                 _otbDataService.Object,
                 _fileSystem.Object,
                 new NullLogger<OtbService>());

            Assert.Throws<ArgumentException>(ctor);

            using (var a = new AssertionScope())
            {
                _otbDataService.Verify(x => x.UseFile);
                _otbDataService.Verify(x => x.UseApi);

                _otbDataService.VerifyNoOtherCalls();
                _fileSystem.VerifyNoOtherCalls();
                _otbParserService.VerifyNoOtherCalls();
            }
        }

        public class InvalidArguments : OtbServiceTests
        {
            [Theory]
            [InlineData(typeof(ArgumentNullException), null)]
            [InlineData(typeof(ArgumentException), "")]
            [InlineData(typeof(ArgumentException), " ")]
            public async Task ShouldThrowExceptionForInvalidClientCode(Type exceptionType, string clientCode)
            {
                await Assert.ThrowsAsync(exceptionType, async () => await SUT.GetOtbRecordsAsync(clientCode, new[] { "BE" }).ConfigureAwait(false)).ConfigureAwait(false);

                using (var a = new AssertionScope())
                {
                    _otbDataService.Verify(x => x.UseFile);
                    _otbDataService.Verify(x => x.UseApi);

                    _otbDataService.VerifyNoOtherCalls();
                    _fileSystem.VerifyNoOtherCalls();
                    _otbParserService.VerifyNoOtherCalls();
                }
            }

            [Theory]
            [MemberData(nameof(InvalidBeCodes))]
            public async Task ShouldThrowExceptionFoInvalidBeCodes(Type exceptionType, string[] beCodes)
            {
                await Assert.ThrowsAsync(exceptionType, async () => await SUT.GetOtbRecordsAsync("TPA", beCodes).ConfigureAwait(false)).ConfigureAwait(false);

                using (var a = new AssertionScope())
                {
                    _otbDataService.Verify(x => x.UseFile);
                    _otbDataService.Verify(x => x.UseApi);

                    _otbDataService.VerifyNoOtherCalls();
                    _fileSystem.VerifyNoOtherCalls();
                    _otbParserService.VerifyNoOtherCalls();
                }
            }
        }

        public class WhenOnlyUsingApi : OtbServiceTests
        {
            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 2)]
            [MemberData(nameof(CreateParams), "CON", "BE", 2)]
            public async Task ShouldNotUseFile(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.ToLower();

                _otbDataService.Setup(x => x.UseFile).Returns(false);

                _otbDataService
                    .Setup(x => x.GetOtbDataAsync(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync((string a, ICollection<string> b) => new OtbBillingEntityClientData("", "", clientCode, ToBeDictionary(beCodes)));

                SetupFileResults(beCodes);

                _ = await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false);

                using (var a = new AssertionScope())
                {
                    _otbDataService.Verify(x => x.UseFile);
                    _otbDataService.Verify(x => x.UseApi);
                    _otbDataService.Verify(x => x.GetOtbDataAsync(
                        It.Is<string>(c => c == clientCode.ToUpper()),
                        It.Is<ICollection<string>>(c =>
                            (c.Count == beCodes.Count()) &&
                            !c.Except(beCodes).Any())),
                        Times.Once);

                    _otbDataService.VerifyNoOtherCalls();
                    _fileSystem.VerifyNoOtherCalls();
                    _otbParserService.VerifyNoOtherCalls();
                }
            }

            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 3)]
            [MemberData(nameof(CreateParams), "CON", "BE", 3)]
            public async Task ShouldThrowExceptionOnFailedValidation(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.ToLower();

                _otbDataService.Setup(x => x.UseFile).Returns(false);

                _otbDataService
                    .Setup(x => x.GetOtbDataAsync(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync((string a, ICollection<string> b) => new OtbBillingEntityClientData("", "", clientCode, ToBeDictionary(beCodes.Skip(1).ToArray())));

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        public class WhenOnlyUsingFile : OtbServiceTests
        {
            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 1)]
            [MemberData(nameof(CreateParams), "CON", "BE", 1)]
            public async Task ShouldNotUseApi(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.EnsureIsSmbSafeName().ToLower();

                _otbDataService.Setup(x => x.UseApi).Returns(false);

                SetupFileResults(beCodes);

                _ = await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false);

                using (var a = new AssertionScope())
                {
                    _otbDataService.Verify(x => x.UseFile);
                    _otbDataService.Verify(x => x.UseApi);

                    _otbDataService.VerifyNoOtherCalls();

                    _fileSystem.Verify(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
                    _fileSystem.Verify(x => x.Path.PathSeparator);
                    _fileSystem.VerifyNoOtherCalls();

                    _otbParserService.Verify(x => x.ParseOTBFile(
                        It.Is<string>(c => c == $"/python~{clientCode}~{clientCode}.otb"),
                        It.IsAny<string>()));
                    _otbParserService.VerifyNoOtherCalls();
                }
            }

            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 3)]
            [MemberData(nameof(CreateParams), "CON", "BE", 3)]
            public async Task ShouldThrowExceptionOnFailedValidation(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.EnsureIsSmbSafeName().ToLower();

                _otbDataService.Setup(x => x.UseApi).Returns(false);

                SetupFileResults(beCodes.Skip(1).ToArray());

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        public class WhenUsingBothSources : OtbServiceTests
        {
            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 2)]
            [MemberData(nameof(CreateParams), "CON", "BE", 2)]
            public async Task ShouldWorkCorrectly(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.ToLower();

                _otbDataService
                    .Setup(x => x.GetOtbDataAsync(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync((string a, ICollection<string> b) => new OtbBillingEntityClientData("", "", clientCode, ToBeDictionary(beCodes)));

                SetupFileResults(beCodes);

                _ = await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false);

                using (var a = new AssertionScope())
                {
                    _otbDataService.Verify(x => x.UseFile);
                    _otbDataService.Verify(x => x.UseApi);
                    _otbDataService.Verify(x => x.GetOtbDataAsync(
                        It.Is<string>(c => c == clientCode.ToUpper()),
                        It.Is<ICollection<string>>(c =>
                            (c.Count == beCodes.Count()) &&
                            !c.Except(beCodes).Any())),
                        Times.Once);
                    _otbDataService.VerifyNoOtherCalls();

                    _fileSystem.Verify(x => x.Path.Combine(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
                    _fileSystem.Verify(x => x.Path.PathSeparator);
                    _fileSystem.VerifyNoOtherCalls();

                    var fileClientCode = clientCode.EnsureIsSmbSafeName();
                    _otbParserService.Verify(x => x.ParseOTBFile(
                        It.Is<string>(c => c == $"/python~{fileClientCode}~{fileClientCode}.otb"),
                        It.IsAny<string>()));
                    _otbParserService.VerifyNoOtherCalls();
                }
            }

            [Theory]
            [MemberData(nameof(CreateParams), "TPA", "BE", 3)]
            [MemberData(nameof(CreateParams), "CON", "BE", 3)]
            public async Task ShouldThrowExceptionOnFailedValidation(string clientCode, string[] beCodes)
            {
                clientCode = clientCode.ToLower();

                _otbDataService.Setup(x => x.UseFile).Returns(false);

                _otbDataService
                    .Setup(x => x.GetOtbDataAsync(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync((string a, ICollection<string> b) => new OtbBillingEntityClientData("", "", clientCode, ToBeDictionary(beCodes.Skip(1).ToArray())));

                await Assert.ThrowsAsync<KeyNotFoundException>(async () => await SUT.GetOtbRecordsAsync(clientCode, beCodes).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        private void SetupFileResults(string[] beCodes)
        {
            _otbParserService
                .Setup(x => x.ParseOTBFile(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(ToBeDictionary(beCodes));
        }

        private static dynamic ToBeDictionary(string[] beCodes)
        => beCodes.ToDictionary(
            k => k,
            v => (IDictionary<string, string>)new Dictionary<string, string>());

        public static IEnumerable<object[]> CreateParams(string tpa, string be, int distinctBeCount)
        {
            var l = new List<string>(distinctBeCount);

            for (int i = 0; i < distinctBeCount; i++)
            {
                l.Add($"{be}{i}");
            }

            yield return new object[] { tpa, l.ToArray() };
        }

        public static IEnumerable<object[]> InvalidBeCodes()
        {
            yield return new object[] { typeof(ArgumentNullException), null };
            yield return new object[] { typeof(ArgumentException), Array.Empty<string>() };
        }
    }
}
