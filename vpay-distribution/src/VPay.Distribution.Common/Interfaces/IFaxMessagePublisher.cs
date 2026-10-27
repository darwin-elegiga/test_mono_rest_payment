using System;

using VPay.DocSys.Common;

namespace VPay.Distribution.Common
{
    /// <summary>
    /// This is used to fax transactions
    /// </summary>
    public interface IFaxMessagePublisher
    {
        /// <summary>
        /// This will convert and publish all <paramref name="faxes"/> as messages to the tiff queue
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="tpa"></param>
        /// <param name="billingEntity"></param>
        /// <param name="boardUploadDateTime"></param>
        /// <param name="faxes"></param>
        /// <returns></returns>
        void PublishToTiffQueue(string origin, string tpa, string billingEntity, DateTime boardUploadDateTime, params DistTxnData[] faxes);
    }
}
