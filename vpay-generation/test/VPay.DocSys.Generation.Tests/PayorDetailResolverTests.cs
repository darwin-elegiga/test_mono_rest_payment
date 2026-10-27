using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using VPay.Data.Db2.Abstractions.Fax;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Helpers;
using VPay.DocSys.Storage;

using Xunit;

namespace VPay.DocSys.Generation.Tests
{
#pragma warning disable CA1034 // Nested types should not be visible
    public class PayorDetailResolverTests
    {
        protected readonly Mock<IArchiveService> _archiveService = new Mock<IArchiveService>();

        protected readonly IPayorDetailResolver SUT;

        protected ClientVPaygeSettingsResponse _configurationResponse = new ClientVPaygeSettingsResponse();

        protected static string Key = PayorDetailResolver.PayorLookupName;
        protected static string XmlKey = "${eob_xml_dict.eob.LOOKUP}";
        protected static string XmlValue = "XML ONLY VALUE";

        protected PayorDetailResolverTests()
        {
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                         <eob_xml>
                          <eob>
                            <LOOKUP>{XmlValue}</LOOKUP>
                          </eob>
                        </eob_xml>";

            _archiveService
                .Setup(_ => _.GetArchiveDetailForTransactionIDAndFileName(It.IsAny<long>(), "eob.xml"))
                .ReturnsAsync(new ArchiveDetail { Data = GetBytes(xml) });

            SUT = new PayorDetailResolver(
                _archiveService.Object,
                new NullLogger<PayorDetailResolver>());
        }

        protected static byte[] GetBytes(string value) => value
            .ToCharArray()
            .Select(Convert.ToByte)
            .ToArray();

        public class WhenUsingBadData : PayorDetailResolverTests
        {
            public WhenUsingBadData() : base() { }

            [Fact]
            public void ShouldThrowExceptionsOnBadCtorParameters()
            {
                Assert.Throws<ArgumentNullException>(() => new PayorDetailResolver(null, null));
                Assert.Throws<ArgumentNullException>(() => new PayorDetailResolver(_archiveService.Object, null));
            }

            [Theory]
            [MemberData(nameof(InvalidParameters))]
            public async Task ShouldThrowExceptionOnInvalidParameters(Type exception, ClientType clientType, string tpa, string be, OtbDetailResponse otbDetailResponse, FieldDictionary fields)
            {
                await Assert.ThrowsAsync(exception, () => SUT.ResolvePayorDetails(clientType, tpa, be, 0, otbDetailResponse, fields));
            }

            [Fact]
            public async Task ShouldThrowExceptionOnInvalidClientType()
            {
                await Assert.ThrowsAsync<ArgumentException>(() => SUT.ResolvePayorDetails(ClientType.BOTH, "TEST", "TEST", 0, new OtbDetailResponse(), new FieldDictionary()));
            }

            public static IEnumerable<object[]> InvalidParameters
            {
                get
                {
                    yield return new object[] { typeof(ArgumentNullException), ClientType.MEDICAL, null, null, null, null, };
                    yield return new object[] { typeof(ArgumentException), ClientType.MEDICAL, "", null, null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.MEDICAL, " ", null, null, null };
                    yield return new object[] { typeof(ArgumentNullException), ClientType.MEDICAL, "TEST", null, null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.MEDICAL, "TEST", "", null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.MEDICAL, "TEST", " ", null, null };
                    yield return new object[] { typeof(ArgumentNullException), ClientType.MEDICAL, "TEST", "TEST", null, null };
                    yield return new object[] { typeof(ArgumentNullException), ClientType.MEDICAL, "TEST", "TEST", new OtbDetailResponse(), null };

                    yield return new object[] { typeof(ArgumentNullException), ClientType.WARRANTY, null, null, null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.WARRANTY, "", null, null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.WARRANTY, " ", null, null, null };
                    yield return new object[] { typeof(ArgumentNullException), ClientType.WARRANTY, "TEST", null, null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.WARRANTY, "TEST", "", null, null };
                    yield return new object[] { typeof(ArgumentException), ClientType.WARRANTY, "TEST", " ", null, null };
                    yield return new object[] { typeof(ArgumentNullException), ClientType.WARRANTY, "TEST", "TEST", null, null };
                }
            }
        }

