using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Generation.Common.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserDirectSpec : IEdi835DocsLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly Edi835ServiceSettings _edi835ServiceSettings;

        public DocsParserDirectSpec(IFileSystem fileSystem, Edi835ServiceSettings edi835ServiceSettings)
        {
            _fileSystem = fileSystem;
            _edi835ServiceSettings = edi835ServiceSettings;
        }

        public Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken)
        {
            // Attempt to find the .docs file created and written out in parsing:
            var fileLocation = _fileSystem.Path.Combine(
                message.GetExpectedDocsFolder(_edi835ServiceSettings.DocsFolder),
                message.GetTempFilename("docs")
                );
            if (!_fileSystem.File.Exists(fileLocation))
            {
                throw new FileNotFoundException($"Unable to locate .docs file at expected location.", fileLocation);
            }
            var content = _fileSystem.File.ReadAllText(fileLocation);
            var docsFile = new DocsFile { Content = content, Filename = message.GetFilename("docs") };
            return Task.FromResult(docsFile);
        }
    }
}
