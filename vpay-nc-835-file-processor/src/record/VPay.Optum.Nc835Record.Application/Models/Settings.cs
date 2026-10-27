namespace VPay.Optum.Nc835Record.Application.Models
{
    public class Settings
    {
        public string RecordEncryptionKey { get; set; } = "";

        public int PrefetchCount { get; set; } = 1;
    }
}