        public class WhenGettingWarrantyPayeeData : PayorDetailResolverTests
        {
            private readonly OtbDetailResponse _response;

            public WhenGettingWarrantyPayeeData() : base()
            {
                _response = new OtbDetailResponse
                {
                    ClientFullName = "TEST Client Full Name",
                    ClientPhone = "TEST Client Phone",
                    VPayPhone = "TEST VPay Phone"
                };
            }

            [Fact]
            public async Task ShouldReturnCorrectPayorDetail()
            {
                var actual = await CallSut();

                Assert.Equal(_response.ClientFullName, actual.PayorName);
                Assert.Equal(_response.ClientPhone, actual.PayorPhone);
                Assert.Equal(_response.VPayPhone, actual.VPayPhone);
            }

            private Task<PayorDetail> CallSut() => SUT.ResolvePayorDetails(
                ClientType.WARRANTY,
                "TEST-TPA",
                "TEST-BE",
                0,
                _response,
                null);
        }

        public class WhenGettingMedicalPayeeData : PayorDetailResolverTests
        {
            public WhenGettingMedicalPayeeData() : base() { }

            [Fact]
            public async Task ShouldThrowExceptionForNullFieldDictionary()
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => SUT.ResolvePayorDetails(ClientType.MEDICAL, "TEST TPA", "TEST BE", 0, new OtbDetailResponse(), null));
            }

            [Theory]
            [MemberData(nameof(GetResolveDirectOtbTestData))]
            [MemberData(nameof(GetResolveToCommonTestData))]
            [MemberData(nameof(GetResolveToOtbTestData))]
            [MemberData(nameof(GetResolveToOverrideTestData))]
            [MemberData(nameof(GetResolveToVCardToOtbTestData))]
            [MemberData(nameof(GetNullVCardTestData))]
            [MemberData(nameof(GetNullCommonTestData))]
            public async Task ShouldUseConfigDictionaryCorrectlyWithOtbEntry(
                ClientVPaygeSettingsResponse configuration,
                OtbDetailResponse otbDetail,
                string contactName,
                bool useEob)
            {
                var fields = SUT.CreateFieldDictionary(otbDetail, configuration);

                var actual = await SUT.ResolvePayorDetails(ClientType.MEDICAL, "TEST TPA", "TEST BE", 0, otbDetail, fields);

                Assert.Equal(contactName, actual.PayorName);

                _archiveService.Verify(_ =>
                    _.GetArchiveDetailForTransactionIDAndFileName(
                       It.IsAny<long>(),
                       It.IsAny<string>()),
                    useEob ? Times.Once() : Times.Never());
            }

            [Theory]
            [MemberData(nameof(GetInvalidMedicalTestData))]
            public async Task ShouldUseOtbDetailFallBackForBadConfiguration(
                ClientVPaygeSettingsResponse configuration,
                string contactName,
                bool useEob)
            {
                var otbDetail = new OtbDetailResponse
                {
                    ClientFullName = "SHOULD ALWAYS GET THIS"
                };

                var fields = SUT.CreateFieldDictionary(otbDetail, configuration);

                var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <eob_xml>
                              <eob>
                              </eob>
                            </eob_xml>";

                _archiveService
                    .Setup(_ => _.GetArchiveDetailForTransactionIDAndFileName(It.IsAny<long>(), "eob.xml"))
                    .ReturnsAsync(new ArchiveDetail { Data = GetBytes(xml) });

                var actual = await SUT.ResolvePayorDetails(ClientType.MEDICAL, "TEST TPA", "TEST BE", 0, otbDetail, fields);

                Assert.Equal(otbDetail.ClientFullName, actual.PayorName);
                Assert.NotEqual(contactName, actual.PayorName);

                _archiveService.Verify(_ => _.GetArchiveDetailForTransactionIDAndFileName(It.IsAny<long>(), It.IsAny<string>()), useEob ? Times.Once() : Times.Never());
            }

