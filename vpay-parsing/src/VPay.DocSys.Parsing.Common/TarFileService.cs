using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Linq;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing.Common
{
    public class TarFileService : ITarFileService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TarFileService> _logger;

        public TarFileService(
            IFileSystem fileSystem,
            ILogger<TarFileService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public List<Guid> CreateTarFile(string tarFilePath, List<TarFileRecord> tarRecords)
        {
            ArgumentHelpers.ThrowIf.IsNullOrWhiteSpace(tarFilePath, nameof(tarFilePath));
            ArgumentHelpers.ThrowIf.IsNull(tarRecords, nameof(tarRecords));

            var statusDictionary = new List<Guid>();

            if (!tarRecords.Any())
            {
                _logger.LogInformation($"No files to insert, tar file {tarFilePath} not created");

                return statusDictionary;
            }

            _logger.LogInformation($"Creating {tarFilePath} to add files");

            using (var file = _fileSystem.File.Create(tarFilePath))
            using (var tarStream = new TarOutputStream(file, null))
            {
                // https://github.com/icsharpcode/SharpZipLib/issues/14
                // also added explicit CloseEntry and Finish calls
                tarStream.IsStreamOwner = false;

                foreach (var tarRecord in tarRecords)
                {
                    var closeStream = false;

                    try
                    {
                        using (var fileStream = new MemoryStream(tarRecord.Data, false))
                        {
                            var entry = TarEntry.CreateTarEntry(tarRecord.TarRecordName);

                            entry.Size = fileStream.Length;

                            tarStream.PutNextEntry(entry);

                            closeStream = true;

                            fileStream.CopyTo(tarStream);
                        }

                        _logger.LogInformation($"Added {tarRecord.FileName} using {tarRecord.Guid} and added to {tarFilePath} as {tarRecord.TarRecordName}");

                        statusDictionary.Add(tarRecord.Guid);
                    }
                    catch (NotSupportedException ex)
                    {
                        _logger.LogError(ex, $"Error trying to copy {tarRecord.FileName} stream to {tarFilePath}");
                    }

                    //required to call manually
                    if (closeStream) { tarStream.CloseEntry(); }
                }

                tarStream.Finish();
            }

            _logger.LogInformation($"Finish adding {tarRecords.Count} files to {tarFilePath}");

            return statusDictionary;
        }

        public List<TarFileRecord> GetAllFiles(string tarFilePath)
        {
            var tarFileRecords = new List<TarFileRecord>();

            if (!_fileSystem.File.Exists(tarFilePath))
                throw new FileNotFoundException("File not found.", tarFilePath);

            var tarFileSize = _fileSystem.FileInfo.New(tarFilePath).Length;

            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(tarFilePath, FileMode.Open))
            using (var memoryMappedViewStream = memoryMappedFile.CreateViewStream(0, tarFileSize, MemoryMappedFileAccess.Read))
            using (var tarInputStream = new TarInputStream(memoryMappedViewStream, null))
            {
                var tarEntry = null as TarEntry;

                while ((tarEntry = tarInputStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    var fileName = tarEntry.Name.Replace('/', Path.DirectorySeparatorChar);

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(fileName))
                        fileName = fileName.Substring(Path.GetPathRoot(fileName).Length);

                    using (var memoryStream = new MemoryStream())
                    {
                        tarInputStream.CopyEntryContents(memoryStream);

                        tarFileRecords.Add(new TarFileRecord()
                        {
                            Guid = new Guid(),
                            FileName = fileName,
                            TarRecordName = fileName,
                            Data = memoryStream.ToArray(),
                        });
                    }
                }

                tarInputStream.Close();
            }

            return tarFileRecords;
        }

        public IEnumerable<TarFileRecord> EnumerateAllFiles(string tarFilePath)
        {
            if (!_fileSystem.File.Exists(tarFilePath))
            {
                throw new FileNotFoundException("File not found.", tarFilePath);
            }

            var tarFileSize = _fileSystem.FileInfo.New(tarFilePath).Length;

            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(tarFilePath, FileMode.Open))
            using (var memoryMappedViewStream = memoryMappedFile.CreateViewStream(0, tarFileSize, MemoryMappedFileAccess.Read))
            using (var tarInputStream = new TarInputStream(memoryMappedViewStream, null))
            {
                TarEntry tarEntry;

                while ((tarEntry = tarInputStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    var fileName = tarEntry.Name.Replace('/', Path.DirectorySeparatorChar);

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(fileName))
                        fileName = fileName.Substring(Path.GetPathRoot(fileName).Length);

                    using (var memoryStream = new MemoryStream())
                    {
                        tarInputStream.CopyEntryContents(memoryStream);

                        yield return new TarFileRecord
                        {
                            Guid = Guid.NewGuid(),
                            FileName = fileName,
                            TarRecordName = fileName,
                            Data = memoryStream.ToArray(),
                        };
                    }
                }

                tarInputStream.Close();
            }
        }
    }
}
