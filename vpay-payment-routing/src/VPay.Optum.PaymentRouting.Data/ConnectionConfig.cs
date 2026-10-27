using System.Diagnostics.CodeAnalysis;

namespace VPay.Optum.PaymentRouting.Data
{
    /* Boiler plate code copied from VPay.ReconReports master/v0.4.0/Commit 30a20b19 */

    [ExcludeFromCodeCoverage]
    public class ConnectionConfig
    {
        public string Hostname { get; set; }
        public string Database { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool IntegratedSecurity { get; set; }

        public string BaseConnectionString { get; set; }

        /// <summary>
        /// The time in seconds to wait for the command to execute. The default is 30 seconds. 
        /// </summary>
        public int? DefaultCommandTimeout { get; set; }
    }
}
