using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserRedCard : IEdi835Creator, IEdi835DocsLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly Edi835ServiceSettings _edi835ServiceSettings;

        public DocsParserRedCard(IFileSystem fileSystem, Edi835ServiceSettings edi835ServiceSettings ) 
        {
            _fileSystem = fileSystem;
            _edi835ServiceSettings = edi835ServiceSettings;
        }

        public async Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken = default)
        {
            string rcDocsFilename = Path.Combine(_edi835ServiceSettings.LegacyDocsFolder, $"{message.UserKey}_DD_0000000000.docs");
            string content = await _fileSystem.File.ReadAllTextAsync(rcDocsFilename).ConfigureAwait(false);
            var docsFile = new DocsFile { Content = content, Filename = message.GetFilename("docs") };
            return docsFile;

        }

        public async Task<string> CreateContent(Edi835CreationMessage message, CancellationToken cancellationToken = default)
        {
            string rcDocsFilename = Path.Combine(_edi835ServiceSettings.LegacyDocsFolder, $"{message.UserKey}_DD_0000000000.docs");
            if (!_fileSystem.File.Exists(rcDocsFilename))
            {
                // If the file is not present, throw an error and let the caller handle it
                throw new FileNotFoundException("Unable to find RedCard .docs file", rcDocsFilename);
            }
            string content = await _fileSystem.File.ReadAllTextAsync(rcDocsFilename).ConfigureAwait(false);
            return content;

        }
    }
}
