using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface ITarService
    {
        void ParseTarAndPublishMessages(List<DocumentTransactionMessage> messages, string folder, bool isRedCard, bool? isEmergencyFile, bool? saveMessages);
    }
}
