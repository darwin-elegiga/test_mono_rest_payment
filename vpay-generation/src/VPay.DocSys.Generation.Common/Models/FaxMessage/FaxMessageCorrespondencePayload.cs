using System;
using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class FaxMessageCorrespondencePayload : BaseFaxMessagePayload
    {
        public FaxMessageCorrespondencePayload()
        {
            TimeStamp = DateTime.Now.ToUniversalTime();

            // set as defaults due to limitation of iSeries json parser
            CorrespondenceId = " ";
            Status = " ";
            FaxNumber = " ";
            TransactionIds = new[] { " " };
        }

        public static FaxMessageCorrespondencePayload CreateErrorPayload(Dictionary<string, object> jsonDictionary)
        {
            return new FaxMessageCorrespondencePayload
            {
                Status = "PARSE_ERR",
                CorrespondenceId = jsonDictionary
                    .TryGetValueDefault("Cid", " ")
                    .ToString()
                    .PadRight(1),
                FaxNumber = jsonDictionary
                    .TryGetValueDefault("faxn", " ")
                    .ToString()
                    .PadRight(1),
                TransactionIds = new[]
                {
                    jsonDictionary
                        .TryGetValueDefault("tids", jsonDictionary.TryGetValueDefault("SETXID", " "))
                        .ToString()
                        .PadRight(1)
                },
            };
        }

        public string[] TransactionIds { get; set; }
        public string CorrespondenceId { get; set; }
        public string Status { get; set; }

        /// <summary>
        /// Used for error messages
        /// </summary>
        public string ReasonCode => " ";
        /// <summary>
        /// Used for error messages
        /// </summary>
        public string ReasonText => " ";
        public string IpAddress => " ";
        public string Port => " ";
        public string RetryCount => " ";
        public string JobId => " ";
        public string FaxNumber { get; set; }
        public DateTime TimeStamp { get; private set; }
    }
}
