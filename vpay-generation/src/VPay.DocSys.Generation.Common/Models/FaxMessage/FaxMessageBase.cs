using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common.Helpers;

namespace VPay.DocSys.Generation.Common
{
    public abstract class FaxMessageBase<T> where T : BaseFaxMessagePayload
    {
        /// <summary>
        /// The Pmb DateTime format
        /// </summary>
        private const string FaxMessageDateTimeFormatString = "yyyy-MM-dd-HH.mm.ss.ffffff";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateFormatString = FaxMessageDateTimeFormatString,
        };

        protected FaxMessageBase()
        {
            CreateTimeStamp = DateTime.Now;
            MessageId = Guid.NewGuid();
        }

        protected FaxMessageBase(string message)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            Priority = ParsePriority(message);
            Severity = int.Parse(message.Substring(87, 2));
            MessageId = Guid.Parse(message.Substring(103, 32));
            OriginAction = message.Substring(135, 20).Trim();
            CreateTimeStamp = DateTime.ParseExact(message.Substring(155, 26), FaxMessageDateTimeFormatString, CultureInfo.InvariantCulture);
            HopCount = int.Parse(message.Substring(181, 2));

            Payload = JsonConvert.DeserializeObject<T>(message.Substring(275));
        }

        public string MessageType => "PMB";
        public string Version => "00.00.0001";
        public abstract string FromSystem { get; }
        public abstract string ToSystem { get; }
        public string Family => "FAXDIST".PadRight(20);
        public string SmallFamily => "FAX";
        public abstract long MessageIndex { get; }
        public byte Priority { get; protected set; }
        public DateTime CreateTimeStamp { get; protected set; }
        public string OriginAction { get; protected set; }
        public int Severity { get; }
        public int HopCount { get; }
        public string OrgMessageId => new string(' ', 32);
        public Guid MessageId { get; protected set; }
        public string OrgFormatId => new string(' ', 20);
        public string Padding => new string(' ', 40);
        public string TypeOfMessage => "I";
        public int FormatVersion => 1;

        public T Payload { get; protected set; }

        public static byte ParsePriority(string message)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(message, nameof(message));

            byte.TryParse(message.Substring(73, 10), out var priority);

            return priority;
        }

        public override string ToString() =>
            new StringBuilder()

            .Append(MessageType.PadAndTrim(3))
            .Append(Version.PadAndTrim(10))

            .Append(FromSystem.PadAndTrim(20))
            .Append(ToSystem.PadAndTrim(20))

            .Append(Family.PadAndTrim(20))

            .Append(Priority.ToString("D10"))

            .Append(SmallFamily.PadAndTrim(3))
            .Append(TypeOfMessage.PadAndTrim(1))
            .Append(Severity.ToString("D2"))
            .Append(MessageIndex.ToString("D6"))
            .Append(FormatVersion.ToString("D4"))
            .Append("    ")

            .Append(MessageId.ToString().Replace("-", string.Empty).ToUpper())

            .Append(OriginAction.PadAndTrim(20))
            .Append(CreateTimeStamp.ToString(FaxMessageDateTimeFormatString))

            .Append(HopCount.ToString().PadAndTrim(2))

            .Append(OrgFormatId.PadAndTrim(20))
            .Append(OrgMessageId.PadAndTrim(32))

            .Append(Padding.PadAndTrim(40))

            .Append(JsonConvert.SerializeObject(Payload, SerializerSettings))

            .ToString();
    }
}
