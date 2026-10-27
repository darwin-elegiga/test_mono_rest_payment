using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Comparers;
using VPay.DocSys.Parsing.Common.Models;
using Xunit;

namespace VPay.DocSys.Parsing.Tests
{
    public class OtbDataServiceTests
    {
        private readonly OtbDataService _service;
        private readonly OtbDataServiceSettings _settings;
        private readonly Mock<IPlatformConfigurationApiClient> _otbApi;
        private readonly ILogger<OtbDataService> _logger = new NullLogger<OtbDataService>();

        public OtbDataServiceTests()
        {
            _settings = new OtbDataServiceSettings { UseFile = true, UseApi = true };
            _otbApi = new Mock<IPlatformConfigurationApiClient>();
            _service = new OtbDataService(_settings, _otbApi.Object, _logger);
        }

        #region Test Data

        private const string _originCode = "TEST";
        private const string _originName = "Test Medical";
        private const string _clientCode = "ACME";
        private const string _beCode1 = "TEST000001";
        private const string _beCode2 = "TEST000002";
        private const string _fixedName0 = "Origin";
        private const string _fixedValue0 = _originCode;
        private const string _fixedName1 = "OriginName";
        private const string _fixedValue1 = _originName;
        private const string _fixedName2 = "TPA";
        private const string _fixedValue2 = _clientCode;
        private const string _fixedName3 = "BillingEntity";
        private const string _fieldName0 = "00";
        private const string _fieldValue0 = "42";
        private const string _fieldName1 = "PayType";
        private const string _fieldValue1 = "*";
        private const string _fieldName2 = "TPA_Name";
        private const string _fieldValue2 = "ACME";
        private const string _fieldName3 = "TPA_Full_Name";
        private const string _fieldValue3 = "ACME, Inc.";
        private const string _fieldName4 = "RawBEName";
        private const string _commonName4 = "FundingAccount";
        private const string _fundingAccount1 = "123";
        private const string _fundingAccount2 = "456";
        private const string _fieldName5 = "ReturnAddress";
        private const string _returnAddress1 = "123 Some St";
        private const string _returnAddress2 = "456 Other St";

        private readonly DateTime _effectiveFromDate = DateTime.Now;

        #region Empty BEs

        private IDictionary<string, IDictionary<string, string>> EmptyBillingEntityList => new Dictionary<string, IDictionary<string, string>>();

        #endregion

        #region Expected Fields

        private IDictionary<string, string> ExpectedFields14 => new Dictionary<string, string>
        {
            { _fixedName0, _fixedValue0 },
            { _fixedName1, _fixedValue1 },
            { _fixedName2, _fixedValue2 },
            { _fixedName3, _beCode1 },
            { _fieldName1, _fieldValue1 },
            { _fieldName2, _fieldValue2 },
            { _fieldName3, _fieldValue3 },
            { _fieldName4, _fundingAccount1 }
        };

        private IDictionary<string, string> ExpectedFields24 => new Dictionary<string, string>
        {
            { _fixedName0, _fixedValue0 },
            { _fixedName1, _fixedValue1 },
            { _fixedName2, _fixedValue2 },
            { _fixedName3, _beCode2 },
            { _fieldName1, _fieldValue1 },
            { _fieldName2, _fieldValue2 },
            { _fieldName3, _fieldValue3 },
            { _fieldName4, _fundingAccount2 }
        };

        private IDictionary<string, string> ExpectedFields15 => new Dictionary<string, string>
        {
            { _fixedName0, _fixedValue0 },
            { _fixedName1, _fixedValue1 },
            { _fixedName2, _fixedValue2 },
            { _fixedName3, _beCode1 },
            { _fieldName1, _fieldValue1 },
            { _fieldName2, _fieldValue2 },
            { _fieldName3, _fieldValue3 },
            { _fieldName4, _fundingAccount1 },
            { _fieldName5, _returnAddress1 }
        };

        private IDictionary<string, string> ExpectedFields25 => new Dictionary<string, string>
        {
            { _fixedName0, _fixedValue0 },
            { _fixedName1, _fixedValue1 },
            { _fixedName2, _fixedValue2 },
            { _fixedName3, _beCode2 },
            { _fieldName1, _fieldValue1 },
            { _fieldName2, _fieldValue2 },
            { _fieldName3, _fieldValue3 },
            { _fieldName4, _fundingAccount2 },
            { _fieldName5, _returnAddress2 }
        };

