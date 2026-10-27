using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Xml.Serialization;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Helpers;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Parsing.Common.Rabbitmq;
using VPay.DocSys.Storage;

namespace VPay.DocSys.Parsing
{
    public class TarService : ITarService
    {
        private readonly ILogger _logger;
        private readonly string _fileDirectory;
        private string _statusFolder;

        private readonly FileHandlingPolicyHolder _fileHandlingPolicies;

        private readonly IRabbitMqPublisherFactory _rabbitMqPublisherFactory;
        private readonly DocumentTransactionQueueSettings _queueSettings;

        public TarService(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRabbitMqPublisherFactory rabbitMqPublisherFactory)
        {
            _logger = loggerFactory.CreateLogger<TarService>();
            _fileDirectory = configuration.GetSection("StatusFile").GetValue<string>("FileHome") +
                             configuration.GetSection("StatusFile").GetValue<string>("TarFolder");
            _fileHandlingPolicies = new FileHandlingPolicyHolder(_logger);
            _queueSettings = new DocumentTransactionQueueSettings();
            configuration.Bind("DocumentQueue", _queueSettings);
            _rabbitMqPublisherFactory = rabbitMqPublisherFactory;
        }

        /// <summary>
        /// Parse the tar file with xml EOB data and associate it with the status file info for the transaction
        /// </summary>
        /// <param name="messages">Status information in message format</param>
        /// <param name="folder">Folder to find tar file</param>
        /// <param name="isRedCard">Boolean value for Red Card</param>
        /// <param name="isEmergencyFile">Used to set priority on queue messages</param>
        /// <param name="saveMessages">Used to specify if we should save messages</param>
        public void ParseTarAndPublishMessages(List<DocumentTransactionMessage> messages, string folder, bool isRedCard,
            bool? isEmergencyFile = false, bool? saveMessages = false)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (!messages.Any())
            {
                return;
            }

            var eobData = new Dictionary<string, TarFileInfo>();
            var tarFileName = _fileDirectory + messages.First().InputFile + "_processed.tar";
            var fileSize = new FileInfo(tarFileName).Length;
            _statusFolder = folder;
            _logger.LogInformation("Start Processing tar file [{TarFileName}]", tarFileName);

            using (var mmTarFile = MemoryMappedFile.CreateFromFile(tarFileName, FileMode.Open))
            {
                var destination = Directory.CreateDirectory(
                    Path.Combine(_statusFolder,
                        Path.GetFileName(tarFileName).Split(new[] { '_' })[0]
                    ));

                var tarArchive = new TarArchiveReader(mmTarFile, tarFileName, fileSize);
                var driverFile = GetDriverFile(tarArchive);
                var hasEOB = GetHasEOB(tarArchive);

                if (hasEOB)
                {
                    ProcessInnerTar(tarArchive, eobData, driverFile, destination, messages, isEmergencyFile.Value, saveMessages.Value);
                }

                // some red card tars are different
                // re open tar file to grab all eob.xml files from archive/eob folder
                if (!hasEOB && isRedCard)
                {
                    foreach (var tarEntry in tarArchive.Files)
                    {
                        if (!tarEntry.IsDirectory &&
                            !eobData.Keys.Contains(tarEntry.Name) &&
                            tarEntry.Name.Contains("/eob/") &&
                            tarEntry.Name.EndsWith(Constants.FileExtensions.EobXml,
                                StringComparison.InvariantCultureIgnoreCase))
                        {
                            var fileName = Path.GetFileName(tarEntry.Name);

                            _logger.LogDebug(
                                $"Adding [{fileName}] to eobData dictionary for tar file [{tarFileName}]");
                            eobData.Add(fileName, tarEntry);
                        }
                    }
                }

                _logger.LogInformation("Start Processing EOB Data for tar file [{TarFileName}]", tarFileName);
                try
                {
                    using (_logger.BeginScope(new Dictionary<string, object>
                    {
                        { "TarFileName", tarFileName }
                    }))
                    {
                        ComposeEOBData(messages, driverFile, eobData, destination.FullName);

                        _fileHandlingPolicies.RemoveDirectoryPolicy.Execute(() =>
                        {
                            Directory.Delete(destination.FullName, true);
                        });
                    }
                }
                catch (Exception ex) when (ex.LogWithScope(_logger))
                {
                    throw;
                }

                _logger.LogInformation("Completed Processing EOB Data for tar file [{TarFileName}]", tarFileName);
                _logger.LogInformation("Completed Processing tar file for tar file [{TarFileName}]", tarFileName);
            }
        }

        private static bool GetHasEOB(TarArchiveReader tarArchive)
        {
            return tarArchive.Files.Any(f => f.Name.Contains("archive") && f.Name.EndsWith(".xml.tar"));
        }

        private DriverFile GetDriverFile(TarArchiveReader tarFile)
        {
            foreach (var tarEntry in tarFile.Files.Where(f => f.Name.EndsWith(".drv")))
            {
                _logger.LogInformation("Located driver file [{DriverFileName}] for tar file [{TarFileName}]",
                    tarEntry.Name, tarFile.ArchiveFileName);

                using var mmStream = tarEntry.GetViewStream();
                using var reader = TarFileInfo.GetFileStream(mmStream);
                var deserializer = new XmlSerializer(typeof(DriverFile));
                return deserializer.Deserialize(reader) as DriverFile;
            }

            return null;
        }

