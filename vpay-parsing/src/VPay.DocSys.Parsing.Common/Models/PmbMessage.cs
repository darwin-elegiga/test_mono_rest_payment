using System;
using System.Text;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class PmbMessage
    {
        public string MessageType { get; } = "PMB";

        public Guid MessageId { get; } = Guid.NewGuid();

        public DateTime CreatedTimeStamp { get; } = DateTime.Now;

        public string Version { get; } = "00.00.0001";

        public string Priority { get; set; } = "0000000000";

        public string HopCount { get; set; } = "";

        public string OriginalFormatId { get; set; } = "";

        public string OriginalMessageId { get; set; } = "n/a";

        public string Padding { get; set; } = "";

        public string TypeOfMessage { get; set; } = "I";

        public string SeverityLevel { get; set; } = "99";

        public string MessageIndex { get; set; } = "000000";

        public string MfiFamily { get; set; } = null;

        public string MfiVersion { get; set; } = "0001";

        public string FromSystem { get; set; }

        public string ToSystem { get; set; }

        public string Family { get; set; }

        public string OriginAction { get; set; }

        public string Payload { get; set; }

        public override string ToString()
        {
            return new StringBuilder()

            .Append(Format(MessageType, 3))
            .Append(Format(Version, 10))
            .Append(Format(FromSystem, 20))
            .Append(Format(ToSystem, 20))
            .Append(Format(Family, 20))
            .Append(Format(Priority, 10))
            .Append(Format(MfiFamily ?? Family, 3))
            .Append(Format(TypeOfMessage, 1))
            .Append(Format(SeverityLevel, 2))
            .Append(Format(MessageIndex, 6))
            .Append(Format(MfiVersion, 4))
            .Append(Pad(4))
            .Append(Format(MessageId, 32))
            .Append(Format(OriginAction, 20))
            .Append(Format(CreatedTimeStamp, 26))
            .Append(Format(HopCount, 2))
            .Append(Format(OriginalFormatId, 20))
            .Append(Format(OriginalMessageId, 32))
            .Append(Format(Padding, 40))
            .Append(Payload ?? string.Empty)

            .ToString();
        }


        private static string Pad(int padding)
        {
            return new string(' ', padding);
        }

        private static string Format(string value, int padding)
        {
            return (value?.SafeSubstring(0, padding) ?? string.Empty).PadRight(padding);
        }

        private static string Format(DateTime value, int padding = 26)
        {
            return Format(value.ToString("yyyy-MM-dd-HH.mm.ss.ffffff"), padding);
        }

        private static string Format(Guid value, int padding = 32)
        {
            return Format(value.ToString().Replace("-", string.Empty), padding);
        }
    }
}
