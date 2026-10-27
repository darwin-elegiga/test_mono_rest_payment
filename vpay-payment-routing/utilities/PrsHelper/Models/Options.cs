// See https://aka.ms/new-console-template for more information
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrsHelper;
using PrsHelper.Formatters;

public record Options : ITemplateFile, INotifyPropertyChanged 
{
    #region CONSTANTS
    /// <summary>Where are the server/endpoints</summary>
    private const string K_API_SETTINGS = "API Settings";
    /// <summary>Where are the files sent</summary>
    private const string K_FTP_SETTINGS = "FTP Settings";
    /// <summary>Where the file are written</summary>
    private const string K_OUTPUT_SETTINGS = "Output Settings";
    /// <summary>How the scripts are formed</summary>
    private const string K_SCRIPT_SETTINGS = "Script Settings";
    /// <summary>How the zips/pdfs are formed</summary>
    private const string K_ZIP_PDF_SETTINGS = "Zip/Pdf Settings";
    #endregion

    #region Properties


    #region private fields
    private ICommandFormatter? _formatter;
    private CommandFormatter _commandFormatter = CommandFormatter.Bash;
    private bool _createFolderUnderOutputFolder = true;
    private string _outputFolder = default!;
    private string _hostUri = "http://localhost:5001";
    private string _queueSummaryEnpoint = "/api/Support/queue-summary";
    private string _queuePaymentRecordEnpoint = "/api/Support/queue-payment-record";
    private string _remittanceAdviceFileEndpoint = "/api/RemittanceAdviceFile/QueueRemittanceAdviceFile";
    private string _sftpServer = "stlstglftp01";
    private string _sftpUserName = "jhill";
    private string _sftpFolder = "/SRVFS/ftp_home/OPTUMSTG/OPTUMSTG/inbound";
    private bool _verbose;

    private bool _use835FilesInCurlData = default(bool);

    private bool _createZipFileWithFakePdfs;
    private bool _useVariableForHost;
    private string _filename = string.Empty;

    private string _artifactsFolder = string.Empty;
    private bool _createPdfCurlStatements;
    private bool _includeBinBash;
    private bool _pauseScriptWindows;
    private string _activeBatchUserName;
    private bool _includeActiveBatchTrigger;
    private bool _includeSshScriptToCheckFtpFolder;

    #endregion

