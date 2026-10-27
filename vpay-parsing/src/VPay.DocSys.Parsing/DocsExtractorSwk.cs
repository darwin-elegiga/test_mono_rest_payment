using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class DocsExtractorSwk : IDocsExtractor
    {
        private readonly List<string> _headerRecords = new List<string> { "SWK", "HDR" };
        private readonly List<string> _trailerRecords = new List<string> { "TRL" };
        private readonly Encoding _encoding = Encoding.GetEncoding(1252);
        private readonly ITarExtractor _tarExtractor;

        private readonly string _lineEnding;

        public DocsExtractorSwk(Edi835ServiceSettings edi835ServiceSettings, ITarExtractor tarExtractor)
        {
            _lineEnding = edi835ServiceSettings.LineEnding;
            _tarExtractor = tarExtractor;
        }

        /// <inheritdoc/>
        public IEnumerable<Edi835ParsedDocsContent> ExtractDocs(string tarDataFilename, string inputFilename, List<DocumentTransactionMessage> messages)
        {
            var currentLineUserKey = string.Empty;
            var previousLineUserKey = string.Empty;

            var userKeysToFind = messages.Select(_ => _.StatusRecord.USERKEY.Trim());

            using (var tarInputStream = _tarExtractor.StreamTarEntry(tarDataFilename, inputFilename, _encoding))
            using (var reader = new StreamReader(tarInputStream, _encoding, false, 4096))
            {
                StringBuilder sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)
                        || line.Length < 3
                        || _headerRecords.Contains(line.Substring(0, 3)))
                    {
                        continue;
                    }
                    string recordType = line.Substring(0, 3);
                    if (_trailerRecords.Contains(recordType) && !string.IsNullOrWhiteSpace(previousLineUserKey))
                    {
                        var currentMessage = messages.FirstOrDefault(_ => _.StatusRecord.USERKEY.Trim().Equals(previousLineUserKey, StringComparison.OrdinalIgnoreCase));
                        yield return new Edi835ParsedDocsContent(currentMessage, sb.ToString());
                    }
                    currentLineUserKey = line.Substring(7, 20).Trim();
                    if (string.IsNullOrWhiteSpace(previousLineUserKey)) { previousLineUserKey = currentLineUserKey; }
                    if (!userKeysToFind.Contains(currentLineUserKey, StringComparer.OrdinalIgnoreCase)) { continue; }

                    if (!currentLineUserKey.Equals(previousLineUserKey, StringComparison.OrdinalIgnoreCase) && sb.Length > 0)
                    {
                        var message = messages.FirstOrDefault(_ => _.StatusRecord.USERKEY.Trim().Equals(previousLineUserKey, StringComparison.OrdinalIgnoreCase));
                        yield return new Edi835ParsedDocsContent(message, sb.ToString());
                        sb = new StringBuilder();
                    }
                    sb.Append(line).Append(_lineEnding);
                    previousLineUserKey = currentLineUserKey;
                }
                reader.Close();
                tarInputStream.Close();
            }
        }    
    }
}
