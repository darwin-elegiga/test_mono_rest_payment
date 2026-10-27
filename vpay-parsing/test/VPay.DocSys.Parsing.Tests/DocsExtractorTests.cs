using FluentAssertions;
using Moq;
using VPay.DocSys.Parsing.Common;
using Xunit;
using System.IO.Abstractions;
using VPay.DocSys.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Collections.Generic;
using VPay.DocSys.Common;
using Microsoft.Extensions.DependencyInjection;

namespace VPay.DocSys.Parsing.Tests
{
    public class DocsExtractorTests
    {
        private static readonly Edi835ServiceSettings _edi835ServiceSettings = new Edi835ServiceSettings
        {
            EnableEdi835Parsing = true,
            FileHome = "",// Use the "current" folder...
            TarDataFolder = "edi835Data/tarData",
            Edi835RootFolder = "edi835Data",
            DocsFolder = "docs",
            LineEnding = "\n"
        };

        private readonly ITarExtractor _tarExtractor;
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly IEdi835TarService _edi835TarService;
        private readonly IDocsExtractorFactory _docsExtractorFactorySUT;
        private readonly Edi835ConfigurationCache _cache;
        private readonly IFileSystem _fileSystem = new FileSystem();
        private readonly ILogger<Edi835TarService> _nullLogger = new NullLogger<Edi835TarService>();

        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();


        public DocsExtractorTests()
        {
            _tarExtractor = new TarExtractor(_fileSystem);

            _edi835Repository.Setup(db => db
                .GetEdi835Configurations(CancellationToken.None))
                .ReturnsAsync(SetupEdi835Configurations());

            _serviceProvider.Setup(sp => sp
                .GetService(typeof(IEdi835Repository)))
                .Returns(_edi835Repository.Object);
            _serviceProvider.Setup(sp => sp
                .GetService(typeof(Edi835ServiceSettings)))
                .Returns(_edi835ServiceSettings);
            _serviceProvider.Setup(sp => sp
                .GetService(typeof(ITarExtractor)))
                .Returns(_tarExtractor);

            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);            

            _cache = new Edi835ConfigurationCache(_serviceScopeFactory.Object);
            _docsExtractorFactorySUT = new DocsExtractorFactory(_serviceProvider.Object, _cache);
            _edi835TarService = new Edi835TarService(_edi835ServiceSettings, _cache, _docsExtractorFactorySUT, _edi835Repository.Object, _fileSystem, _nullLogger);
        }


        [Theory]
        [InlineData("edi835Data/B000000001_FSHEQTN001.sts",
            "edi835Data/tarData/FSHEQTN001_processed.tar",
            "FSHEQTN001_processed/FSHEQTN001",
            Edi835Parser.Heqtn)]
        [InlineData("edi835Data/B000000002_FSSWK00001.sts",
            "edi835Data/tarData/FSSWK00001_processed.tar",
            "FSSWK00001_processed/FSSWK00001",
            Edi835Parser.Swk)]
        [InlineData("edi835Data/B000000003_FSEHI00001.sts",
            "edi835Data/tarData/FSEHI00001_processed.tar",
            "FSEHI00001_processed/FSEHI00001",
            Edi835Parser.DirectSpec)]
        [InlineData("edi835Data/B000000003_FSEHI00001.sts",
            "edi835Data/tarData/FSEHI00001_processed.tar",
            "FSEHI00001_processed/FSEHI00001",
            Edi835Parser.DualSpec)]
        public void DocsExtractor_Should_ExtractDocs(string statusFullPath, string tarFullPath, string inputFileName, string parser)
        {
            // Arrange
            var messages = _edi835TarService.ParseSts(statusFullPath);
            var sut = GetDocsExtractorObject(parser);

            // Act
            var docList = sut.ExtractDocs(tarFullPath, inputFileName, messages);

            // Assert
            foreach (var doc in docList)
            {
                doc.Should().NotBeNull();
                doc.Content.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Theory]
        [InlineData("SWK", typeof(DocsExtractorSwk))]
        [InlineData("HEQTN", typeof(DocsExtractorHeqtn))]
        [InlineData("EHI", typeof(DocsExtractorDirectSpec))]
        [InlineData("HPS", typeof(DocsExtractorDualSpec))]
        public void DocsExtractorFactory_Should_CreateDocsExtractor(string clientCode, Type extractorType)
        {
            // Act
            var result = _docsExtractorFactorySUT.GetDocsExtractor(clientCode).Result;

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType(extractorType);
        }

        #region Test Setup/Utility
        private IDocsExtractor GetDocsExtractorObject(string parser)
        {
            return parser switch
            {
                Edi835Parser.Swk => new DocsExtractorSwk(_edi835ServiceSettings, _tarExtractor),
                Edi835Parser.Heqtn => new DocsExtractorHeqtn(_edi835ServiceSettings, _tarExtractor),
                Edi835Parser.DirectSpec => new DocsExtractorDirectSpec(_edi835ServiceSettings, _tarExtractor),
                Edi835Parser.DualSpec => new DocsExtractorDualSpec(_edi835ServiceSettings, _tarExtractor),
                Edi835Parser.RedCard => null,
                _ => null
            };
        }

        private static IList<Edi835Configuration> SetupEdi835Configurations()
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
                    Edi835ConfigurationId = 5,
                    ClientCode = "HPS",
                    Parser = "DualSpec",
                }
            };
        }
        #endregion
    }
}
