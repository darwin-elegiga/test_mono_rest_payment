using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Inbound835Files;

namespace VPay.DocSys.Parsing
{
    public class Inbound835FileParser : IInbound835FileParser
    {
        private readonly IFileSystem _fileSystem;

        public Inbound835FileParser(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Inbound835FileType GetFileType(string path)
        {
            if(!string.IsNullOrEmpty(path) && _fileSystem.Path.HasExtension(path))
            {
                var fileExt = Path.GetExtension(path)?.ToUpper();

                if(fileExt == ".835")
                {
                    return Inbound835FileType.RemittanceAdvice;
                }
                else if(fileExt == ".BRN")
                {
                    return Inbound835FileType.BlueRemittanceNotification;
                }
                else if(fileExt == ".ACK")
                {
                    return Inbound835FileType.Acknowledgment;
                }
                else if(fileExt == ".TXT")
                {
                    // if the file name of a .txt file matches this regex expression
                    // its an Acknowledgment file; otherwise, its a PaymentPosted file
                    const string ackFileRegExExpression = @"^[\w-]*_NEW_\d{14}.txt$";

                    if(new Regex(ackFileRegExExpression).Match(_fileSystem.Path.GetFileName(path)).Success)
                    {
                        return Inbound835FileType.Acknowledgment;
                    }

                    return Inbound835FileType.PaymentPosted;
                }
            }

            return Inbound835FileType.Unknown;
        }

        public async Task<AcknowledgmentFile> ParseAcknowledgmentFile(string path, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(path, nameof(path));

            // verify file exists
            if(!_fileSystem.File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            // split out file name from path
            var fileName = _fileSystem.Path.GetFileName(path);
            var filePath = _fileSystem.Path.GetDirectoryName(path);
            var fullFileName = _fileSystem.Path.GetFullPath(path);

            // split file name into sections separated by underscore (_)
            var fileNameSections = fileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            // verify we have at least 7 sections with non-empty values
            if(fileNameSections.Length < 7 || fileNameSections.Any(_ => string.IsNullOrWhiteSpace(_)))
            {
                throw new Exception($"Invalid ACK file name - expected at least 7 underscore separated sections: {path}");
            }

            // PaymentID is the 5th section
            var paymentId = fileNameSections[4];

            // TransactionId is the 6th value as a long int
            if(!long.TryParse(fileNameSections[5], out var transactionId))
            {
                throw new Exception($"Invalid ACK file name - expected section 6 to be valid transaction id: {path}");
            }

            // type code is 7th section
            var typeCode = fileNameSections[6];

            // read file contents
            var contents = await _fileSystem.File.ReadAllTextAsync(path, cancellationToken);

            // split content on comma
            var lineSections = contents
                .Trim()
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            //  validate content and get time stamp and rejection code (content should be either an {time-stamp} or {trans-id,rejectionCode})
            var rejectionCode = string.Empty;
            var ackTimeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            if(lineSections.Length < 1 || lineSections.Length > 2 || lineSections[0].Any(c => !char.IsDigit(c)))
            {
                throw new Exception($"Invalid ACK file contents - expected ack time stamp or rejection code: {path}");
            }
            else if(lineSections.Length == 1)
            {
                ackTimeStamp = lineSections[0].SafeSubstring(0, 14);
            }
            else
            {
                rejectionCode = lineSections[1];
            }

            return new AcknowledgmentFile()
            {
                FilePath = filePath,
                FileName = fileName,
                FullFileName = fullFileName,
                PaymentId = paymentId,
                TransactionId = transactionId,
                TypeCode = typeCode,
                AckTimeStamp = ackTimeStamp,
                RejectionCode = rejectionCode,
                Contents = contents,
            };
        }

        public async Task<PaymentPostedFile> ParsePaymentPostedFile(string path, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(path, nameof(path));

            // verify file exists
            if(!_fileSystem.File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            var pmtPostedFile = new PaymentPostedFile();

            // split out file name from path
            pmtPostedFile.FileName = _fileSystem.Path.GetFileName(path);
            pmtPostedFile.FilePath = _fileSystem.Path.GetDirectoryName(path);
            pmtPostedFile.FullFileName = _fileSystem.Path.GetFullPath(path);

            // create dictionary to ensure only one line per transaction id
            var dictionary = new Dictionary<long, PaymentPostedFile.Line>();

            // keep a line index
            var lineIndex = 0;

            // open file and read in lines
            using(var stream = _fileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using(var reader = new StreamReader(stream))
            {
                while(!reader.EndOfStream)
                {
                    var lineText = await reader.ReadLineAsync();

                    if(!string.IsNullOrWhiteSpace(lineText))
                    {
                        var values = lineText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim()).ToArray();

                        if(!long.TryParse(values[0], out var transactionId))
                        {
                            throw new Exception($"Unexpected value at line {lineIndex + 1} of payment posted file: {path}");
                        }

                        var rejectionCode = values.Length > 1 ? values[1] : string.Empty;

                        dictionary[transactionId] = new PaymentPostedFile.Line(transactionId, rejectionCode);
                    }

                    lineIndex++;
                }
            }

            // assign lines from dictionary values
            pmtPostedFile.Lines = dictionary.Values.ToList();

            return pmtPostedFile;
        }

        public async Task<RemittanceAdviceFile> ParseRemittanceAdviceFile(string path, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(path, nameof(path));

            // verify file exists
            if(!_fileSystem.File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            // split out file name from path
            var fileName = _fileSystem.Path.GetFileName(path);
            var filePath = _fileSystem.Path.GetDirectoryName(path);
            var fullFileName = _fileSystem.Path.GetFullPath(path);

            // split file name into sections separated by underscore (_)
            var fileNameSections = fileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            // verify we have at least 5 sections with non-empty values
            if(fileNameSections.Length < 5 || fileNameSections.Any(_ => string.IsNullOrWhiteSpace(_)))
            {
                throw new Exception($"Invalid 835/brn file name - expected at least 5 underscore separated sections: {fileName}");
            }

            // tpa is the 2nd value
            var clientCode = fileNameSections[1];

            // TransactionId is the 4th value as a long int
            if(!long.TryParse(fileNameSections[3], out var transactionId))
            {
                throw new Exception($"Invalid 835/brn file name - expected section 4 to be valid transaction id: {fileName}");
            }

            // time stamp is the last value
            var timeStamp = fileNameSections[4];

            // read file contents
            var contents = await _fileSystem.File.ReadAllTextAsync(path, cancellationToken);

            return new RemittanceAdviceFile()
            {
                FilePath = filePath,
                FileName = fileName,
                FullFileName = fullFileName,
                TransactionId = transactionId,
                ClientCode = clientCode,
                TimeStamp = timeStamp,
                Contents = contents,
            };
        }

        public Task WritePaymentPostedFile(string path, IEnumerable<PaymentPostedFile.Line> lines, CancellationToken cancellationToken = default)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(path, nameof(path));
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(lines, nameof(lines));

            // NOTE: the iSeries program that reads these files expects them have CRLF
            //       line-endings and will not process them correctly with just LF

            var buffer = new StringBuilder();

            foreach(var line in lines)
            {
                if(string.IsNullOrEmpty(line.RejectionCode))
                {
                    buffer.Append($"{line.TransactionId:D20}\r\n");
                }
                else
                {
                    buffer.Append($"{line.TransactionId:D20},{line.RejectionCode}\r\n");
                }
            }

            return _fileSystem.File.WriteAllTextAsync(path, buffer.ToString(), cancellationToken);
        }
    }
}
