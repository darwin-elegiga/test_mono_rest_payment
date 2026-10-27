using System.IO.Abstractions;
using Xunit;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class FaxRetryArchiveMessageTests
    {
        private readonly IFileSystem _fileSystem = new FileSystem();

        private FaxRetryArchiveMessage SUT;

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(10, 10)]
        [InlineData(1, 10)]
        [InlineData(10, 1)]
        public void ShouldFormatRetryRatioCorrectly(int retriesRemaining, int maxRetries)
        {
            SUT = new FaxRetryArchiveMessage
            {
                RetriesRemaining = retriesRemaining,
                MaxRetries = maxRetries,
                DocumentPath = "/tmp/fake.ext",
                CorrespondenceId = 0
            };

            var retryRatio = $"{SUT.MaxRetries - SUT.RetriesRemaining + 1,2} of {SUT.MaxRetries + 1,2}";

            Assert.Equal($"Fax Retry {retryRatio}", SUT.Description.Substring(0, 18));

            Assert.Equal($"Fax Retry {retryRatio}", SUT.GetFileContents(0L).Substring(0, 18));

            Assert.Equal($"fake.0.fax_retry_{retryRatio.Replace(" ", "_")}.txt", SUT.GetRetryFileName(_fileSystem));
        }
    }
}
