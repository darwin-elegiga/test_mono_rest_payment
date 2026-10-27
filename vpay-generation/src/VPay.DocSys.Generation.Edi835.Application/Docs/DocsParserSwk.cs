using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Common.Edi835;
using DS = VPay.DocSys.Data;

namespace VPay.DocSys.Generation.Edi835
{
    public class DocsParserSwk : IEdi835Creator, IEdi835DocsLocator
    {
        private readonly IFileSystem _fileSystem;
        private readonly Edi835ServiceSettings _edi835ServiceSettings;

        public DocsParserSwk(IFileSystem fileSystem, Edi835ServiceSettings edi835ServiceSettings)
        {
            _fileSystem = fileSystem;
            _edi835ServiceSettings = edi835ServiceSettings;
        }

        public Task<DocsFile> LocateContent(DS.Edi835 message, CancellationToken cancellationToken = default)
        {
            // Attempt to find the .docs file created and written out in parsing:

            var fileLocation = _fileSystem.Path.Combine(
                message.GetExpectedDocsFolder(_edi835ServiceSettings.DocsFolder),
                message.GetTempFilename("docs")
                );
            if (!_fileSystem.File.Exists(fileLocation))
            {
               throw new FileNotFoundException("Unable to locate .docs file at expected location.", fileLocation);
            }
            var content = _fileSystem.File.ReadAllText(fileLocation);
            var docsFile = new DocsFile { Content = content, Filename = message.GetFilename("docs") };
            return Task.FromResult(docsFile);
        }

        public async Task<string> CreateContent(Edi835CreationMessage message, CancellationToken cancellationToken = default)
        {
            if (!_fileSystem.File.Exists(message.RawFilePath))
            {
                throw new FileNotFoundException("Unable to find client raw file", message.RawFilePath);
            }

            StringBuilder sb = new StringBuilder();
            bool foundUserKey = false; // Lines are contiguous, so once we find the UserKey, we
            // only have to read lines until they no longer contain the UserKey

            // Even though the legacy python read this in as ISO-8859-1, the actual file has
            // character that only translate into the internal UTF-16 System.String byte array format
            // when read in as Codepage 1252. 
            var encoding = Encoding.GetEncoding(1252);
            var lineEnding = '\n';
            using (var memMap = MemoryMappedFile.CreateFromFile( message.RawFilePath, FileMode.Open))
            using (var viewStream = memMap.CreateViewStream())
            using (var reader = new StreamReader(viewStream, encoding, false, 4096))
            {
           
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    if ( line.Length < 20 ) { continue; }
                    var lineUserKey = line.Substring(7, 20).Trim().ToUpper();
                    if (!lineUserKey.Equals(message.UserKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (foundUserKey) { break; } else { continue; }
                    }
                    // If where get here, we've found the userkey we want at least once.
                    foundUserKey = true;
                    sb.Append(line).Append(lineEnding);
                }
                reader.Close();
                viewStream.Close();
                memMap.Dispose();
            }
            return sb.ToString();
        }
    }
}
