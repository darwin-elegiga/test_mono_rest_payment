// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;
using PrsHelper;
using PrsHelper.Formatters;
using PrsHelper.Models;

public class BatchSummary
{
    #region ctor and properties
    public BatchSummary(Options options)
    {
        BatchDateTime = DateTime.Now;
        //
        DateTimeStamp = BatchDateTime.ToString("yyyyMMddHHmmssfffff");

        _options = options;

        GlobalFontSettings.FontResolver = new FontResolver();
    }

    private string _outputFolder = string.Empty;

    private string CurrentOutputFolder
    {
        get
        {

            if (string.IsNullOrWhiteSpace(_outputFolder))
            {

                if (Options.CreateFolderUnderOutputFolder)
                {
                    _outputFolder = Path.Combine(Options.OutputFolder, BatchId);

                }
                else
                {
                    _outputFolder = Options.OutputFolder;
                }

                if (!Directory.Exists(_outputFolder))
                {
                    Directory.CreateDirectory(_outputFolder);
                }

            }
            return _outputFolder;
        }
    }
    private string _artifactFolder = string.Empty;
    /// <summary>
    /// Folder where artifacts (not the main scripts) go... will use same as <see cref="CurrentOutputFolder"/>
    /// if <see cref="Options.ArtifactsFolder"/> is null or empty.
    /// </summary>
    private string ArtifactFolder
    {
        get
        {

            if (!string.IsNullOrWhiteSpace(Options.ArtifactsFolder) && string.IsNullOrEmpty(_artifactFolder))
            {
                _artifactFolder = Path.Combine(CurrentOutputFolder, Options.ArtifactsFolder);
                if (!Directory.Exists(_artifactFolder))
                {
                    Directory.CreateDirectory(_artifactFolder);
                }
            }
            return _artifactFolder;
        }
    }



    private string _batchId = string.Empty;
    [JsonIgnore]
    public string BatchId
    {
        get
        {
            if (string.IsNullOrEmpty(_batchId))
            {
                _batchId = $"B{PayerAlias}{(string.IsNullOrWhiteSpace(BatchNameOverride) ? string.Empty : "_" + BatchNameOverride)}_{DateTimeStamp}";
            }

            return _batchId;
        }
    }
    [JsonIgnore]
    internal DateTime BatchDateTime { get; set; }
    public string BatchNameOverride { get; set; } = default!;
    [JsonIgnore]
    public string DateTimeStamp { get; set; } = default!;
    public string PayerAlias { get; set; } = default!;
    public string Payer835Id { get; set; } = default!;
    public string ClientCode { get; set; } = default!;
    public string OriginCode { get; set; } = default!;
    public string RemitMethod { get; set; } = default!;
    public string UserKeyPrefix { get; set; } = default!;
    public bool IsSubpayerSpecific { get; set; } = default!;
    public uint PaymentMultiplier { get; set; } = 1;

    [JsonIgnore]
    public string InboundZipFileName { get; set; } = default!;
    public List<PaymentDetail> PaymentDetails { get; set; } = default!;

    public void Set_Options(Options opt)
    {
        _options = opt;
    }

    public Options Options => _options;
    private Options _options;

    #endregion

    public List<string> GenerateBatch()
    {
        var payments = GenerateFakePayments(PaymentDetails);
        payments.Add(GetHeaderRecord());
        return GenerateBatch(payments);
    }

    public List<string> GenerateBatch(List<PaymentRecordDto> payments)
    {
        var zipScript = WriteZipScript(payments);

        if (_options.CreateZipFileWithFakePdfs || _options.CreatePdfCurlStatements)
        {
            SaveScript("01A_ZipCopy", zipScript);
            if (Options.IncludeSshScriptToCheckFtpFolder)
            {
                CreateSshToViewFtpLocation("01B");
            }
            if (Options.IncludeActiveBatchTrigger)
            {
                CreateActiveBatchTriggerScript("01C", Options.ActiveBatchUserName);
            }
        }
        var curlScript = WriteCurlScript(payments);
        var sqlScripts = WriteSqlScripts(payments);
        SaveScript("02_PrsCurls", curlScript);

        SaveSqlScript("99", sqlScripts);

        var script = new List<string>();
        script.AddRange(zipScript);
        script.AddRange(curlScript);
        script.AddRange(curlScript);
        return script;

    }


