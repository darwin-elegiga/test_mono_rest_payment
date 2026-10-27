using System;
using System.IO.Abstractions;

namespace VPay.DocSys.Parsing.Common
{
    public abstract class FileWriterBase
    {
        protected readonly IFileSystem _fileSystem;

        public FileWriterBase(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        protected string DateTimeStamp { get { return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); } }
    }
}
