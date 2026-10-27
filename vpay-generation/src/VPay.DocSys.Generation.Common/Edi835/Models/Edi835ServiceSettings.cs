using System.IO.Abstractions;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class Edi835ServiceSettings
    {
        private readonly IFileSystem _fileSystem;

        public Edi835ServiceSettings()
        {
            _fileSystem = new FileSystem();
        }

        public Edi835ServiceSettings(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string DocsFolder { get; set; } = @"/SRVFS/bogner/Edi835/docs";
        public string LegacyDocsFolder { get; set; } = @"/SRVFS/bogner/transfer/835/docs";
        public string OutboundTarFolder { get; set; } = @"/SRVFS/bogner/transfer/835/outbound";
        public string LegacyTgzArchiveFolder { get; set; } = @"/SRVFS/bogner/transfer/835/archive";
        public TarFileTransferSettings TarFileTransfer { get; set; } = new TarFileTransferSettings();
        public string[] PdfFileNamesEndWith { get; set; } = new string[] {
            ".CHECK_cover_eob.pdf",
            "stapled.pdf",
            ".CLEAR_eob.pdf",
            ".EFT_cover_eob.pdf",
            ".NPF_cover_eob.pdf" };

        public string DocsArchiveFolder { get; set; } = @"/SRVFS/bogner/transfer/835/archive/docs";
        public bool ArchiveRedCardDocsAfterProcessing { get; set; }
        public string GetRedCardDocsArchiveFolder(System.DateTime loadDate)
        {
            return _fileSystem.Path.Combine(DocsArchiveFolder,
                loadDate.ToString("yyyy"),
                loadDate.ToString("MM"),
                loadDate.ToString("dd"));
        }
    }
}
