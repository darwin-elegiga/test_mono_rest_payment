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
    public class DocsExtractorDirectSpec : IDocsExtractor
    {
        private readonly string _headerRecord = "HR000";
        private readonly string _trailerRecord = "TR999";

        private readonly ITarExtractor _tarExtractor;
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly string _lineEnding;
        public DocsExtractorDirectSpec(Edi835ServiceSettings edi835ServiceSettings, ITarExtractor tarExtractor)
        {
            _lineEnding = edi835ServiceSettings.LineEnding;
            _tarExtractor = tarExtractor;
        }
        /// <inheritdoc/>
        public IEnumerable<Edi835ParsedDocsContent> ExtractDocs(string tarDataFilename, string inputFilename, List<DocumentTransactionMessage> messages)
        {
            var currentUserKey = string.Empty;
            var previousUserKey = string.Empty;
            var userKeysToFind = messages.Select(_ => _.StatusRecord.USERKEY.Trim());

            using (var tarInputStream = _tarExtractor.StreamTarEntry(tarDataFilename, inputFilename, _encoding))
            using (var reader = new StreamReader(tarInputStream, _encoding, false, 4096))
            {
                StringBuilder sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.Length < 3) { continue; }

                    string[] tokenizedLine = line.Split('|');
                    string recordType = tokenizedLine[0];
                    if (_headerRecord.Equals(recordType, StringComparison.OrdinalIgnoreCase)) { continue; }
                    if (_trailerRecord.Equals(recordType, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(previousUserKey))
                    {
                        var currentMessage = messages.FirstOrDefault(_ => _.StatusRecord.USERKEY.Trim().Equals(previousUserKey, StringComparison.OrdinalIgnoreCase));
                        reader.ReadToEnd();
                        yield return new Edi835ParsedDocsContent(currentMessage, sb.ToString());
                    }
                    currentUserKey = tokenizedLine[1];
                    if (string.IsNullOrWhiteSpace(previousUserKey)) { previousUserKey = currentUserKey; }
                    if (!userKeysToFind.Contains(currentUserKey, StringComparer.OrdinalIgnoreCase)) { continue; }

                    if (!currentUserKey.Equals(previousUserKey, StringComparison.OrdinalIgnoreCase) && sb.Length > 0)
                    {
                        var message = messages.FirstOrDefault(_ => _.StatusRecord.USERKEY.Trim().Equals(previousUserKey, StringComparison.OrdinalIgnoreCase));
                        yield return new Edi835ParsedDocsContent(message, sb.ToString());
                        sb = new StringBuilder();
                    }
                    sb.Append(line).Append(_lineEnding);
                    previousUserKey = currentUserKey;
                }
                reader.Close();
                tarInputStream.Close();
            }
        }
    }
}
