namespace VPay.DocSys.Generation.Configs
{
    public class DocSysSettings
    {
        public string ResultPath { get; set; } = "~/Results";
        public string WhiteBoxPdf { get; set; } = "/home/bogner/eob_merge/whiteBox.pdf";
        public bool AllowUnicode { get; set; } = true;
    }
}
