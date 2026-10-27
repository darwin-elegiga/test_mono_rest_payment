namespace VPay.DocSys.Parsing.Common
{
    public class Edi835ServiceSettings
    {
        /// <summary>
        /// Should status file parsing attempt to do anything with Edi835?
        /// </summary>
        /// <remarks>I decided to move this out of the generic "StatusFile" section of the configuration
        /// and into this settings, as this is where in makes sense to look for anything having to do with
        /// the parsing part of Edi835.  It's set to false, so that if the configuration is completely absent
        /// in the appsettings.json, it will get that as a default. It will not attempt to interrupt the flow
        /// of normal status file parsing (and potentially take down parsing altogether). You must explicitly add
        /// the key/value pair with true in order for Edi835 parsing to occur.</remarks>
        public bool EnableEdi835Parsing { get; set; } = false;

        /// <summary>
        /// The "bogner root"
        /// </summary>
        public string FileHome { get; set; } = @"/SRVFS/bogner/";
        /// <summary>
        /// Relative folder path under bogner where the tar files for
        /// the .sts files are stored.
        /// </summary>
        public string TarDataFolder { get; set; } = @"tarData/";
 
        /// <summary>
        /// Root directory where client raw files are stored.
        /// </summary>
        public string UploadFilesPath { get; set; } = @"/SRVFS/bogner/uploadFiles";

        public string TransferRoot { get; set; } = @"/SRVFS/bogner/transfer";

        /// <summary>
        /// Root folder where all the "new" Edi835 files are located.
        /// </summary>
        public string Edi835RootFolder { get; set; } = "transfer/835";

        /// <summary>
        /// During Parsing, SWK and HEQTN .docs will be extracted from the InputFile
        /// inside of the ?_processed.tar file, and saved with a deterministic name
        /// under this folder. (under Edi835Root)
        /// </summary>
        public string DocsFolder { get; set; } = "docs";

        /// <summary>
        /// Pull the flags for "IsActive" and "IsInactiveButGenerate" from the database tables.
        /// Once the Legacy/Rewrite transisition is complete, and we are
        /// completely ready to go full steam ahead, we can set this to false.
        /// </summary>
        public bool UseEdi835ConfigurationTable { get; set; } = true;


        public string LineEnding { get; set; } = "\n";
    }
}
