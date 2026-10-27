namespace VPay.DocSys.Generation.Models
{
    public class ProcessorSettings
    {
        public int SleepTimeInSeconds { get; set; }
        public int MaxRetryCount { get; set; }
        public int GarbageCollectionInterval { get; set; } = 10;
    }
}