    /// <summary>
    /// Note this only works in Powershell...
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    private string CreateActiveBatchTriggerScript(string prefix, string username)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("$sResponse = \"N/A\";");
        sb.AppendLine("while ($sResponse -ne \"y\" -and $sResponse -ne \"n\")");
        sb.AppendLine("{");
        sb.AppendLine("	$sResponse = Read-Host -Prompt \"Are you sure you want to Trigger job 'STG_Inbound_File_Sweep' (/Staging/STLSTGLFTP01/Bogner/STG Optum Inbound Processing/STG_Inbound_File_Sweep) on plprdwabs01? [y/n]\";");
        sb.AppendLine("};");
        sb.AppendLine("if ($sResponse -eq \"n\") { Exit 0; };");

        sb.AppendLine($"$sPassword = Read-Host \"Enter password for '{username}' on 'plprdwabs01'\" -AsSecureString;");
        sb.AppendLine("$sPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sPassword));");
        sb.AppendLine("if ($sPassword -eq \"\") { Exit 0; };");

        sb.AppendLine($"$oJss = Connect-AbatJss \"plprdwabs01\" -Username \"{username}\" -Password \"$sPassword\"");
        sb.AppendLine("$oInstance = Start-AbatJob -JobScheduler $oJss -ObjectKey \"/Staging/STLSTGLFTP01/Bogner/STG Optum Inbound Processing/STG_Inbound_File_Sweep\"");

        sb.AppendLine("//PLPRDWABS01");
        sb.AppendLine("/Staging/STLSTGLFTP01/Bogner/STG Optum Inbound Processing/STG_Inbound_File_Sweep");

