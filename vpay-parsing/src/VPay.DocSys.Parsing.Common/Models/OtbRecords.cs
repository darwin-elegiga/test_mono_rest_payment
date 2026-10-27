using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace VPay.DocSys.Parsing.Common
{
    [Serializable]
    public sealed class OtbRecords : Dictionary<string, IDictionary<string, string>>
    {
        public OtbRecords() { }

        public OtbRecords(IDictionary<string, IDictionary<string, string>> dictionary)
            : base(dictionary) { }

        private OtbRecords(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