    #region API Settings
    [Category(K_API_SETTINGS)]
    [DisplayName("Host URI")]
    [Description("The host which will be used in the curl statements to POST data.")]
    [DefaultValue("http://localhost:5001")]
    [TypeConverter(typeof(HostUriStringConverter))]
    public string HostUri
    {
        get => _hostUri;
        set
        {
            if (value != this._hostUri)
            {
                _hostUri = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_API_SETTINGS), Description("Swagger API endpoint for queuing the Batch Summary")]
    public string QueueSummaryEnpoint
    {
        get => _queueSummaryEnpoint;
        set
        {
            if (value != this._queueSummaryEnpoint)
            {
                _queueSummaryEnpoint = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_API_SETTINGS), Description("Swagger API endpoint for queuing the Payments (835 & EOF)")]
    public string QueuePaymentRecordEnpoint
    {
        get => _queuePaymentRecordEnpoint;
        set
        {
            if (value != this._queuePaymentRecordEnpoint)
            {
                _queuePaymentRecordEnpoint = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_API_SETTINGS), Description("Swagger API endpoint for queuing Zip/Pdf file information")]
    public string RemittanceAdviceFileEndpoint
    {
        get => _remittanceAdviceFileEndpoint;
        set
        {
            if (value != this._remittanceAdviceFileEndpoint)
            {
                _remittanceAdviceFileEndpoint = value;
                NotifyPropertyChanged();
            }
        }
    }
    #endregion



    #region FTP Settings

    [Category(K_FTP_SETTINGS)]
    [Description("Server to upload zip/ctrl files to.")]
    public string sftpServer
    {
        get => _sftpServer;
        set
        {
            if (value != this._sftpServer)
            {
                _sftpServer = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_FTP_SETTINGS)]
    [Description("User for sftp server.")]
    public string sftpUserName
    {
        get => _sftpUserName;
        set
        {
            if (value != this._sftpUserName)
            {
                _sftpUserName = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_FTP_SETTINGS)]
    [Description("sftp destination folder.")]
    public string sftpFolder
    {
        get => _sftpFolder;
        set
        {
            if (value != this._sftpFolder)
            {
                _sftpFolder = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_FTP_SETTINGS)]
    [Description("Do you want to include a script to check the folder location where the files are FTP'ed to?")]
    public bool IncludeSshScriptToCheckFtpFolder
    {
        get => _includeSshScriptToCheckFtpFolder;
        set
        {
            if (value != this._includeSshScriptToCheckFtpFolder)
            {
                _includeSshScriptToCheckFtpFolder = value;
                NotifyPropertyChanged();
            }
        }
    }


    [Category(K_FTP_SETTINGS)]
    [Description("Do you want to include an ActiveBatch Powershell script to trigger job?")]
    public bool IncludeActiveBatchTrigger
    {
        get => _includeActiveBatchTrigger;
        set
        {
            if (value != this._includeActiveBatchTrigger)
            {
                _includeActiveBatchTrigger = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_FTP_SETTINGS)]
    [Description("Username that has access to trigger Active Batch job")]
    public string ActiveBatchUserName
    {
        get => _activeBatchUserName;
        set
        {
            if (value != this._activeBatchUserName)
            {
                _activeBatchUserName = value;
                NotifyPropertyChanged();
            }
        }
    }



    #endregion

    #region Output Settings

    [Category(K_OUTPUT_SETTINGS)]
    [Description("Create a new folder with batch name under OutputFolder for all generated files.")]
    public bool CreateFolderUnderOutputFolder
    {
        get => _createFolderUnderOutputFolder;
        set
        {
            if (value != this._createFolderUnderOutputFolder)
            {
                _createFolderUnderOutputFolder = value;
                NotifyPropertyChanged();
            }
        }
    }
    [Category(K_OUTPUT_SETTINGS)]
    [Description("Folder where generated files will be output.")]
    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            if (value != this._outputFolder)
            {
                _outputFolder = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_OUTPUT_SETTINGS)]
    [Description("If not blank, store any artifacts to the process in a subfolder with this name")]
    public string ArtifactsFolder
    {
        get => _artifactsFolder;
        set
        {
            if (value != this._artifactsFolder)
            {
                _artifactsFolder = value;
                NotifyPropertyChanged();
            }
        }
    }

    #endregion

    #region Script Settings


    [JsonIgnore]
    [Browsable(false)]
    public ICommandFormatter Formatter
    {
        get
        {
            if (_formatter == null)
            {
                switch (CommandFormatter)
                {
                    case CommandFormatter.Dos:
                        {
                            // Need to "turn off" certain options for MS-DOS
                            IncludeBinBash = false;
                            UseVariableForHost = false;
                            _formatter = new DosCommandFormatter();
                        }
                        break;
                    case CommandFormatter.Bash:
                    default:
                        _formatter = new BashCommandFormatter();
                        break;
                }
            }
            return _formatter;
        }
    }

    [Category(K_SCRIPT_SETTINGS)]
    [Description("Determine how the script files will be output (bash or MS-DOS")]
    public CommandFormatter CommandFormatter
    {
        get => _commandFormatter;
        set
        {
            if (value != this._commandFormatter)
            {
                _commandFormatter = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_SCRIPT_SETTINGS)]
    [Description("Include extra information about process.")]
    public bool Verbose
    {
        get => _verbose;
        set
        {
            if (value != this._verbose)
            {
                _verbose = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_SCRIPT_SETTINGS)]
    [Description("Inlcude '#!/bin/bash' statement in output ")]
    public bool IncludeBinBash
    {
        get => _includeBinBash;
        set
        {
            if (value != this._includeBinBash)
            {
                _includeBinBash = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_SCRIPT_SETTINGS)]
    [Description("Use a variable for the hostname (allows for easier changing of script to use in other environments, such as local dev)")]
    public bool UseVariableForHost
    {
        get => _useVariableForHost;
        set
        {
            if (value != this._useVariableForHost)
            {
                _useVariableForHost = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_SCRIPT_SETTINGS)]
    [Description("Create json files and reference in curl statements (rather than using escaped json)")]
    public bool Use835FilesInCurlData
    {
        get => _use835FilesInCurlData;
        set
        {
            if (value != this._use835FilesInCurlData)
            {
                _use835FilesInCurlData = value;
                NotifyPropertyChanged();
            }
        }
    }



    [Category(K_SCRIPT_SETTINGS)]
    [Description("Pause scripts with console window open (use for launching from Windows).")]

    public bool PauseScriptWindows
    {
        get => _pauseScriptWindows;
        set
        {
            if (value != this._pauseScriptWindows)
            {
                _pauseScriptWindows = value;
                NotifyPropertyChanged();
            }
        }
    }

    #endregion

    #region Zip/Pdf Settings


    [Category(K_ZIP_PDF_SETTINGS)]
    [Description("Create the zip (with faked up pdfs and MANIFEST.TXT) to be copied " +
        "to stlstglftp01:/SRVFS/ftp_home/OPTUMSTG/OPTUMSTG/inbound.")]
    public bool CreateZipFileWithFakePdfs
    {
        get => _createZipFileWithFakePdfs;
        set
        {
            if (value != this._createZipFileWithFakePdfs)
            {
                _createZipFileWithFakePdfs = value;
                NotifyPropertyChanged();
            }
        }
    }

    [Category(K_ZIP_PDF_SETTINGS)]
    [Description("Create curl to insert remittance advice file information.")]
    public bool CreatePdfCurlStatements
    {
        get => _createPdfCurlStatements;
        set
        {
            if (value != this._createPdfCurlStatements)
            {
                _createPdfCurlStatements = value;
                NotifyPropertyChanged();
            }
        }
    }

    #endregion

    #region non-visible settings
    [Browsable(false)]
    [JsonIgnore]
    public bool HasChanges { get; set; } = false;

    [Browsable(false)]
    [JsonIgnore]
    public string Filename
    {
        get => _filename;
        set
        {
            if (value != this._filename)
            {
                _filename = value;
                NotifyPropertyChanged();
            }
        }
    }



    #endregion



    #endregion

    #region Methods and Events

    public event PropertyChangedEventHandler? PropertyChanged;
    // This method is called by the Set accessor of each property.  
    // The CallerMemberName attribute that is applied to the optional propertyName  
    // parameter causes the property name of the caller to be substituted as an argument.  
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        this.HasChanges = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ITemplateFile Save(string fullPath)
    {
        var fileContent = JsonSerializer.Serialize(this,
           new JsonSerializerOptions { WriteIndented = true }
           );
        File.WriteAllText(fullPath, fileContent);
        Filename = fullPath;
        HasChanges = false;
        return this;
    }

    public static Options LoadFromFile(string optionsFilename)
    {
        if (!File.Exists(optionsFilename))
        {
            throw new FileNotFoundException(optionsFilename);
        }
        var content = File.ReadAllText(optionsFilename);
        var opts = JsonSerializer.Deserialize<Options>(content);
        if (opts is null)
        {
            throw new Exception("Something is wrong with options file");
        }
        opts.Filename = optionsFilename;
        opts.HasChanges = false;
        return opts;
    }

    public Options CreateCopy()
    {
 
        // setup
        var json = JsonSerializer.Serialize(this);

        // get
        return JsonSerializer.Deserialize<Options>(json)??new Options();
   
    }
    #endregion
}
