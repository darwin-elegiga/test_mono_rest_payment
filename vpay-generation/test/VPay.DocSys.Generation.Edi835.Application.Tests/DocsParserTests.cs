using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using FluentAssertions;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using Xunit;

namespace VPay.DocSys.Generation.Edi835.Application.Tests
{
    public class DocsParserTests
    {
        private readonly DocsParserDirectSpec _DirectSpecSUT;
        private readonly DocsParserDualSpec _DualSpecSUT;
        private readonly IFileSystem _fileSystem = new FileSystem();
        private readonly Edi835ServiceSettings _edi835ServiceSettings;

        public DocsParserTests()
        {
            _edi835ServiceSettings = new Edi835ServiceSettings { DocsFolder = "docs" };
            _DirectSpecSUT = new DocsParserDirectSpec(_fileSystem, _edi835ServiceSettings);
            _DualSpecSUT = new DocsParserDualSpec(_fileSystem, _edi835ServiceSettings);
        }

        #region DirectSpec       
        [Fact]
        public void DirectSpec_LocateContent_Should_Succeed()
        {
            // Arrange
            var edi835 = new Data.Edi835
            {
                PaymentStatusFile = "B000000001_FSSWK00001.sts",
                TaxId = "999999999",
                OriginCode = "IC",
                ClientCode = "SWK",
                BillingEntityCode = "SWK0001001",
                UserKey = "A0019189742MEMAX07",
                TransactionId = 1004553741
            };
            var expectedFileName = "999999999_IC_SWK_SWK0001001_A0019189742MEMAX07_1004553741_NEW_00010101000000.docs";

            // Act
            var result = _DirectSpecSUT.LocateContent(edi835, CancellationToken.None).Result;

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().NotBeNullOrWhiteSpace();
            result.Filename.Should().Be(expectedFileName);
        }

        [Fact]
        public void DirectSpec_LocateContent_Should_Fail_IfNotFound()
        {
            // Arrange
            var edi835 = new Data.Edi835
            {
                PaymentStatusFile = "B000000001_FSSWK00001.sts",
                TaxId = "1234",
                OriginCode = "IC",
                ClientCode = "SWK",
                BillingEntityCode = "SWK0001001",
                UserKey = "A0019189742MEMAX07",
                TransactionId = 1004553741
            };

            // Act
            Action action = () => _DirectSpecSUT.LocateContent(edi835, CancellationToken.None);

            // Assert
            action.Should().Throw<FileNotFoundException>()
                .WithMessage("Unable to locate .docs file at expected location.");
        }
        #endregion

        #region DualSpec
        [Fact]
        public void DualSpec_LocateContent_Should_Succeed()
        {
            // Arrange
            var edi835 = new Data.Edi835
            {
                PaymentStatusFile = "B000000001_FSSWK00001.sts",
                TaxId = "999999999",
                OriginCode = "IC",
                ClientCode = "SWK",
                BillingEntityCode = "SWK0001001",
                UserKey = "A0019189742MEMAX07",
                TransactionId = 1004553741
            };
            var expectedFileName = "999999999_IC_SWK_SWK0001001_A0019189742MEMAX07_1004553741_NEW_00010101000000.docs";

            // Act
            var result = _DualSpecSUT.LocateContent(edi835, CancellationToken.None).Result;

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().NotBeNullOrWhiteSpace();
            result.Filename.Should().Be(expectedFileName);
        }

        [Fact]
        public void DualSpec_LocateContent_Should_Fail_IfNotFound()
        {
            // Arrange
            var edi835 = new Data.Edi835
            {
                PaymentStatusFile = "B000000001_FSSWK00001.sts",
                TaxId = "1234",
                OriginCode = "IC",
                ClientCode = "SWK",
                BillingEntityCode = "SWK0001001",
                UserKey = "A0019189742MEMAX07",
                TransactionId = 1004553741
            };

            // Act
            Action action = () => _DualSpecSUT.LocateContent(edi835, CancellationToken.None);

            // Assert
            action.Should().Throw<FileNotFoundException>()
                .WithMessage("Unable to locate .docs file at expected location.");
        }
        #endregion
    }
}
