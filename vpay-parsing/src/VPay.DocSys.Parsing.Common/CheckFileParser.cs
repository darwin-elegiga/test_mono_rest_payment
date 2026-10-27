using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class CheckFileParser : ICheckFileParser
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<CheckFileParser> _logger;

        public CheckFileParser(
            IFileSystem fileSystem,
            ILogger<CheckFileParser> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IList<CheckInfo> ParseCheckFileAsync(string checkFilePath, bool isCustomSendBackTpa, bool isRedCardTpa)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(checkFilePath, nameof(checkFilePath));

            _logger.LogInformation("Check Parse Started.");

            if (!isCustomSendBackTpa && isRedCardTpa)
            {
                // RedCard will not have any check data, instead check info is set in EOB XML
                _logger.LogInformation("No check file required for RedCard TPA");
                return new CheckInfo[] { };
            }

            if (!_fileSystem.File.Exists(checkFilePath))
            {
                _logger.LogAndThrowError($"{checkFilePath} not found");
            }

            // normal processing, assumes file exists
            using (var reader = new StreamReader(checkFilePath))
            {
                var deserializer = new XmlSerializer(typeof(CheckFile));
                var checkFile = (CheckFile)deserializer.Deserialize(reader);
                reader.Close();

                _logger.LogInformation("Check Parse Complete.");
                return checkFile.GetCheckInfo();
            }
        }
    }
}
