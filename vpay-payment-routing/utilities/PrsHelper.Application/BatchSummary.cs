// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrsHelper.Models;

internal class BatchSummary
{
    public BatchSummary(Options options)
    {
        BatchDateTime = DateTime.Now;
        //
        DateTimeStamp = BatchDateTime.ToString("yyyyMMddHHmmssfffff");

        _options = options;
    }


    public string BatchId()
    {
        return $"{PayerAlias}_{DateTimeStamp}";
    }
    [JsonIgnore]
    internal DateTime BatchDateTime { get; set; }

    [JsonIgnore]
    public string DateTimeStamp { get; internal set; } = default!;
    public string PayerAlias { get; internal set; } = default!;
    public decimal[] PaymentsAmounts { get; internal set; } = default!;
    public bool CreateZipFile { get; internal set; } = default!;
    public bool CreateCurlForPdfs { get; internal set; } = default!;
    public string Payer835Id { get; internal set; } = default!;
    public string ClientCode { get; internal set; } = default!;
    public string OriginCode { get; internal set; } = default!;
    public string RemitMethod { get; internal set; } = default!;
    public string InboundZipFileName { get; internal set; } = default!;
    public List<PaymentDetail> PaymentDetails { get; internal set; } = default!;

    private List<string> _existingUserKeys = new List<string>();
    private List<string> curlStatements = new List<string>();

    public Options Options => _options;
    private readonly Options _options;

    public List<string> GenerateBatch()
    {
        var payments = GenerateFakePayments(PaymentDetails);
        return GenerateBatch(payments);
    }

    public List<string> GenerateBatch(List<PaymentRecordDto> payments)
    {
        curlStatements.Clear();

        if (Options.IncludeBinBash)
        {
            curlStatements.Add("#!/bin/bash");
            curlStatements.Add("###########");
            if (_options.UseVariableForHost)
            {
                // use this to be able to easily manipulate the script to be able to change to a different host.
                curlStatements.Add($"hostUri={_options.HostUri}");
            }
        }


        var pdfs = new List<RemittanceAdviceFileDto>();

        if (Options.CreateZipFileWithFakePdfs)
        {
            CreateZipFileWithFakePdfs(payments);
        }

        foreach (var detail in payments)
        {
            if (CreateCurlForPdfs)
            {
                var rafDto = GetRemittanceAdviceFile(detail.UnconsolPaymentNbr);
                pdfs.Add(rafDto);
                string rafCurl = GenerateRemittanceAdviceFileCurl(rafDto);
                curlStatements.Add(rafCurl);
            }
        }



        payments.Add(GetHeaderRecord());


        var batchRecord = GetBatchRecord(PaymentDetails.Count(), PaymentDetails.Sum(_ => _.Amount));
        foreach (var payment in payments)
        {
            var curl = GenerateQueuePaymentRecordCurl(payment);
            curlStatements.Add(curl);
        }
        var curlSummary = GenerateQueueSummaryCurl(batchRecord);
        curlStatements.Add(curlSummary);


        return curlStatements;

    }

