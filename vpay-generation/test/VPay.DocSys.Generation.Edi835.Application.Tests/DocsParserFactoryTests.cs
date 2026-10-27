using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Generation.Common.Edi835;
using Xunit;

namespace VPay.DocSys.Generation.Edi835.Application.Tests
{
    public class DocsParserFactoryTests
    {
        private readonly IDocsParserFactory _SUT;
        private readonly Mock<IEdi835Repository> _edi835Repository = new Mock<IEdi835Repository>();
        private readonly Mock<IServiceProvider> _serviceProvider = new Mock<IServiceProvider>();
        private readonly Mock<IServiceScope> _serviceScope = new Mock<IServiceScope>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();

        public DocsParserFactoryTests()
        {
            _edi835Repository
                .Setup(x => x.GetEdi835Configurations(CancellationToken.None))
                .Returns(Task.FromResult(Edi835ConfigurationData()));
            _serviceProvider
                .Setup(x => x.GetService(typeof(IEdi835Repository)))
                .Returns(_edi835Repository.Object);
            _serviceProvider
                .Setup(x => x.GetService(typeof(IFileSystem)))
                .Returns(new FileSystem());
            _serviceProvider
                .Setup(x => x.GetService(typeof(Edi835ServiceSettings)))
                .Returns(new Edi835ServiceSettings());
            _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);

            _SUT = new DocsParserFactory(_serviceProvider.Object, new Edi835ConfigurationCache(_serviceScopeFactory.Object));
        }

        [Theory]
        [InlineData("SWK", typeof(DocsParserSwk))]
        [InlineData("HEQTN", typeof(DocsParserHeqtn))]
        [InlineData("HPS", typeof(DocsParserDualSpec))]
        [InlineData("EHI", typeof(DocsParserDirectSpec))]
        public void GetDocsParser_Should_ReturnDocsParser(string clientCode, Type docsParserType)
        {
            // Act
            var result = _SUT.GetDocsParser(clientCode).Result;

            // Assert
            result.Should().NotBeNull().And.BeOfType(docsParserType);
        }

        private IList<Edi835Configuration> Edi835ConfigurationData()
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
    }
}
