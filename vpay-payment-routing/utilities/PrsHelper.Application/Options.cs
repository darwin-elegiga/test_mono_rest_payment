// See https://aka.ms/new-console-template for more information
using System.Reflection.Emit;

internal class Options
{
    #region Properties

    public string ConfigurationFile { get; private set; } = default!;
    public string HostUri { get; private set; } = "http://localhost:5001";
    public string QueueSummaryEnpoint { get; private set; } = "/api/Support/queue-summary";
    public string QueuePaymentRecordEnpoint { get; private set; } = "/api/Support/queue-payment-record";
    public string RemittanceAdviceFileEndpoint { get; private set; } = "/api/RemittanceAdviceFile/QueueRemittanceAdviceFile";
    public bool Verbose { get; internal set; }
    public bool IncludeBinBash { get; internal set; }
    public bool ExecuteCurlStatements { get; private set; }
    public bool SaveOutputToShellScript { get; private set; }
    public string OutputFolder { get; private set; } = default!;
    public bool UseVariableForHost { get; internal set; }
    public bool CreateZipFileWithFakePdfs { get; private set; }

    #endregion

    public static Options ParseArgs(string[] args)
    {


        return DefaultOptions();
        //TODO: read commandline and make this do other things...

    }

    private static Options DefaultOptions()
    {

        /// Let's you take the "defaults", which is all I'm using right now...
        /// 
        return new Options
        {
            ConfigurationFile = "",
            HostUri = "https://payment-routing-api.stg.pks.vpayusa.net",
            CreateZipFileWithFakePdfs = true,
            Verbose = false,
            IncludeBinBash = true,
            UseVariableForHost = true,
            ExecuteCurlStatements = false,
            SaveOutputToShellScript = true,
            OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "testScripts")
        };

    }


}
