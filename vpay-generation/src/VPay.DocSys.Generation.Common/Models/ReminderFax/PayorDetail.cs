namespace VPay.DocSys.Generation.Common
{
    public class PayorDetail
    {
        public PayorDetail()
        {
            IsEobDependent = false;
        }

        public string PayorName { get; set; }
        public string PayorPhone { get; set; }
        public string VPayPhone { get; set; }

        /// <summary>
        /// Used to determine if the object values are dependent on the EOB (XML file)
        /// </summary>
        public bool IsEobDependent { get; set; }
    }
}
