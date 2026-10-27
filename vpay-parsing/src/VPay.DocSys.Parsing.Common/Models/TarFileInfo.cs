using System.IO;
using System.IO.MemoryMappedFiles;

namespace VPay.DocSys.Parsing.Common
{
    public class TarFileInfo
    {
        private readonly MemoryMappedFile _mmfTarFile;

        public TarFileInfo(MemoryMappedFile mmfTarFile)
        {
            _mmfTarFile = mmfTarFile;
        }

        public long FilePos { get; set; }
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }

        public static StreamReader GetFileStream(MemoryMappedViewStream memoryMappedStream)
        {
            return new StreamReader(memoryMappedStream);
        }

        public void CopyTo(Stream copyToStream)
        {
            using var tarViewStream = GetViewStream();
            tarViewStream.CopyTo(copyToStream);
        }

        public MemoryMappedViewStream GetViewStream()
        {
            return _mmfTarFile.CreateViewStream(FilePos, Size);
        }

        public string GetTextValue()
        {
            using var tarViewStream = GetViewStream();
            using var reader = GetFileStream(tarViewStream);
            return reader.ReadToEnd();
        }
    }
}
