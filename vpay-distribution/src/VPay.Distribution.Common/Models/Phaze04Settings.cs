using System.IO;

namespace VPay.Distribution.Common
{
    public class Phaze04Settings
    {
        public string ScriptName { get; set; }

        public string ScriptPath { get; set; }

		public string GetFullFileName()
		{
			return Path.Combine(this.ScriptPath ?? string.Empty, this.ScriptName ?? string.Empty);
		}
    }
}
