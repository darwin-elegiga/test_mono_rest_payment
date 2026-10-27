using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using VPay.DocSys.Generation.Common.Edi835;
using VPay.DocSys.Generation.Edi835.Application.Tests.TestModels;
using Xunit;

namespace VPay.DocSys.Generation.Edi835.Application.Tests.Edi835.Models
{
    public class Edi835ServiceSettingsTests
    {
        private MockRepository mockRepository;



        public Edi835ServiceSettingsTests()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);
 
        }

        private Edi835ServiceSettings CreateEdi835ServiceSettings()
        {
            return new Edi835ServiceSettings();
        }

        private Edi835ServiceSettings CreateEdi835ServiceSettings_IFileSystem()
        {
            return new Edi835ServiceSettings(new MoqFileSystem(new Dictionary<string, MockFileData>()));
        }

        [Fact]
        public void GetRedCardDocsArchiveFolder_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var edi835ServiceSettings = this.CreateEdi835ServiceSettings();
            DateTime loadDate = default;
            // Normalize line endings
            var expectedArchiveFolderEnding = $"{edi835ServiceSettings.DocsArchiveFolder}/{loadDate.ToString("yyyy/MM/dd")}".Replace("\\", "/");
            // Act
            var result = edi835ServiceSettings.GetRedCardDocsArchiveFolder(loadDate);
            // Normalize line endings
            result.Replace("\\", "/").Should().EndWith(expectedArchiveFolderEnding);
            // Assert
 
        }

        [Fact]
        public void GetRedCardDocsArchiveFolder_StateUnderTest_IFileSystem_ExpectedBehavior()
        {
            // Arrange
            var edi835ServiceSettings = this.CreateEdi835ServiceSettings_IFileSystem();
            DateTime loadDate = default;
            // Normalize line endings
            var expectedArchiveFolderEnding = $"{edi835ServiceSettings.DocsArchiveFolder}/{loadDate.ToString("yyyy/MM/dd")}".Replace("\\", "/");
            // Act
            var result = edi835ServiceSettings.GetRedCardDocsArchiveFolder(loadDate);
            // Normalize line endings
            result.Replace("\\", "/").Should().EndWith(expectedArchiveFolderEnding);
            // Assert

        }

        [Fact]
        public void ArchiveRedCardDocsAfterProcessing_StateUnderTest_ExpectedBehavior()
        {
            // SonarQube is a bit draconian in it's idea of what needs tests...
            var edi835ServiceSettings = this.CreateEdi835ServiceSettings();

            edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing = true;
            var result = edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing;
            result.Should().BeTrue();

            edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing = false;
            result = edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing;
            result.Should().BeFalse();
        }
        [Fact]
        public void ArchiveRedCardDocsAfterProcessing_StateUnderTest_IFileSystem_ExpectedBehavior()
        {
            // SonarQube is a bit draconian in it's idea of what needs tests...
            var edi835ServiceSettings = this.CreateEdi835ServiceSettings_IFileSystem();

            edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing = true;
            var result = edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing;
            result.Should().BeTrue();

            edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing = false;
            result = edi835ServiceSettings.ArchiveRedCardDocsAfterProcessing;
            result.Should().BeFalse();

        }

        

    }
}
