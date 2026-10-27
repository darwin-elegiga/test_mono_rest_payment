using System;
using System.Collections.Generic;
using System.Text;
using VPay.DocSys.Parsing.Common;

namespace VPay.DocSys.Parsing
{
    public class Inbound835FileServiceSettings
    {
        public AcknowledgmentFileSettings AcknowledgmentFile { get; set; } = new AcknowledgmentFileSettings();
        public RemittanceAdviceFileSettings RemittanceAdviceFile { get; set; } = new RemittanceAdviceFileSettings();
        public PaymentPostedFileSettings PaymentPostedFile { get; set; } = new PaymentPostedFileSettings();
    }

    public class AcknowledgmentFileSettings
    {
        public bool IgnoreMissingTransactions { get; set; } = false;
        public SendAckPmbMessageSettings SendAckPmbMessage { get; set; } = new SendAckPmbMessageSettings();
    }

    public class SendAckPmbMessageSettings
    {
        public bool Enabled { get; set; } = true;
        public int Retries { get; set; } = 2;
        public TimeSpan SleepBetweenRetries { get; set; } = TimeSpan.FromSeconds(3);
    }

    public class RemittanceAdviceFileSettings
    {
        public bool IgnoreMissingTransactions { get; set; } = false;
        public SendToAvailitySettings SendToAvaility { get; set; } = new SendToAvailitySettings();
    }

    public class SendToAvailitySettings
    {
        public bool Enabled { get; set; } = true;
        public SendToAvailityTransferSettings TransferSettings { get; set; } = new SendToAvailityTransferSettings();
    }

    public class SendToAvailityTransferSettings
    {
        public string Host { get; set; } = "";
        public bool UseRsaKey { get; set; } = false;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string TransferPath { get; set; } = "";
    }

    public class PaymentPostedFileSettings
    {
        public bool IgnoreMissingTransactions { get; set; } = false;
        public iSeriesTransferSettings iSeriesTransfer { get; set; } = new iSeriesTransferSettings();
        public iSeriesProcessSettings iSeriesProcess { get; set; } = new iSeriesProcessSettings();
    }

    public class iSeriesTransferSettings
    {
        public bool Enabled { get; set; } = true;
        public string Host { get; set; } = "";
        public bool UseRsaKey { get; set; } = false;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string TransferPath { get; set; } = "";
    }

    public class iSeriesProcessSettings
    {
        public bool Enabled { get; set; } = true;
        public int Retries { get; set; } = 2;
        public TimeSpan SleepBetweenRetries { get; set; } = TimeSpan.FromSeconds(3);
    }
}
