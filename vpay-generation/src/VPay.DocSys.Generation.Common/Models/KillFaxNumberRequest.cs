namespace VPay.DocSys.Generation.Common
{
    public class KillFaxNumberRequest
    {
        public KillFaxNumberRequest(string phoneNumber)
        {
            PhoneNumber = phoneNumber;
        }

        public string PhoneNumber { get; }
        public string JiraTicket => "Real Time";
        public string ModifyUser => "iSeries";
    }
}