        string filename = Path.Combine(CurrentOutputFolder, $"{prefix}_Trigger_ActiveBatch_STG_Inbound_File_Sweep.ps1");

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename} already exists!");
            return filename;
        }
        File.WriteAllText(filename, sb.ToString());
        return filename;

    }

    private string CreateSshToViewFtpLocation(string prefix)
    {
        var fmt = _options.Formatter;
        List<string> script = new List<string>(WriteHeader());
        script.Add(fmt.ssh(Options.sftpUserName, Options.sftpServer, "cd /SRVFS/ftp_home/OPTUMSTG/OPTUMSTG/inbound/ && ls -lsa *PAYFILE*;"));
        script.Add(fmt.pause());

        string filename = Path.Combine(CurrentOutputFolder, $"{prefix}_Check_sftp_location{fmt.Extension}");

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename} already exists!");
            return filename;
        }
        File.WriteAllLines(filename, script.ToArray());
        return filename;

    }

    private List<string> WriteCurlScript(List<PaymentRecordDto> payments)
    {
        var headerScript = WriteHeader(true);
        var fmt = _options.Formatter;
        List<string> script = new List<string>(headerScript);
        script.Add(fmt.comment("These are the EOF/835 payment record curls"));
        script.Add(fmt.comment("it will insert the information for a batch to begin processing in PRS"));

        var paymentsNoEof = payments.Where(x => !x.TransactionType.Equals("EOF"));

        var batchRecord = GetBatchRecord(paymentsNoEof.Count(), paymentsNoEof.Sum(_ => Convert.ToDecimal(_.PaymentAmount)));
        foreach (var payment in payments)
        {
            var echoText = payment.TransactionType == "EOF"
                ? $"Calling {_options.QueuePaymentRecordEnpoint} for EOF PaymentRecord"
                : $"Calling {_options.QueuePaymentRecordEnpoint} for PaymentRecord {payment.UnconsolPaymentNbr}";

            script.Add(fmt.echo(echoText));
            if (Options.Use835FilesInCurlData)
            {
                var curlData = SaveRecordAs835File(payment);
                var curl = GenerateQueuePaymentRecordCurl(curlData);
                script.Add(curl);
            }
            else
            {
                var curl = GenerateQueuePaymentRecordCurl(payment);
                script.Add(curl);
            }
            script.Add(_options.Formatter.echo(""));
        }

        script.Add(fmt.comment("--------------------------------- #"));
        script.Add(fmt.comment("This is the batch summary"));
        script.Add(fmt.comment("running this statement _should_ trigger the PRS batch to go to the converter *"));

        script.Add(fmt.echo($"'Calling {_options.QueueSummaryEnpoint} for OptumBatchId {BatchId}'"));
        if (Options.Use835FilesInCurlData)
        {
            var curlData = SaveSummmaryAs835File(batchRecord);
            var curl = GenerateQueueSummaryCurl(curlData);
            script.Add(curl);
        }
        else
        {
            var curlSummary = GenerateQueueSummaryCurl(batchRecord);
            script.Add(curlSummary);
        }

        script.Add("");
        script.Add("");
        script.Add(fmt.comment("# * Some exclusions may apply."));

        if (Options.PauseScriptWindows)
        {
            script.Add(fmt.pause());
        }


        return script;
    }

    private List<string> WriteSqlScripts(List<PaymentRecordDto> payments)
    {
        List<string> sql = new List<string>();

        var inClause = IsSubpayerSpecific
            ? string.Join(',', payments.Where(p => p.TransactionType == "835")
                .Select(k => $"'{k.PayerSubpayerID}_{k.UnconsolPaymentNbr}'").ToArray())
            : string.Join(',', payments.Where(p => p.TransactionType == "835")
                .Select(k => $"'{k.UnconsolPaymentNbr}'").ToArray()); ;
        

        sql.Add("USE PaymentRouting");
        sql.Add("GO");

        if (_options.CreateZipFileWithFakePdfs || _options.CreatePdfCurlStatements)
        {

            sql.Add("");
            sql.Add("--------------------------------------------------------------------------------");
            sql.Add($"-- This is what _should_ be showing up after the zip file has uploaded OR");
            sql.Add($"-- the curl statements have been run to simulate the files been handled.");
            sql.Add("SELECT     *");
            sql.Add("FROM       [dbo].[RemittanceAdviceFile]");
            sql.Add($"WHERE      [UserKey] IN ({inClause})");
            sql.Add("--------------------------------------------------------------------------------");
        }
        sql.Add("");
        sql.Add($"-- This will show payments AS WELL as the transaction information AFTER it's been set up in the iSeries");
        sql.Add($"SELECT     *");
        sql.Add($"FROM       [dbo].[InboundPayment] P");
        sql.Add($"           LEFT JOIN [dbo].[ApMessageTracking] A ON A.InboundPaymentId=P.Id");
        sql.Add($"WHERE      P.[UserKey] IN ({inClause})");
        sql.Add("");
        sql.Add("--------------------------------------------------------------------------------");
        sql.Add("");
        sql.Add($"-- This will show you the batch/EOF/Header info ");
        sql.Add($"SELECT     * ");
        sql.Add($"FROM       [dbo].[InboundBatch] B");
        sql.Add($"           LEFT JOIN [dbo].[InboundHeader] H ON H.InboundBatchId=B.Id");
        sql.Add($"WHERE      B.[OptumBatchId] = '{BatchId}'");
        sql.Add("");
        sql.Add("--------------------------------------------------------------------------------");
        sql.Add("-- I always use this to see if the batches are incomplete...");
        sql.Add("-- exec usp_InboundBatch_IncompleteBatchDetails 0");
        sql.Add("-- Select * from dbo.vwRemittanceAdviceFileAgingReport");

        return sql;
    }

    private List<string> WriteZipScript(List<PaymentRecordDto> payments)
    {
        List<string> script = new List<string>(WriteHeader(false));
        var fmt = _options.Formatter;
        var pdfs = new List<RemittanceAdviceFileDto>();
        if (Options.CreateZipFileWithFakePdfs)
        {
            if (_options.Verbose)
            {
                script.Add("");
                script.Add(fmt.comment("If you have an .ssh key set up in your local bash setup"));
                script.Add(fmt.comment("the the following should work to copy the files to the appropriate"));
                script.Add(fmt.comment($"location: {Options.sftpServer}:{Options.sftpFolder}"));
                script.Add(fmt.comment("Once the trigger file is copied, if the activeBatch job is running"));
                script.Add(fmt.comment("it should put entries into the database as well as extract the appropriate"));
                script.Add(fmt.comment("pdf files to the appropriate location"));
                script.Add("");
            }

            var (zipFilename, ctrlFilename) = CreateZipFileWithFakePdfs(payments);

            script.Add(fmt.scp(zipFilename, Options.sftpUserName, Options.sftpServer, Options.sftpFolder));
            script.Add(fmt.scp(ctrlFilename, Options.sftpUserName, Options.sftpServer, Options.sftpFolder));

            script.Add("");

        }
        else
        {
            // Make the "create zip/ create curl to do zip" mutually exclusive
            if (Options.CreatePdfCurlStatements)
            {
                if (_options.Verbose)
                {
                    script.Add("# The following will simulate the zip file having already been ");
                    script.Add("# received and input.  Note that the file will fail when the python");
                    script.Add("# converter/dual-spec parser tries to find the files, unless they ");
                    script.Add("# exist in the proper folder (on SRVFS/bogner mount)");
                    script.Add("# ");
                    script.Add("# --------------------------------- #");
                    script.Add("# ");
                }
                foreach (var detail in payments)
                {
                    script.Add(_options.Formatter.echo($"Calling endpoint to insert zip/pdf info for {detail.UnconsolPaymentNbr}"));
                    var rafDto = GetRemittanceAdviceFile(detail.UnconsolPaymentNbr);
                    pdfs.Add(rafDto);
                    string rafCurl = GenerateRemittanceAdviceFileCurl(rafDto);
                    script.Add(rafCurl);
                    script.Add(_options.Formatter.echo(""));
                }
            }
        }

        if (Options.PauseScriptWindows)
        {
            script.Add(fmt.pause());
        }

        return script;
    }

    private List<string> WriteHeader(bool includeHostVariable = false)
    {
        var tmpOptions = Options.CreateCopy();
        tmpOptions.UseVariableForHost = includeHostVariable;
        return _options.Formatter.scriptHeader(tmpOptions).ToList();
    }

    private (string, string) CreateZipFileWithFakePdfs(List<PaymentRecordDto> payments)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var pdfNames = PdfHelper.CreatePdfs(payments, tempFolder, IsSubpayerSpecific).ToArray();

        var manifestFilename = Path.Combine(tempFolder, "MANIFEST.TXT");
        File.WriteAllLines(manifestFilename, pdfNames);

        string zipfileName = string.Empty;
        string triggerFilename = string.Empty;
        if (IsSubpayerSpecific)
        {
            var subpayerID = payments.GroupBy(_ => _.PayerSubpayerID).Select(_ => _.Key).First();

            zipfileName = Path.Combine(ArtifactFolder, $"{PayerAlias.ToUpper()}.{subpayerID}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ZIP");
            triggerFilename = Path.Combine(ArtifactFolder, $"{PayerAlias.ToUpper()}.{subpayerID}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ctrl");
        }
        else
        {
            zipfileName = Path.Combine(ArtifactFolder, $"{PayerAlias.ToUpper()}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ZIP");
            triggerFilename = Path.Combine(ArtifactFolder, $"{PayerAlias.ToUpper()}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ctrl");
        }        

        ZipFile.CreateFromDirectory(tempFolder, zipfileName, CompressionLevel.Fastest, false);
        File.Create(triggerFilename);

        Directory.Delete(tempFolder, true);
        return
        (
            Path.GetRelativePath(CurrentOutputFolder, zipfileName),
            Path.GetRelativePath(CurrentOutputFolder, triggerFilename)
        );
    }

    private List<PaymentRecordDto> GenerateFakePayments(List<PaymentDetail> details)
    {
        var payments = new List<PaymentRecordDto>();
        for (int i = 0; i < this.PaymentMultiplier; i++)
        {
            foreach (var detail in details)
            {
                detail.UserKey = UserKeyHelpers.GetUserKey(string.IsNullOrWhiteSpace(UserKeyPrefix) ? PayerAlias : UserKeyPrefix);
                var pr = GetPaymentRecord(detail.ProvTin, detail.PayerSubpayerId, detail.ZipCode, detail.UserKey, detail.Amount);
                payments.Add(pr);
            }
        }
        return payments;
    }



    #region create the objects


    private RemittanceAdviceFileDto GetRemittanceAdviceFile(string userKey)
    {
        if (string.IsNullOrEmpty(InboundZipFileName))
        {
            InboundZipFileName = $"{PayerAlias}.PAYFILE.{DateTime.Now:yyyy-MM-dd-HH.mm.ss}.zip";
        }

        return new RemittanceAdviceFileDto
        {
            payer835Id = Payer835Id,
            inboundZipfile = InboundZipFileName,
            userKey = userKey,
            payerAlias = PayerAlias,
            originCode = OriginCode,
            clientCode = ClientCode,
            remitMethod = RemitMethod,
            pdfExtractPath = $"/SRVFS/bogner/transfer/{OriginCode}/{ClientCode}/PDF/{userKey}.pdf",
            createdBy = "A Cool Person"
        };


    }

    private BatchRecordDto GetBatchRecord(int totalPayments, decimal totalAmount)
    {
        return new BatchRecordDto
        {
            BatchId = BatchId,
            Payer835Id = Payer835Id,
            TotalPaymentCount = totalPayments,
            TotalPaymentAmount = totalAmount

        };

    }



    private PaymentRecordDto GetHeaderRecord()
    {
        return new PaymentRecordDto
        {
            RemitMethod = RemitMethod,
            BatchID = BatchId,
            FileProcID = "1",
            PayerAlias = PayerAlias,
            Payer835ID = Payer835Id,
            TransactionType = "EOF",
            ProvTIN = "null",
            PayerSubpayerID = "",
            ZipCode = "",
            UnconsolPaymentNbr = "null",
            UconslPaymentAmount = "null",
            BlobStorageURI = "null",
            PaymentClob = GetIsaHeader()
        };


    }


    private PaymentRecordDto GetPaymentRecord(string provTin, string payerSubpayerId, string zipCode, string userKey, decimal amount)
    {
        return new PaymentRecordDto
        {
            RemitMethod = RemitMethod,
            BatchID = BatchId,
            FileProcID = "1",
            PayerAlias = PayerAlias,
            Payer835ID = Payer835Id,
            TransactionType = "835",
            ProvTIN = provTin,
            PayerSubpayerID = payerSubpayerId,
            ZipCode = zipCode,
            UnconsolPaymentNbr = userKey,
            UconslPaymentAmount = amount.ToString(),
            BlobStorageURI = "null",
            PaymentClob = GetStSeSegment(provTin, payerSubpayerId, zipCode, userKey, amount)
        };
    }


    #endregion


    #region create the curl statements


    private string GenerateRemittanceAdviceFileCurl(RemittanceAdviceFileDto record)
    {
        var data = JsonSerializer.Serialize(record);
        return GenerateCurlPostStatement(_options.RemittanceAdviceFileEndpoint, data);
    }

    private string SaveRecordAs835File(PaymentRecordDto paymentRecord)
    {
        var id = paymentRecord.TransactionType == "EOF" ? $"EOF_{paymentRecord.BatchID}" : $"835_{paymentRecord.UnconsolPaymentNbr}";
        var justFilename = $"PRS_{id}.json";
        var fullPath = Path.Combine(ArtifactFolder, justFilename);
        var data = JsonSerializer.Serialize(new PaymentRecordWrapper(paymentRecord));
        File.WriteAllText(fullPath, data);
        return Path.GetRelativePath(CurrentOutputFolder, fullPath);
    }

    private string SaveSummmaryAs835File(BatchRecordDto batchRecord)
    {
        var justFilename = $"PRS_SUM_{batchRecord.BatchId}.json";
        var fullPath = Path.Combine(ArtifactFolder, justFilename);
        var data = JsonSerializer.Serialize(batchRecord);
        File.WriteAllText(fullPath, data);
        return Path.GetRelativePath(CurrentOutputFolder, fullPath);
    }


    private string GenerateQueuePaymentRecordCurl(string dataFileName)
    {
        return GenerateCurlPostStatement(_options.QueuePaymentRecordEnpoint, $"@{dataFileName}");
    }
    private string GenerateQueuePaymentRecordCurl(PaymentRecordDto paymentRecord)
    {
        var data = JsonSerializer.Serialize(new PaymentRecordWrapper(paymentRecord));
        return GenerateCurlPostStatement(_options.QueuePaymentRecordEnpoint, data);
    }
    private string GenerateQueueSummaryCurl(string dataFileName)
    {
        return GenerateCurlPostStatement(_options.QueueSummaryEnpoint, $"@{dataFileName}");
    }
    private string GenerateQueueSummaryCurl(BatchRecordDto batchRecord)
    {
        var data = JsonSerializer.Serialize(batchRecord);
        return GenerateCurlPostStatement(_options.QueueSummaryEnpoint, data);
    }

    private string GenerateCurlPostStatement(string endpoint, string data)
    {

        var url = _options.UseVariableForHost ? "${hostUri}" : _options.HostUri;
        return _options.Formatter.curl_post(url + endpoint, new[] { "accept: */*", "Content-Type: application/json" }, data);

    }



    #endregion


    #region create EDI835 BS


    private string GetIsaHeader()
    {
        /// Fully fake data... but will parse...
        return
            "ISA*00*          *00*          *ZZ*391995276      *ZZ*411858498      *170321*1225*#*00501*000000870*0*T*:~" +
            "GS*HP*391995276*411858498*20170321*122536*54411*X*005010X221A1~";

    }

    private string GetStSeSegment(string provTin, string payerSubpayerId, string zipCode, string userKey, decimal amount)
    {
        // taken from sample data... This will "parse" at "JD's thing"... but certainly not accurate data...
        // There is a 
        return "ST*835*54411003~" +
            $"BPR*I*{amount}*C*ACH*CCP*01*000*DA*000*1{payerSubpayerId}*0000{Payer835Id}*01*000*DA*000*20170321~" +
            $"TRN*1*{userKey}*1{payerSubpayerId}*0000{Payer835Id}~" +
            "REF*EV*841162789~" +
            "DTM*405*20170321~" +

            "N1*PR*UMR EAST TEXAS MEDICAL CENTER~" +
            "N3*115 W WAUSAU AVE~" +
            $"N4*WAUSAU*WI*{zipCode}~" +

            $"REF*2U*{Payer835Id}~" +
            $"REF*EO*1{payerSubpayerId}~" +
            "PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~" +
            $"N1*PE*DENTAL HEALTH ASSOCIATES PA*FI*{provTin}~" +
            "N3*115 W WAUSAU AVE~" +
            $"N4*WAUSAU*WI*{zipCode}~" +

            "LX*1~" +
            "CLP*18251682*1*160*141.3*18.7*12*17076130016~" +
            "NM1*QC*1*LORD*LIANNA~" +
            "NM1*IL*1*LORD*LIANNA****MI*18251682~" +
            "NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~" +
            "REF*1L*76480007~" +
            "AMT*AU*141.3~" +
            "SVC*AD:D0120*75*56.3~" +
            "DTM*472*20170304~" +
            "CAS*PR*45*18.7~" +
            "AMT*B6*56.3~" +
            "SVC*AD:D1110*85*85~" +
            "DTM*472*20170304~" +
            "AMT*B6*85~" +

            "LX*2~" +
            "CLP*18251682*1*160*141.3*18.7*12*17076130017~" +
            "NM1*QC*1*BARTON*ELIZABETH~" +
            "NM1*IL*1*LORD*LIANNA****MI*18251682~" +
            "NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~" +
            "REF*1L*76480007~" +
            "AMT*AU*141.3~" +
            "SVC*AD:D0120*75*56.3~" +
            "DTM*472*20170305~" +
            "CAS*PR*45*18.7~" +
            "AMT*B6*56.3~" +
            "SVC*AD:D1110*85*85~" +
            "DTM*472*20170305~" +
            "AMT*B6*85~" +

            "LX*3~" +
            "CLP*18251683*1*160*141.3*18.7*12*17076130018~" +
            "NM1*QC*1*BOHANNAN*SARAH~" +
            "NM1*IL*1*BOHANNAN*SARAH****MI*18251683~" +
            "NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~" +
            "REF*1L*76480007~" +
            "AMT*AU*141.3~" +
            "SVC*AD:D0120*75*56.3~" +
            "DTM*472*20170305~" +
            "CAS*PR*45*18.7~" +
            "AMT*B6*56.3~" +
            "SVC*AD:D1110*85*85~" +
            "DTM*472*20170305~" +
            "AMT*B6*85~" +

            "LX*4~" +
            "CLP*18251684*1*160*141.3*18.7*12*17076130019~" +
            "NM1*QC*1*MEDRANO*JOSE~" +
            "NM1*IL*1*MEDRANO*JOSE****MI*18251684~" +
            "NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~" +
            "REF*1L*76480007~" +
            "AMT*AU*141.3~" +
            "SVC*AD:D0120*75*56.3~" +
            "DTM*472*20170315~" +
            "CAS*PR*45*18.7~" +
            "AMT*B6*56.3~" +
            "SVC*AD:D1110*85*85~" +
            "DTM*472*20170315~" +
            "AMT*B6*85~" +

            "LX*5~" +
            "CLP*18251684*1*160*141.3*18.7*12*17076130020~" +
            "NM1*QC*1*MEDRANO*ZENAIDA~" +
            "NM1*IL*1*MEDRANO*JOSE****MI*18251684~" +
            "NM1*82*1*ABDEL MALAK*AZIZ****XX*1265817969~" +
            "REF*1L*76480007~" +
            "AMT*AU*141.3~" +
            "SVC*AD:D0120*75*56.3~" +
            "DTM*472*20170305~" +
            "CAS*PR*45*18.7~" +
            "AMT*B6*56.3~" +
            "SVC*AD:D1110*85*85~" +
            "DTM*472*20170305~" +
            "AMT*B6*85~" +

            "SE*83*54411003~";
    }

    #endregion

    #region create PDF

    //private IEnumerable<string> CreatePdfs(List<PaymentRecordDto> payments, string tempFolder)
    //{
    //    foreach (var p in payments.Where(p => p.TransactionType == "835"))
    //    {
    //        var pdfName = $"{p.UnconsolPaymentNbr}.pdf";

    //        var copyToPdfFilename = Path.Combine(tempFolder, pdfName);

    //        if (!File.Exists(copyToPdfFilename))
    //        {
    //            CreatePdf(copyToPdfFilename, p.UnconsolPaymentNbr);
    //        }

    //        yield return pdfName;
    //    }
    //}

    //private void CreatePdf(string filePath, string content)
    //{
    //    using var document = new PdfDocument();
    //    var page = document.AddPage();

    //    var gfx = XGraphics.FromPdfPage(page);
    //    var font = new XFont("Arial", 20, XFontStyle.Bold);

    //    var textColor = XBrushes.Black;
    //    var layout = new XRect(20, 20, page.Width, page.Height);
    //    var format = XStringFormats.Center;

    //    gfx.DrawString(content, font, textColor, layout, format);

    //    document.Save(filePath);
    //}

    #endregion

    #region Save files
    public void SaveBatchSummary()
    {
        SaveBatchSummary($"Batch-{BatchId}.prs-bs.json");

    }

    public void SaveBatchSummary(string outputFilename)
    {
        var fullPath = Path.Combine(CurrentOutputFolder, outputFilename);
        var fileContent = JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }
            );


        File.WriteAllText(fullPath, fileContent);

    }

    public string SaveScript(string filePrefix, List<string> script)
    {
        if (!Directory.Exists(CurrentOutputFolder))
        {
            Directory.CreateDirectory(CurrentOutputFolder);
        }
        var ext = Options.Formatter.Extension;
        var file = $"{filePrefix}_{BatchId}{ext}";

        string filename = Path.Combine(CurrentOutputFolder, file);

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename} already exists!");
            return filename;
        }
        File.WriteAllLines(filename, script.ToArray());
        return filename;
    }

    public string SaveSqlScript(string scriptNumber, List<string> script)
    {
        if (!Directory.Exists(CurrentOutputFolder))
        {
            Directory.CreateDirectory(CurrentOutputFolder);
        }

        var file = $"{scriptNumber}_SqlScripts_{BatchId}.sql";

        string filename = Path.Combine(CurrentOutputFolder, file);

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename} already exists!");
            return filename;
        }
        File.WriteAllLines(filename, script.ToArray());
        return filename;
    }
    public string SaveAnyScript(string file, string[] lines)
    {
        string filename = Path.Combine(CurrentOutputFolder, file);

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename} already exists!");
            return filename;
        }
        File.WriteAllLines(filename, lines);
        return filename;
    }




    #endregion

    #region Load Files
    public static BatchSummary GetDefaultBS(Options options)
    {
        return new BatchSummary(options)
        {
            PayerAlias = "DUM25",
            Payer835Id = "UMR01",
            ClientCode = "UMR",
            OriginCode = "SEMED",
            RemitMethod = "PDF",
            PaymentDetails = new List<PaymentDetail>()
                {
                  new PaymentDetail { ProvTin="222692491", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=110.0m },
                  new PaymentDetail { ProvTin="222692491", PayerSubpayerId="627395711", ZipCode ="54402",  Amount=120.0m },
                  new PaymentDetail { ProvTin="222222211", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=130.0m },
                  new PaymentDetail { ProvTin="222222211", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=150.0m },
                }
        };
    }


    public static BatchSummary LoadFromFile(Options opt, string inputFilename)
    {
        if (!File.Exists(inputFilename))
        {
            throw new FileNotFoundException(inputFilename);
        }
        var content = File.ReadAllText(inputFilename);
        var bs = JsonSerializer.Deserialize<BatchSummary>(content);
        if (bs is null)
        {
            throw new Exception("Something is wrong with that file");
        }
        bs.Set_Options(opt);
        return bs;
    }

    public static BatchSummary LoadFromFile(string inputFilename)
    {
        if (!File.Exists(inputFilename))
        {
            throw new FileNotFoundException(inputFilename);
        }
        var content = File.ReadAllText(inputFilename);
        var bs = JsonSerializer.Deserialize<BatchSummary>(content);
        if (bs is null)
        {
            throw new Exception("Something is wrong with that file");
        }
        return bs;
    }

    #endregion

}
