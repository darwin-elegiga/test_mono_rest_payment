using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class TarExtractor : ITarExtractor
    {
        private readonly IFileSystem _fileSystem;
        public TarExtractor(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public MemoryStream StreamTarEntry(string tarFilePath, string entryFilename, Encoding encoding)
        {
            if (!_fileSystem.File.Exists(tarFilePath))
            {
                throw new FileNotFoundException("File not found.", tarFilePath);
            }
            // Normalize the entry name so, regardless of Windows Testing or Linux pod, we get the same name
            entryFilename = entryFilename.Replace('/', Path.DirectorySeparatorChar);
            var tarFileSize = _fileSystem.FileInfo.New(tarFilePath).Length;
            bool entryFound = false;

            MemoryStream stream = new MemoryStream();

            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(tarFilePath, FileMode.Open))
            using (var memoryMappedViewStream = memoryMappedFile.CreateViewStream(0, tarFileSize, MemoryMappedFileAccess.Read))
            using (var tarInputStream = new TarInputStream(memoryMappedViewStream, encoding))
            {
                TarEntry tarEntry;
                while ((tarEntry = tarInputStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory) { continue; }

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    var tarEntryFileName = tarEntry.Name.Replace('/', Path.DirectorySeparatorChar);

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(tarEntryFileName))
                    {
                        tarEntryFileName = tarEntryFileName.Substring(Path.GetPathRoot(tarEntryFileName).Length);
                    }
                    if (entryFilename.Equals(tarEntryFileName, StringComparison.OrdinalIgnoreCase))
                    {

                        tarInputStream.CopyEntryContents(stream);
                        stream.Position = 0;
                        entryFound = true;
                        break;
                    }

                }
                tarInputStream.Close();
            }
            if (!entryFound)
            {
                throw new FileNotFoundException($"TarEntryFilename {entryFilename} not found in TarFile {tarFilePath}", tarFilePath);
            }

            return stream;

        }
    }
}
