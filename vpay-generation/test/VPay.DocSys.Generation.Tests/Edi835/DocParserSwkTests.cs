using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Edi835;
using VPay.DocSys.Generation.Models;
using Xunit;

namespace VPay.DocSys.Generation.Edi835.ApplicationTests
{
    public class DocParserSwkTests
    {
        public DocParserSwkTests()
        {
            // Not sure why, but tests don't have an understanding of what codepages are available...
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public async Task DocsParserSwk_CreateContent_Should_CreateIdenticalFileToLegacyWhenFoundInRawFile()
        {
            // NOTE - Patient Names have been scrubbed from the data, but otherwise, these are straight from prod values.
            var expectedFilename = "Edi835/270810641_IC_SWK_SWKPF01271_K0000040540MEMAX09_805224793_NEW_20200602165259.docs";
            var expectedContent = File.ReadAllText(expectedFilename).Replace("\r", "");
            DocsParserSwk parser = new DocsParserSwk(new FileSystem(), new Common.Edi835.Edi835ServiceSettings());
            Edi835CreationMessage message = new Edi835CreationMessage
            {
                RawFilePath = "Edi835/swk-rawfile-contains-835.txt",
                UserKey = "K0000040540memax09"
            };
            var actualContent = (await parser.CreateContent(message)).Replace("\r", "");

            Assert.NotNull(actualContent);
            // StringBuilder AppendLine add the Environment.NewLine.  It's a serious PITA to change it for just a unit test
            // This workaround will not affect the file on a linux platform.
            //actualContent = actualContent.Replace("\r", "");
            Assert.Equal(expectedContent, actualContent);
        }
    }
}