        #endregion

        #region Expected BEs

        /// <summary>
        /// Expected result with 1 billing entity with 4 custom fields.
        /// </summary>
        private IDictionary<string, IDictionary<string, string>> ExpectedBillingEntity14 => new Dictionary<string, IDictionary<string, string>>
        {
            { _beCode1, ExpectedFields14 }
        };

        /// <summary>
        /// Sample result with 2 billing entities each one with 4 custom fields.
        /// </summary>
        private IDictionary<string, IDictionary<string, string>> ExpectedBillingEntity24 => new Dictionary<string, IDictionary<string, string>>
        {
            { _beCode1, ExpectedFields14 },
            { _beCode2, ExpectedFields24 }
        };

        /// <summary>
        /// Expected result with 1 billing entity with 5 custom fields.
        /// </summary>
        private IDictionary<string, IDictionary<string, string>> ExpectedBillingEntity15 => new Dictionary<string, IDictionary<string, string>>
        {
            { _beCode1, ExpectedFields15 }
        };

        /// <summary>
        /// Sample result with 2 billing entities each one with 5 custom fields.
        /// </summary>
        private IDictionary<string, IDictionary<string, string>> ExpectedBillingEntity25 => new Dictionary<string, IDictionary<string, string>>
        {
            { _beCode1, ExpectedFields15 },
            { _beCode2, ExpectedFields25 }
        };

        #endregion

        #region Test Fields

        private OtbBillingEntityFieldDto FieldDto0 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName0,
            FieldValue = _fieldValue0,
            FieldOrder = 0,
            CommonName = string.Empty,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto1 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName1,
            FieldValue = _fieldValue1,
            FieldOrder = 1,
            CommonName = string.Empty,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto2 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName2,
            FieldValue = _fieldValue2,
            FieldOrder = 2,
            CommonName = string.Empty,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto3 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName3,
            FieldValue = _fieldValue3,
            FieldOrder = 3,
            CommonName = _commonName4,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto41 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName4,
            FieldValue = _fundingAccount1,
            FieldOrder = 1,
            CommonName = _commonName4,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto42 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName4,
            FieldValue = _fundingAccount2,
            FieldOrder = 1,
            CommonName = _commonName4,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto51 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName5,
            FieldValue = _returnAddress1,
            FieldOrder = 1,
            CommonName = string.Empty,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        private OtbBillingEntityFieldDto FieldDto52 => new OtbBillingEntityFieldDto
        {
            FieldName = _fieldName5,
            FieldValue = _returnAddress2,
            FieldOrder = 1,
            CommonName = string.Empty,
            EffectiveFromDate = _effectiveFromDate,
            EffectiveToDate = null
        };

        #endregion

        #region Test BEs

        private OtbBillingEntityDetailDto BillingEntityDetailDto14 => new OtbBillingEntityDetailDto
        {
            BillingEntityCode = _beCode1,
            Fields = new List<OtbBillingEntityFieldDto>
            {
                FieldDto0,
                FieldDto1,
                FieldDto2,
                FieldDto3,
                FieldDto41
            }
        };

        private OtbBillingEntityDetailDto BillingEntityDetailDto24 => new OtbBillingEntityDetailDto
        {
            BillingEntityCode = _beCode2,
            Fields = new List<OtbBillingEntityFieldDto>
            {
                FieldDto0,
                FieldDto1,
                FieldDto2,
                FieldDto3,
                FieldDto42
            }
        };

        private OtbBillingEntityDetailDto BillingEntityDetailDto15 => new OtbBillingEntityDetailDto
        {
            BillingEntityCode = _beCode1,
            Fields = new List<OtbBillingEntityFieldDto>
            {
                FieldDto0,
                FieldDto1,
                FieldDto2,
                FieldDto3,
                FieldDto41,
                FieldDto51
            }
        };

        private OtbBillingEntityDetailDto BillingEntityDetailDto25 => new OtbBillingEntityDetailDto
        {
            BillingEntityCode = _beCode2,
            Fields = new List<OtbBillingEntityFieldDto>
            {
                FieldDto0,
                FieldDto1,
                FieldDto2,
                FieldDto3,
                FieldDto42,
                FieldDto52
            }
        };

        #endregion

        #region Test Client BEs