            public static IEnumerable<object[]> GetResolveDirectOtbTestData
            {
                get
                {
                    var contactName = "THIS IS THE END RESULT";

                    var baseField = new FieldConfiguration
                    {
                        Name = Key,
                        Source = FieldConfiguration.SourceEnum.Variable,
                        Type = FieldConfiguration.TypeEnum.String,
                    };

                    var otbDetail = new OtbDetailResponse
                    {
                        BillingEntityCode = "TEST BE CODE",
                        BillingEntityFullName = "TEST BE FULL NAME",
                        ClientCode = "TEST CLIENT CODE",
                        ClientFullName = "TEST CLIENT FULL NAME",
                        ClientPhone = "TEST PHONE",
                        OriginCode = "TEST ORIGIN CODE",
                        OriginName = "TEST ORIGIN NAME",
                        VPayPhone = "TEST VPAY PHONE"
                    };

                    otbDetail.BillingEntityCode = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.BillingEntity)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.BillingEntity)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);

                    otbDetail.BillingEntityCode = "TEST BE CODE";
                    otbDetail.BillingEntityFullName = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.BillingEntityName)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.BillingEntityName)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);

                    otbDetail.BillingEntityFullName = "TEST BE FULL NAME";
                    otbDetail.ClientCode = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.TPA_Name)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.TPA_Name)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);

                    otbDetail.ClientCode = "TEST CLIENT CODE";
                    otbDetail.ClientFullName = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.TPA_Full_Name)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.TPA_Full_Name)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);

                    otbDetail.ClientFullName = "TEST CLIENT FULL NAME";
                    otbDetail.OriginCode = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.Origin)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.Origin)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);

                    otbDetail.OriginCode = "TEST ORIGIN CODE";
                    otbDetail.OriginName = contactName;
                    baseField.Value = $"on behalf of ${{OTB_ENTRY.{nameof(OtbEntry.OriginName)}}}";
                    yield return GetTest(baseField, contactName, otbDetail);

                    baseField.Value = $"${{OTB_ENTRY.{nameof(OtbEntry.OriginName)}}} is expediting";
                    yield return GetTest(baseField, contactName, otbDetail);
                }
            }

            public static IEnumerable<object[]> GetResolveToOverrideTestData
            {
                get
                {
                    var contactName = "THIS IS THE END RESULT";

                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common = new List<FieldConfiguration>{
                                    baseField,
                                    GetField("LOOKUP", contactName, FieldConfiguration.SourceEnum.Literal)
                                },
                                VCard = new List<FieldConfiguration>{
                                    GetField("LOOKUP", contactName + contactName, FieldConfiguration.SourceEnum.Literal)
                                },
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : contactName + contactName,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetResolveToCommonTestData
            {
                get
                {
                    var contactName = "THIS IS THE END RESULT";

                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common =  new List<FieldConfiguration>{
                                    baseField,
                                    GetField("LOOKUP", contactName, FieldConfiguration.SourceEnum.Literal)
                                },
                                VCard =  new List<FieldConfiguration>{ },
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : contactName,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetResolveToOtbTestData
            {
                get
                {
                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common =  new List<FieldConfiguration>{
                                    baseField,
                                    GetField("LOOKUP", $"${{OTB_ENTRY.{nameof(OtbEntry.Origin)}}}")
                                },
                                VCard =  new List<FieldConfiguration>{ },
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : otbDetail.OriginCode,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetResolveToVCardToOtbTestData
            {
                get
                {
                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common =  new List<FieldConfiguration>{
                                    baseField,
                                },
                                VCard =  new List<FieldConfiguration>{
                                    GetField("LOOKUP", "${LOOKUP2}"),
                                    GetField("LOOKUP2", $"${{OTB_ENTRY.{nameof(OtbEntry.Origin)}}}")
                                },
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : otbDetail.OriginCode,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetNullVCardTestData
            {
                get
                {
                    var contactName = "THIS IS THE END RESULT";

                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common = new List<FieldConfiguration>{
                                    baseField,
                                    GetField("LOOKUP", contactName, FieldConfiguration.SourceEnum.Literal)
                                },
                                VCard = null,
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : contactName,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetNullCommonTestData
            {
                get
                {
                    var contactName = "THIS IS THE END RESULT";

                    var otbDetail = new OtbDetailResponse
                    {
                        ClientFullName = "SHOULD NEVER GET THIS",
                        OriginCode = "ORIGIN CODE"
                    };

                    foreach (var kvp in GetBaseTestData)
                    {
                        var baseField = new FieldConfiguration
                        {
                            Name = Key,
                            Source = FieldConfiguration.SourceEnum.Variable,
                            Type = FieldConfiguration.TypeEnum.String,
                            Value = kvp.Key,
                        };

                        yield return new object[]
                        {
                            new ClientVPaygeSettingsResponse
                            {
                                Common = null,
                                VCard = new List<FieldConfiguration>{
                                    baseField,
                                    GetField("LOOKUP", contactName, FieldConfiguration.SourceEnum.Literal) },
                            },
                            otbDetail,
                            kvp.Value ? XmlValue : contactName,
                            kvp.Value
                        };
                    }
                }
            }

            public static IEnumerable<object[]> GetInvalidMedicalTestData
            {
                get
                {
                    var baseField = new FieldConfiguration
                    {
                        Name = Key,
                        Source = FieldConfiguration.SourceEnum.Variable,
                        Type = FieldConfiguration.TypeEnum.String,
                        Value = "${LOOKUP} is expediting",
                    };

                    var contactName = "SHOULD NEVER GET THIS";

                    // null dictionaries
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common = null,
                            VCard =  null,
                        },
                        contactName,
                        false
                    };

                    // no dictionary entries
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common = new List<FieldConfiguration>{ },
                            VCard =  new List<FieldConfiguration>{ },
                        },
                        contactName,
                        false
                    };

                    // variable, with no lookup entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField
                            },
                            VCard =  new List<FieldConfiguration>{ },
                        },
                        contactName,
                        false
                    };

                    // one level deep, no secondary entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                                GetField("LOOKUP", "${LOOKUP2}")
                            },
                            VCard =  new List<FieldConfiguration>{ },
                        },
                        contactName,
                        false
                    };

                    // incorrect type, not a string
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common = new List<FieldConfiguration>
                            {
                                GetField(baseField.Name, contactName, FieldConfiguration.SourceEnum.Literal, FieldConfiguration.TypeEnum.List)
                            },
                            VCard = new List<FieldConfiguration>{ },
                        },
                        contactName,
                        false
                    };

                    // one level deep, no otb entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                                GetField("LOOKUP", "${OTB_ENTRY.BadOtbKey}")
                            },
                            VCard = new List<FieldConfiguration>{ },
                        },
                        contactName,
                        false
                    };

                    // one level deep, no otb entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                                GetField("LOOKUP", "${eob_xml_dict.BadEobKey}")
                            },
                            VCard = new List<FieldConfiguration>{ },
                        },
                        contactName,
                        true
                    };

                    // one level deep, null override entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                            },
                            VCard =  new List<FieldConfiguration>
                            {
                                GetField(baseField.Name, null, FieldConfiguration.SourceEnum.Literal),
                            },
                        },
                        contactName,
                        false
                    };

                    // one level deep, empty override entry
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                            },
                            VCard =  new List<FieldConfiguration>
                            {
                                GetField(baseField.Name, "", FieldConfiguration.SourceEnum.Literal),
                            },
                        },
                        contactName,
                        false
                    };

                    // one level deep, xml key
                    yield return new object[]
                    {
                        new ClientVPaygeSettingsResponse
                        {
                            Common =  new List<FieldConfiguration>
                            {
                                baseField,
                                GetField("LOOKUP", XmlKey)
                            },
                        },
                        contactName,
                        true
                    };
                }
            }

            private static object[] GetTest(FieldConfiguration baseField, string contactName, OtbDetailResponse otbDetail)
            {
                return new object[]
                {
                    new ClientVPaygeSettingsResponse
                    {
                        Common = new List<FieldConfiguration>{
                            baseField,
                            GetField($"{{{baseField.Value}}}", contactName, FieldConfiguration.SourceEnum.Literal)
                        },
                        VCard = new List<FieldConfiguration>(0),
                    },
                    otbDetail,
                    contactName,
                    false
                };
            }

            private static IEnumerable<KeyValuePair<string, bool>> GetBaseTestData
            {
                get
                {
                    // direct or otb resolves
                    yield return new KeyValuePair<string, bool>("${LOOKUP} is expediting", false);
                    yield return new KeyValuePair<string, bool>("${LOOKUP} EXTRA !@# TEXT 123 HERE is expediting", false);
                    yield return new KeyValuePair<string, bool>("on behalf of ${LOOKUP}", false);

                    // eob resolves
                    yield return new KeyValuePair<string, bool>($"{XmlKey} is expediting", true);
                    yield return new KeyValuePair<string, bool>($"{XmlKey} EXTRA !@# TEXT 123 HERE is expediting", true);
                    yield return new KeyValuePair<string, bool>($"on behalf of {XmlKey}", true);
                }
            }

            private static FieldConfiguration GetField(string name, string value, FieldConfiguration.SourceEnum source = FieldConfiguration.SourceEnum.Variable, FieldConfiguration.TypeEnum type = FieldConfiguration.TypeEnum.String)
            {
                return new FieldConfiguration
                {
                    Name = name,
                    Source = source,
                    Type = type,
                    Value = value
                };
            }
        }

        public class WhenUsingRegexToFindVariableInBody1 : PayorDetailResolverTests
        {
            public WhenUsingRegexToFindVariableInBody1() : base() { }

            [Theory]
            // ABC, AVH, MTM, NAT, PLR
            [InlineData("${contact_name} is expediting", "contact_name")]
            // ACB, AEG, BPA, ENT, LHO, MBA, NEL, RMA, YRK, 
            [InlineData("${be_name} is expediting", "be_name")]
            // AFX, HEQPF, HEQTN, NOV
            [InlineData("on behalf of ${tpa_full_name}", "tpa_full_name")]
            // default, AMF
            [InlineData("${tpa_full_name} is expediting", "tpa_full_name")]
            // CCI
            [InlineData("${representative_name} is expediting", "representative_name")]
            // CCM
            [InlineData("${OTB_ENTRY.TPA_Name} is expediting", "OTB_ENTRY.TPA_Name")]
            // EBMC
            [InlineData("${tpa_short_name} is expediting", "tpa_short_name")]
            // HS1, IASM, SCM, TLC
            [InlineData("${OTB_ENTRY.BillingEntityName} is expediting", "OTB_ENTRY.BillingEntityName")]
            // LHO
            [InlineData("${be_name}, a wholly-owned subsidiary of Blue Cross Blue Shield of Kansas City is expediting", "be_name")]
            // NDB
            [InlineData("${OTB_ENTRY.TPA_Full_Name}, dental plan administrator, is expediting this payment to you, on behalf of ${OTB_ENTRY.BillingEntityName}", "OTB_ENTRY.BillingEntityName")]
            // PBS
            [InlineData("${representative_name} on behalf of ${OTB_ENTRY.BillingEntityName} is expediting", "representative_name")]
            // SWK
            [InlineData("on behalf of ${eob_xml_dict.eob.return_address1}", "eob_xml_dict.eob.return_address1")]
            // TASB
            [InlineData("&lt;font name='Corbel'&gt;${be_name}&lt;/font&gt; is expediting ", "be_name")]
            // UCH
            [InlineData("${OTB_ENTRY.TPA_Full_Name} on behalf of ${OTB_ENTRY.BillingEntityName} is expediting", "OTB_ENTRY.TPA_Full_Name")]
            public void ShouldFindCorrectVariable(string body, string expected)
            {
                var result = PayorDetailResolver.PayorLookupValueRegex.Matches(body);
                var actual = result.GetLastSuccess();

                Assert.Equal(expected, actual);
            }
        }
    }
#pragma warning restore CA1034 // Nested types should not be visible
}