        private void ProcessInnerTar(TarArchiveReader tarArchive, Dictionary<string, TarFileInfo> eobData,
            DriverFile driverFile, DirectoryInfo destination, List<DocumentTransactionMessage> messages, bool isEmergencyFile, bool saveMessages)
        {
            var tarFile = tarArchive.Files.FirstOrDefault(f => f.Name.Contains("archive") && f.Name.EndsWith(".xml.tar"));
            if (tarFile == null)
            {
                // This really should never happen unless the tar file was somehow changed while running or the GetHasEOB method breaks
                var errorMessage = $"Unable to find tar file inside archive {tarArchive.ArchiveFileName}";
                _logger.LogError(errorMessage);
                throw new Exception(errorMessage);
            }

            using var nestedTar = tarFile.GetViewStream();
            var nestedArchive = new TarArchiveReader(tarArchive.ArchiveMap, nestedTar, tarFile.Name, tarFile.FilePos);

            foreach (var eobEntry in nestedArchive.Files.Where(f => f.Name.EndsWith("pdf") || f.Name.EndsWith("xml")))
            {
                //Some transactions may share the same PDF. We only need one copy
                if (eobData.Keys.Contains(eobEntry.Name))
                    continue;

                if (eobEntry.Name.EndsWith("pdf"))
                {
                    using var fs = File.Create(Path.Combine(destination.FullName, Path.GetFileName(eobEntry.Name)));

                    eobEntry.CopyTo(fs);
                    fs.Flush();
                    fs.Close();
                    eobData.Add(eobEntry.Name, eobEntry);
                }
                else
                {
                    //Find driver file record for tar entry
                    var driverRecord = driverFile.Items.FirstOrDefault(di =>
                        di.file_name == eobEntry.Name || di.full_file_name == eobEntry.Name);

                    //If there is no PDF to load we have the data we need to publish
                    if (driverRecord != null && string.IsNullOrEmpty(driverRecord.pdf_name))
                    {
                        var msg = messages.FirstOrDefault(m => m.GUID == driverRecord.uuid);
                        if (msg == null)
                        {
                            continue;
                        }

                        msg.EOBData = new EOB
                        {
                            Name = eobEntry.Name,
                            XML = eobEntry.GetTextValue()
                        };

                        if (saveMessages)
                        {
                            SaveMessage(_statusFolder, msg);
                        }

                        PublishMessage(msg);

                        messages.Remove(msg);
                        // Free up the used memory since some XML files are large.
                        msg = null;
                        GC.Collect();
                    }
                    //Need to store XML and until we have we have PDF to publish
                    else
                    {
                        eobData.Add(eobEntry.Name, eobEntry);
                    }
                }
            }
        }

        private void ComposeEOBData(List<DocumentTransactionMessage> messages, DriverFile driverFile,
            Dictionary<string, TarFileInfo> eobData, string path, bool isEmergencyFile = false)
        {
            foreach (var msg in messages.Where(m => m.EOBData == null))
            {
                _logger.LogDebug(
                    "Processing driver file record for message Guid = {GUID} TransactionId = {TransactionId} RefCode = {RefCode}",
                    msg.GUID, msg.StatusRecord.TXID.RemoveLeadingZeroes(), msg.StatusRecord.TRANID);
                string name;

                var msgValue = driverFile?.Items.FirstOrDefault(e => e.uuid == msg.GUID);
                if (msgValue != null)
                {
                    name = msgValue.full_file_name ?? msgValue.file_name ?? string.Empty;
                }
                else
                {
                    name = eobData.Keys.First(k => k.Contains(msg.GUID));
                }

                var xmlText = string.Empty;

                if (!string.IsNullOrEmpty(name))
                {
                    xmlText = eobData[name].GetTextValue();
                }

                var pdfName = msgValue?.pdf_name ?? string.Empty;

                var base64PDF = Base64Pdf(path, pdfName);

                msg.EOBData = new EOB
                {
                    Name = name,
                    XML = xmlText,
                    PDF = base64PDF
                };

                PublishMessage(msg);
                _logger.LogDebug("Queued  Guid = {GUID} TransactionId = {TransactionId} RefCode = {RefCode}",
                    msg.GUID, msg.StatusRecord.TXID.RemoveLeadingZeroes(), msg.StatusRecord.TRANID);
            }
            eobData.Clear();
            eobData = null;

            //Because EOB data can contain large XML and PDF data we want to clean up any temporary copies as soon as we can.
            GC.Collect();
        }

        private void PublishMessage(DocumentTransactionMessage msg)
        {
            using (var msgQueue = _rabbitMqPublisherFactory.GetPublisher(_queueSettings, _logger))
            {
                msgQueue.PublishUsingMessageRepository(msg);
            }
        }

        private static string Base64Pdf(string path, string pdfName)
        {
            string base64PDF = null;
            if (!string.IsNullOrWhiteSpace(pdfName))
            {
                using var eobStream = new FileStream(Path.Combine(path, pdfName), FileMode.Open);
                using var reader = new BinaryReader(eobStream);

                var pdfBytes = reader.ReadBytes(Convert.ToInt32(eobStream.Length));
                base64PDF = Convert.ToBase64String(pdfBytes);
                reader.Close();
                eobStream.Close();
            }

            return base64PDF;
        }

        private static void SaveMessage(string statusFilePath, DocumentTransactionMessage message)
        {
            var statusFolder = Path.GetDirectoryName(statusFilePath) + "/";
            var statusFileName = Path.GetFileNameWithoutExtension(statusFilePath);
            var msgFolder = statusFolder + statusFileName + "_Messages";
            Directory.CreateDirectory(msgFolder);
            using var msgFile = File.CreateText(msgFolder + $"/{message.TransactionId}.json");
            var serializer = new JsonSerializer();
            serializer.Serialize(msgFile, message);
        }
    }
}