        /// <summary>
        /// Sample result with 1 billing entity with 4 custom fields.
        /// </summary>
        private OtbBillingEntityClientDto BillingEntityClientDto14 => new OtbBillingEntityClientDto
        {
            OriginCode = _originCode,
            OriginName = _originName,
            ClientCode = _clientCode,
            BillingEntities = new List<OtbBillingEntityDetailDto>
            {
                BillingEntityDetailDto14,
            },
            EffectiveFromDate = _effectiveFromDate
        };

        /// <summary>
        /// Sample result with 2 billing entities each one with 4 custom fields.
        /// </summary>
        private OtbBillingEntityClientDto BillingEntityClientDto24 => new OtbBillingEntityClientDto
        {
            OriginCode = _originCode,
            OriginName = _originName,
            ClientCode = _clientCode,
            BillingEntities = new List<OtbBillingEntityDetailDto>
            {
                BillingEntityDetailDto14,
                BillingEntityDetailDto24
            },
            EffectiveFromDate = _effectiveFromDate
        };

        /// <summary>
        /// Sample result with 1 billing entity with 5 custom fields.
        /// </summary>
        private OtbBillingEntityClientDto BillingEntityClientDto15 => new OtbBillingEntityClientDto
        {
            OriginCode = _originCode,
            OriginName = _originName,
            ClientCode = _clientCode,
            BillingEntities = new List<OtbBillingEntityDetailDto>
            {
                BillingEntityDetailDto15,
            },
            EffectiveFromDate = _effectiveFromDate
        };

        /// <summary>
        /// Sample result with 2 billing entities each one with 5 custom fields.
        /// </summary>
        private OtbBillingEntityClientDto BillingEntityClientDto25 => new OtbBillingEntityClientDto
        {
            OriginCode = _originCode,
            OriginName = _originName,
            ClientCode = _clientCode,
            BillingEntities = new List<OtbBillingEntityDetailDto>
            {
                BillingEntityDetailDto15,
                BillingEntityDetailDto25
            },
            EffectiveFromDate = _effectiveFromDate
        };

        #endregion

        #endregion

        /// <summary>
        /// Tests for the constructor of <see cref="AddOtbOriginCommandHandler"/>
        /// </summary>
        public sealed class Constructor : OtbDataServiceTests
        {
            [Fact]
            public void WhenDataServiceSettingsIsNull_ShouldThrowArgumentNullException()
            {
                Action ctor = () => new OtbDataService(null!, _otbApi.Object, _logger);
                ctor.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("settings");
            }

            [Fact]
            public void WhenPlatformConfigApiIsNull_ShouldThrowArgumentNullException()
            {
                Action ctor = () => new OtbDataService(_settings, null!, _logger);
                ctor.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("otbApi");
            }

            [Fact]
            public void WhenLoggerIsNull_ShouldThrowArgumentNullException()
            {
                Action ctor = () => new OtbDataService(_settings, _otbApi.Object, null!);
                ctor.Should().ThrowExactly<ArgumentNullException>()
                    .Which.ParamName.Should().Be("logger");
            }
        }

        /// <summary>
        /// Tests for <see cref="OtbDataService.GetOtbDataAsync(string, System.Collections.Generic.ICollection{string})"/>
        /// </summary>
        public sealed class GetOtbDataAsync : OtbDataServiceTests
        {
            [Fact]
            public async Task WithInvalidClientCode_ShouldThrowArgumentNullException()
            {
                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                Func<Task> act = async () => await _service.GetOtbDataAsync(null);
                (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).And.ParamName.Should().Be("clientCode");
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            public async Task WithInvalidClientCode_ShouldThrowArgumentExceptionAsync(string clientCode)
            {
                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                Func<Task> act = async () => await _service.GetOtbDataAsync(clientCode);
                (await act.Should().ThrowExactlyAsync<ArgumentException>()).And.ParamName.Should().Be("clientCode");
            }

            [Fact]
            public async Task WithValidParamsNotUsingApi_ShouldNotLoadOtbData()
            {
                _settings.UseApi = false;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                var response = await _service.GetOtbDataAsync(_clientCode);

                using (new AssertionScope())
                {
                    response.ClientCode.Should().Be(string.Empty);
                    response.OriginCode.Should().Be(string.Empty);
                    response.OriginName.Should().Be(string.Empty);
                    response.BillingEntities.Should().BeEquivalentTo(EmptyBillingEntityList);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Never);
                }
            }

            [Fact]
            public async Task WithValidParamsUsingApi_ShouldLoadOtbData()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                var response = await _service.GetOtbDataAsync(_clientCode);

