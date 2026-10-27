using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Moq;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Common;
using Xunit;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Data;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace VPay.DocSys.Parsing.Tests
{
    public class Edi835TarServiceTests
    {
        private readonly IEdi835TarService SUT;

        private readonly IFileSystem _fileSystem = new FileSystem();
        private readonly ILogger<Edi835TarService> _nullLogger = new NullLogger<Edi835TarService>();
        private readonly Mock<IMemoryCache> _memoryCache = new Mock<IMemoryCache>();
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly Edi835ConfigurationCache _cache;
        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();

        public Edi835TarServiceTests()
        {
            Edi835ServiceSettings edi835ServiceSettings = new Edi835ServiceSettings
            {
                EnableEdi835Parsing = true,
                FileHome = "",// Use the "current" folder...
                TarDataFolder = "edi835Data/tarData",
                Edi835RootFolder = "edi835Data",
                DocsFolder = "docs"
            };

            
            _edi835Repository.Setup(db => db
                  .Create835s(
                        It.IsAny<IEnumerable<long>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
                  .ReturnsAsync( (IEnumerable<long> txIds, string s1, string s2, CancellationToken ct) =>
                      txIds.Select(_=> new Edi835 { Edi835Id = _, TransactionId = _ }).ToList()
                      );

            _edi835Repository.Setup(db => db
                .GetEdi835Configurations(CancellationToken.None))
                .ReturnsAsync(SetupEdi835Configurations());
            _serviceProvider.Setup(x => x.GetService(typeof(IEdi835Repository))).Returns(_edi835Repository.Object);
            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);

            _cache = new Edi835ConfigurationCache(_serviceScopeFactory.Object);

            SUT = new Edi835TarService(edi835ServiceSettings, _cache, SetupDocsExtractors(), _edi835Repository.Object, _fileSystem, _nullLogger);
        }

        [Theory]
        [InlineData("edi835Data/B000000001_FSHEQTN001.sts",
            "edi835Data/docs/HEQTN/HEQTN01001/FSHEQTN001/", 2,
            "231714249_SEMED_HEQTN_HEQTN01001_38FO5VR_99838416273_DD_0000000000.docs",
            "860985433_SEMED_HEQTN_HEQTN01001_F3MUKBT_99838416248_DD_0000000000.docs" )]
        [InlineData("edi835Data/B000000002_FSSWK00001.sts",
            "edi835Data/docs/SWK/SWKPF01271/FSSWK00001/", 1,
            "270810641_IC_SWK_SWKPF01271_K0000040540MEMAX09_99805224793_DD_0000000000.docs" )]
        public async Task Sts_should_parse_and_save_docsAsync(string stsFilename, string folder, int edi835Ids, params string[] filenames)
        {
            // Ensure that everything was cleaned up to begin with (possible
            // that files may exists if the test was stopped during debug
            // or some other error occurred.

            for (int i = 0; i < filenames.Length; i++)
            {
                var tmpPath = Path.Combine(folder, filenames[i]);
                if (_fileSystem.File.Exists(tmpPath))
                {
                    _fileSystem.File.Delete(tmpPath);
                }
            }

            if (_fileSystem.Directory.Exists(folder))
            {
                _fileSystem.Directory.Delete(folder, true);
            }



            var messages = SUT.ParseSts(stsFilename);
            var edi835 = await SUT.ExtractDocs(messages, CancellationToken.None);
            edi835.Count.Should().Be(edi835Ids);
            _fileSystem.Directory.Exists(folder).Should().BeTrue();
            for (int i = 0; i < filenames.Length; i++)
            {
                var tmpPath = Path.Combine(folder, filenames[i]);
                _fileSystem.File.Exists(tmpPath).Should().BeTrue();
            }

            // Clean up after yourself.
            for (int i = 0; i < filenames.Length; i++)
            {
                var tmpPath = Path.Combine(folder, filenames[i]);
                if (_fileSystem.File.Exists(tmpPath))
                {
                    _fileSystem.File.Delete(tmpPath);
                }
            }

            if (_fileSystem.Directory.Exists(folder))
            {
                _fileSystem.Directory.Delete(folder, true);
            }
        }

        private IList<Edi835Configuration> SetupEdi835Configurations()
        {
            return new List<Edi835Configuration>
            {
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 1,
                    ClientCode = "SWK",
                    Parser = "SWK",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 2,
                    ClientCode = "HEQTN",
                    Parser = "HEQTN",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 3,
                    ClientCode = "RC",
                    Parser = "RC",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "EHI",
                    Parser = "DirectSpec",
                },
                new Edi835Configuration
                {
                    Edi835ConfigurationId = 4,
                    ClientCode = "HPS",
                    Parser = "DualSpec",
                }
            };
        }

        private IDocsExtractorFactory SetupDocsExtractors()
        {
            var mockDocsExtractorHEQTN = new Mock<IDocsExtractor>();
            mockDocsExtractorHEQTN
                .Setup(x => x.ExtractDocs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<DocumentTransactionMessage>>()))
                .Returns(new List<Edi835ParsedDocsContent>()
                {
                    new Edi835ParsedDocsContent
                    {
                        DocumentTransactionMessage = new DocumentTransactionMessage
                        {
                            StatusRecord = new StatusRecord
                            {
                                TIN = "231714249",
                                ORIGIN = "SEMED",
                                TPA = "HEQTN",
                                BILENT = "HEQTN01001",
                                USERKEY = "38FO5VR",
                                TXID = "99838416273"
                            },
                            TPA = "HEQTN",
                            BillingEntity = "HEQTN01001",
                            InputFile = "FSHEQTN001",
                            TransactionId = "54321"
                        },
                        Content = "TestDataHEQTN1"
                    },
                    new Edi835ParsedDocsContent
                    {
                        DocumentTransactionMessage = new DocumentTransactionMessage
                        {
                            StatusRecord = new StatusRecord
                            {
                                TIN = "860985433",
                                ORIGIN = "SEMED",
                                TPA = "HEQTN",
                                BILENT = "HEQTN01001",
                                USERKEY = "F3MUKBT",
                                TXID = "99838416248"
                            },
                            TPA = "HEQTN",
                            BillingEntity = "HEQTN01001",
                            InputFile = "FSHEQTN001",
                            TransactionId = "12345"
                        },
                        Content = "TestDataHEQTN2"
                    }
                });

            var mockDocsExtractorSWK = new Mock<IDocsExtractor>();
            mockDocsExtractorSWK
                .Setup(x => x.ExtractDocs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<DocumentTransactionMessage>>()))
                .Returns(new List<Edi835ParsedDocsContent>()
                {
                    new Edi835ParsedDocsContent
                    {
                        DocumentTransactionMessage = new DocumentTransactionMessage
                        {
                            StatusRecord = new StatusRecord
                            {
                                TIN = "270810641",
                                ORIGIN = "IC",
                                TPA = "SWK",
                                BILENT = "SWKPF01271",
                                USERKEY = "K0000040540MEMAX09",
                                TXID = "99805224793"
                            },
                            TPA = "SWK",
                            BillingEntity = "SWKPF01271",
                            InputFile = "FSSWK00001",
                            TransactionId = "1234"
                        },
                        Content = "TestDataSWK"
                    }
                });

            var mockFactory = new Mock<IDocsExtractorFactory>();
            mockFactory
                .Setup(x => x.GetDocsExtractor(It.Is<string>(val => val.Equals("HEQTN"))))
                .Returns(Task.FromResult(mockDocsExtractorHEQTN.Object));

            mockFactory
                .Setup(x => x.GetDocsExtractor(It.Is<string>(val => val.Equals("SWK"))))
                .Returns(Task.FromResult(mockDocsExtractorSWK.Object));

            return mockFactory.Object;
        }
    }
}
