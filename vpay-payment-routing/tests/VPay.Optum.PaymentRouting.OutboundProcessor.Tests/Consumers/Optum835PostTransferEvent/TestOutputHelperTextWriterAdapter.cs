using System.IO;
using System.Text;
using Xunit;

namespace VPay.Optum.PaymentRouting.OutboundProcessor.Tests.Consumers.Outbound835
{
    // <summary>
    ///     A <see cref="TextWriter" /> implementation that will output to the <see cref="ITestOutputHelper" />.
    /// </summary>
    public class TestOutputHelperTextWriterAdapter : TextWriter
    {
        private readonly ITestOutputHelper _output;

        public TestOutputHelperTextWriterAdapter(ITestOutputHelper output) =>
            _output = output;

        public override Encoding Encoding { get; } = Encoding.UTF8;

        public bool Enabled { get; set; } = true;

        public override void WriteLine(string value)
        {
            if (Enabled)
            {
                _output.WriteLine(value);
            }
        }
    }
}
