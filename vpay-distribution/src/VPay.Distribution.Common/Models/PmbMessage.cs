using System;
using System.Text;
using Microsoft.Extensions.Internal;
using VPay.Distribution.Common.Interfaces;

namespace VPay.Distribution.Common.Models
{
    public class PmbMessage : IPmbMessage
    {

        private readonly string _messageType;
        private string _fromSystem = "";
        private string _toSystem = "";
        private string _family = "";
        private string _priority;
        private string _formatId;        
        private readonly string _messageId;
        private readonly string _originAction;
        private string _createTimestamp;        
        private readonly string _padding;
        private string _data = "";
        private readonly string _version;

        public PmbMessage() : this(Guid.NewGuid(), new SystemClock())
        {
        }

        // ISystemGuid is used for testing. Normally just does a Guid.NewGuid() but for testing you can mock it to return something else.
        public PmbMessage(Guid systemGuid, ISystemClock systemClock)
        {
            _createTimestamp = systemClock.UtcNow.ToString("yyyy-MM-dd-HH.mm.ss.ffffff").PadRight(26);
            _messageType = "PMB";
            _version = "00.00.0001";
            _messageId = systemGuid.ToString().Replace("-", "");
            _priority = "0000000000";
            _formatId = "STPI00000006";            
            _originAction = "".PadRight(20);            
            _padding = "".PadRight(94);
        }

        public string MessageType => _messageType;

        public string Version => _version;

        public string FromSystem
        {
            get => _fromSystem.Trim().PadRight(20);
            set => _fromSystem = value ?? "";
        }
        public string ToSystem
        {
            get => _toSystem.Trim().PadRight(20, ' ');
            set => _toSystem = value ?? "";
        }

        public string Family
        {
            get => _family.Trim().PadRight(20, ' ');
            set => _family = value ?? "";
        }

        public string Priority
        {
            get => _priority.Trim().PadRight(10, ' ');
            set => _priority = value ?? "";
        }

        public string FormatId
        {
            get => _formatId.Trim().PadRight(20, ' ');
            set => _formatId = value ?? "";
        }
       
        public string MessageId => _messageId;

        public string OriginAction => _originAction;

        public string CreateTimestamp
        {
            get => _createTimestamp.Trim().PadRight(26, ' ');
            set => _createTimestamp = value ?? "";
        }   

        public string Padding => _padding;

        public string Data
        {
            get => _data.Trim().PadRight(4845, ' ');
            set => _data = value ?? "";

        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(MessageType);
            sb.Append(Version);
            sb.Append(FromSystem);
            sb.Append(ToSystem);
            sb.Append(Family);
            sb.Append(Priority);
            sb.Append(FormatId);           
            sb.Append(MessageId);
            sb.Append(OriginAction);
            sb.Append(CreateTimestamp);                        
            sb.Append(Padding);
            sb.Append(Data);

            return sb.ToString();
        }
    }
}