    private void CreateZipFileWithFakePdfs(List<PaymentRecordDto> payments)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }
        var pdfNames = payments.Select(_ => $"{_.UnconsolPaymentNbr}.pdf").ToArray();
        var fakePdfFilename = "FakePra.pdf";
        foreach (var pdfName in pdfNames)
        {
            var copyToPdfFilename = Path.Combine(tempFolder, pdfName);
            if (!File.Exists(copyToPdfFilename))
            {
                File.Copy(fakePdfFilename, copyToPdfFilename);
            }
        }
        var manifestFilename = Path.Combine(tempFolder, "MANIFEST.TXT");
        File.WriteAllLines(manifestFilename, pdfNames);
        string zipfileName = Path.Combine(Options.OutputFolder, $"{PayerAlias.ToUpper()}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ZIP");
        string triggerFilename = Path.Combine(Options.OutputFolder, $"{PayerAlias.ToUpper()}.PAYFILE.{BatchDateTime:yyyy.MM.dd.HH-mm-ss}.ctrl");

        ZipFile.CreateFromDirectory(tempFolder, zipfileName, CompressionLevel.Fastest, false);
        File.Create(triggerFilename);

        Directory.Delete(tempFolder, true);
    }

    private List<PaymentRecordDto> GenerateFakePayments(List<PaymentDetail> details)
    {
        var payments = new List<PaymentRecordDto>();
        foreach (var detail in details)
        {
            detail.UserKey = GetUserKey();
            var pr = GetPaymentRecord(detail.ProvTin, detail.PayerSubpayerId, detail.ZipCode, detail.UserKey, detail.Amount);
            payments.Add(pr);
        }
        return payments;
    }


    public string SaveScript()
    {
        if (curlStatements.Count == 0) { GenerateBatch(); }

        if (!Directory.Exists(Options.OutputFolder))
        {
            Directory.CreateDirectory(Options.OutputFolder);
        }
        string filename = Path.Combine(Options.OutputFolder, $"{BatchId()}.sh");

        if (File.Exists(filename))
        {
            Console.Error.WriteLine($"File {filename}  already exists!");
            return filename;
        }

        File.WriteAllLines(filename, curlStatements.ToArray());

        return filename;


    }



    private string GetUserKey()
    {
        int seq = 1;
        while (true)
        {
            var userKey = $"{PayerAlias}_{DateTimeStamp}_{seq++.ToString().PadLeft(3, '0')}";
            if (!_existingUserKeys.Contains(userKey))
            {
                _existingUserKeys.Add(userKey);
                return userKey;
            }
        }
    }

    #region create batch
    #endregion


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
            BatchId = BatchId(),
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
            BatchID = BatchId(),
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
            BatchID = BatchId(),
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

    private string GenerateQueuePaymentRecordCurl(PaymentRecordDto paymentRecord)
    {
        var data = JsonSerializer.Serialize(new PaymentRecordWrapper(paymentRecord));
        return GenerateCurlPostStatement(_options.QueuePaymentRecordEnpoint, data);
    }

    private string GenerateQueueSummaryCurl(BatchRecordDto batchRecord)
    {
        var data = JsonSerializer.Serialize(batchRecord);
        return GenerateCurlPostStatement(_options.QueueSummaryEnpoint, data);
    }

    private string GenerateCurlPostStatement(string endpoint, string data)
    {
        if (_options.UseVariableForHost)
        {
            return $"curl -X POST \"${{hostUri}}{endpoint}\" -k -H 'accept: */*' -H 'Content-Type: application/json' -d '{data}'";


        }
        else
        {
            return $"curl -X POST {_options.HostUri}{endpoint} -k -H 'accept: */*' -H 'Content-Type: application/json' -d '{data}'";
        }
    }



    #endregion


    #region create EDI835 BS


    private string GetIsaHeader()
    {
        /// Fully fake data... but will parse...
        return
            "ISA*00*          *00*          *ZZ*391995276      *ZZ*411858498      *170321*1225*^*00501*000000870*0*T*:~" +
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

    public void SaveBatchSummary()
    {
        SaveBatchSummary($"Batch-{BatchId()}.prs-bs");

    }

    public void SaveBatchSummary(string outputFilename)
    {
        var fullPath = Path.Combine(Options.OutputFolder, outputFilename);
        var fileContent = JsonSerializer.Serialize(this);
        File.WriteAllText(fullPath, fileContent);

    }

    public static BatchSummary GetDefaultBS(Options options)
    {
        return new BatchSummary(options)
        {
            PayerAlias = "DUM25",
            Payer835Id = "UMR01",
            ClientCode = "UMR",
            OriginCode = "SEMED",
            RemitMethod = "PDF",
            CreateZipFile = false,
            CreateCurlForPdfs = true,
            PaymentDetails = new List<PaymentDetail>()
                {
                  new PaymentDetail { ProvTin="222692491", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=110.0m },
                  new PaymentDetail { ProvTin="222692491", PayerSubpayerId="627395711", ZipCode ="54402",  Amount=120.0m },
                  new PaymentDetail { ProvTin="222222211", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=130.0m },
                  new PaymentDetail { ProvTin="222222211", PayerSubpayerId="752348381", ZipCode ="54402",  Amount=150.0m },
                }
        };
    }

}