                using (new AssertionScope())
                {
                    response.ClientCode.Should().Be(_clientCode);
                    response.OriginCode.Should().Be(_originCode);
                    response.OriginName.Should().Be(_originName);
                    response.BillingEntities.Should().BeEquivalentTo(ExpectedBillingEntity14);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }
        }

        /// <summary>
        /// Tests for <see cref="OtbDataService.CompareOtbData(string, System.Collections.Generic.IDictionary{string, System.Collections.Generic.IDictionary{string, string}})"/>
        /// </summary>
        public sealed class CompareOtbData : OtbDataServiceTests
        {
            [Fact]
            public void WithInvalidClientCode_ShouldThrowArgumentNullException()
            {
                Action act = () => OtbDataComparer.CompareOtbData(null, EmptyBillingEntityList, EmptyBillingEntityList);
                act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("beCode");
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            public void WithInvalidClientCode_ShouldThrowArgumentException(string beCode)
            {
                Action act = () => OtbDataComparer.CompareOtbData(beCode, EmptyBillingEntityList, EmptyBillingEntityList);
                act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("beCode");
            }

            [Fact]
            public void WithInvalidBillingEntityList_ShouldThrowArgumentNullException()
            {
                Action act = () => OtbDataComparer.CompareOtbData(_clientCode, EmptyBillingEntityList, null);
                act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("otbFileData");
            }

            [Fact]
            public async Task WithSameSingleBeAndSameFieldsAsFile_ShouldReturnNoDifferences()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode1, response.BillingEntities, ExpectedBillingEntity14);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(0);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithSameMultipleBesAndSameFieldsAsFile_ShouldReturnNoDifferences()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto24);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode1, response.BillingEntities, ExpectedBillingEntity24);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(0);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithOneMoreBeThanFile_ShouldReturnOneDifference()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto24);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity14);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(1);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithOneLessBeThanFile_ShouldReturnOneDifference()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity24);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(1);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithSameSingleBeAndOneMoreFieldThanFile_ShouldReturnOneDifference()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto15);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode1, response.BillingEntities, ExpectedBillingEntity14);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(1);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithSameSingleBeAndOneLessFieldThanFile_ShouldReturnOneDifference()
            {
                _settings.UseApi = true;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(BillingEntityClientDto14);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode1, response.BillingEntities, ExpectedBillingEntity15);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(1);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithOneDiffFieldValueThanFile_ShouldReturnOneDifference()
            {
                _settings.UseApi = true;

                var billingEntityClient = BillingEntityClientDto25;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName4).FieldValue = _fundingAccount1;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(billingEntityClient);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity25);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(1);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithTwoDiffFieldValuesThanFile_ShouldReturnTwoDifferences()
            {
                _settings.UseApi = true;

                var billingEntityClient = BillingEntityClientDto25;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName4).FieldValue = _fundingAccount1;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName5).FieldValue = _returnAddress1;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(billingEntityClient);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity25);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(2);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithOneMoreFieldAndOneDiffFieldValueThanFile_ShouldReturnTwoDifferences()
            {
                _settings.UseApi = true;

                var billingEntityClient = BillingEntityClientDto25;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName4).FieldValue = _fundingAccount1;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(billingEntityClient);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity24);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(2);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }

            [Fact]
            public async Task WithOneMoreFieldAndTwoDiffFieldValuesThanFile_ShouldReturnThreeDifferences()
            {
                _settings.UseApi = true;

                var billingEntityClient = BillingEntityClientDto25;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName4).FieldValue = _fundingAccount1;
                billingEntityClient.BillingEntities
                    .First(b => b.BillingEntityCode == _beCode2).Fields
                    .First(f => f.FieldName == _fieldName3).FieldValue = _originName;

                _otbApi.Setup(mock => mock.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()))
                    .ReturnsAsync(billingEntityClient);

                var response = await _service.GetOtbDataAsync(_clientCode);

                var result = OtbDataComparer.CompareOtbData(_beCode2, response.BillingEntities, ExpectedBillingEntity24);

                using (new AssertionScope())
                {
                    result.Should().NotBeNull();
                    result.Count().Should().Be(3);

                    _otbApi.Verify(x => x.GetOtbClientBillingEntities(It.IsAny<string>(), It.IsAny<ICollection<string>>()), Times.Once);
                }
            }
        }
    }
}
