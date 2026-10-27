using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common.Extensions;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class AuxiliaryFileWriter : FileWriterBase, IAuxiliaryFileWriter
    {
        private readonly ILogger _logger;
        private readonly IRedCardRepository _redCardRepository;

        public AuxiliaryFileWriter(
            ILogger<AuxiliaryFileWriter> logger,
            IRedCardRepository redCardRepository,
            IFileSystem fileSystem) : base(fileSystem)
        {
            _logger = logger;
            _redCardRepository = redCardRepository;
        }

        public async Task<string> Write(string statusFilePath, long fileId, StatusFile statusFile, bool isReissue, bool checkAuxFileGenerated = true, bool updateStatus = true)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                {"STSFile", statusFilePath}
            }))
            {
                if (checkAuxFileGenerated && await _redCardRepository.GetFileHasStatus(fileId, RedCardFileStatusCode.AuxiliaryFileGenerated))
                {
                    _logger.LogAndThrowError($".AUX already created for file {statusFilePath.Replace(Environment.NewLine, "")}.");
                }

                var auxiliaryFile = statusFilePath.GetAuxiliaryFilePathFromStatusFilePath();

                _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(statusFilePath));

                _logger.LogInformation($"Creating {auxiliaryFile}");

                using (var stream = _fileSystem.File.CreateText(auxiliaryFile))
                {
                    _logger.LogInformation($"Writing to {auxiliaryFile}");
                    Write(stream, statusFile, isReissue);
                }

                if (updateStatus)
                {
                    _logger.LogInformation($"Updating RedCardFileStatus record to {nameof(RedCardFileStatusCode.AuxiliaryFileGenerated)} for {statusFilePath.Replace(Environment.NewLine, "")}");

                    await _redCardRepository.UpdateFileStatus(fileId, RedCardFileStatusCode.AuxiliaryFileGenerated);
                }

                return auxiliaryFile;
            }
        }

        private void Write(TextWriter textWriter, StatusFile statusFile, bool isReissue)
        {
            var archiveName = GetArchiveName(statusFile.Header);

            textWriter.WriteLine(CreateAuxiliaryHeader(archiveName));

            foreach (var r in statusFile.Records)
            {
                textWriter.WriteLine(CreateAuxiliaryLine(r, statusFile.Header, isReissue));
            }

            textWriter.WriteLine(CreateAuxiliaryFooter(statusFile.Records.Count));
        }

        private string GetArchiveName(StatusHeader header)
        {
            return $"{header.InputFile}.{header.CDate}";
        }

        private string CreateAuxiliaryHeader(string archiveName)
        {
            return $"00|{DateTimeStamp}|MASTER-ARCHIVE.{archiveName}|";
        }

        private string CreateAuxiliaryLine(StatusRecord record, StatusHeader header, bool isReissue)
        {
            var coverName = null as string;

            if (isReissue)
            {
                coverName = $"{header.InputFile}.{record.GUID}{record.PAYTYPE.ToRedCardFileExtension(true)}";
            }
            else
            {
                coverName = $"{header.CDate}.{header.InputFile}.{record.GUID}{record.PAYTYPE.ToRedCardFileExtension(false)}";
            }

            return $"33|{record.TXID.RemoveLeadingZeroes()}|{record.GUID}|{coverName}|{record.PAYTYPE}|{record.DIST}|";
        }

        private string CreateAuxiliaryFooter(int count)
        {
            return $"99|{DateTimeStamp}|{count}|";
        }
    }
}
