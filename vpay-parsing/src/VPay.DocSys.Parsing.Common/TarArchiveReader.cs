using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;

namespace VPay.DocSys.Parsing.Common
{
    public class TarArchiveReader
    {
        public TarArchiveReader(MemoryMappedFile mmTarFile, MemoryMappedViewStream tarStream, string tarFileName, long offset = 0L)
        {
            ArchiveMap = mmTarFile;
            ArchiveFileName = tarFileName;
            Files = new List<TarFileInfo>();

            using var processedTar = new TarInputStream(tarStream, null);
            ReadArchiveFiles(processedTar, offset);
        }

        public TarArchiveReader(MemoryMappedFile mmTarFile, string tarFileName, long fileSize, long offset = 0L)
        {
            ArchiveMap = mmTarFile;
            ArchiveFileName = tarFileName;
            Files = new List<TarFileInfo>();

            using var tarStream = mmTarFile.CreateViewStream(0, fileSize);
            using var processedTar = new TarInputStream(tarStream, null);
            ReadArchiveFiles(processedTar, offset);
        }

        public string ArchiveFileName { get; }
        public MemoryMappedFile ArchiveMap { get; }
        public List<TarFileInfo> Files { get; }

        private void ReadArchiveFiles(TarInputStream tarStream, long offset)
        {
            var currentPosition = 0L;
            TarEntry tarEntry;

            while ((tarEntry = tarStream.GetNextEntry()) != null)
            {
                //Advance the pointer past the header info for the tar entry
                currentPosition += TarBuffer.BlockSize;
                if (tarEntry.Name.Length > 100)
                {
                    currentPosition += TarBuffer.BlockSize * 2;
                }

                Files.Add(new TarFileInfo(ArchiveMap)
                {
                    FilePos = currentPosition + offset,
                    Name = tarEntry.Name,
                    Size = tarEntry.Size,
                    IsDirectory = tarEntry.IsDirectory
                });

                //Advance the pointer by the number of full blocks the tar entry is taking up in the file
                currentPosition +=
                    (long)Math.Ceiling(tarEntry.Size / (decimal)TarBuffer.BlockSize) *
                    TarBuffer.BlockSize;
            }
        }
    }
}
