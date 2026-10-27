using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PrsHelper.Models;

namespace PrsHelper.WinForms
{
    public class BatchMananger : INotifyPropertyChanged
    {

        private BatchMananger(BatchTemplate batchTemplate, Options options)
        {
            BatchTemplate = batchTemplate;
            Options = options;
        }

        #region _private fields and KONSTANTS

        private const string K_DEFAULT_FOLDER = "PRSHelper";
        private const string K_DEFAULT_TEMPLATE_FILENAME = "BatchTemplate";
        private const string K_DEFAULT_TEMPLATE_FILE_EXT = ".btm-json";
        private const string K_DEFAULT_OPTION_FILENAME = "Options";
        private const string K_DEFAULT_OPTION_FILE_EXT = ".opt-json";
        private string _batchTemplateFilename;
        private string _optionsFilename;

        #endregion

        #region public Properties

        public Options? Options { get; set; } = default!;

        public BatchTemplate? BatchTemplate { get; set; } = default!;

        public string BatchTemplateFilename
        {
            get => _batchTemplateFilename;
            set
            {
                if (value != _batchTemplateFilename)
                {
                    _batchTemplateFilename = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string OptionsFilename
        {
            get => _optionsFilename;
            set
            {
                if (value != _optionsFilename)
                {
                    _optionsFilename = value;
                    NotifyPropertyChanged();
                }

            }
        }

 
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion


        #region Load Methods

        public static BatchMananger LoadFromSettings()
        {
            var saveDirectory = GetSaveDirectory();
            var batchTemplate = GetCurrentBatchTemplate();
            var options = GetCurrentOptions();

            UserSettings.Default.Save();
            return new BatchMananger(batchTemplate, options);
        }




        public void LoadOptions(string optionFilename)
        {

            if (File.Exists(optionFilename))
            {
                this.Options = Options.LoadFromFile(optionFilename);
            }
            else if (File.Exists(UserSettings.Default.LastOptionFilename))
            {
                optionFilename = UserSettings.Default.LastOptionFilename;
                this.Options = Options.LoadFromFile(optionFilename);
            }
            else
            {
                this.Options = new Options();
                this.Options.Save(optionFilename);
            }
            this.OptionsFilename = optionFilename;
            if (optionFilename != UserSettings.Default.LastOptionFilename)
            {
                UserSettings.Default.LastOptionFilename = optionFilename;
                UserSettings.Default.Save();
            }
        }
        public void LoadBatchTemplate(string batchTemplateFilename)
        {
            BatchTemplate = LoadBatchTemplateInternal(batchTemplateFilename);

            if (batchTemplateFilename != UserSettings.Default.LastBatchTemplateFilename)
            {
                UserSettings.Default.LastBatchTemplateFilename = batchTemplateFilename;
                UserSettings.Default.Save();
            }
        }


        #endregion

        #region Save Methods

        #endregion



        #region static methods

        internal static string GetSaveDirectory()
        {
            string saveDirectory = UserSettings.Default.LastSaveDirectory;
            if (string.IsNullOrWhiteSpace(saveDirectory))
            {
                // Default to the documents/PRSHelper folder
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                saveDirectory = Path.Combine(documents, K_DEFAULT_FOLDER);
            }
            if (UserSettings.Default.CreateSaveDirectoryIfNotExists && !Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            if (saveDirectory != UserSettings.Default.LastSaveDirectory)
            {
                UserSettings.Default.LastSaveDirectory = saveDirectory;
            }

            return saveDirectory;
        }
        internal static BatchTemplate GetCurrentBatchTemplate()
        {
            var batchTemplateFilename = UserSettings.Default.LastBatchTemplateFilename;

            // If it's not set... use create a new default.
            if (string.IsNullOrWhiteSpace(batchTemplateFilename))
            {
                string saveDirectory = UserSettings.Default.LastSaveDirectory;
                batchTemplateFilename = Path.Combine(saveDirectory, K_DEFAULT_TEMPLATE_FILENAME + K_DEFAULT_TEMPLATE_FILE_EXT);
            }


            var bt = LoadBatchTemplateInternal(batchTemplateFilename);


            if (batchTemplateFilename != UserSettings.Default.LastBatchTemplateFilename)
            {
                UserSettings.Default.LastBatchTemplateFilename = batchTemplateFilename;
            }
            return bt;
        }
        internal static Options GetCurrentOptions()
        {
            Options opt;
            string optionFilename = UserSettings.Default.LastOptionFilename;

            if (string.IsNullOrWhiteSpace(optionFilename))
            {
                string saveDirectory = UserSettings.Default.LastSaveDirectory;
                optionFilename = Path.Combine(saveDirectory, K_DEFAULT_OPTION_FILENAME + K_DEFAULT_OPTION_FILE_EXT);
            }
            if (!File.Exists(optionFilename))
            {
                opt = new Options();
                opt.Save(optionFilename);
            }
            else
            {
                opt = Options.LoadFromFile(optionFilename);
            }

            if (optionFilename != UserSettings.Default.LastOptionFilename)
            {
                UserSettings.Default.LastOptionFilename = optionFilename;
            }
            return opt;
        }
        internal static BatchTemplate LoadBatchTemplateInternal(string batchTemplateFilename)
        {
            BatchTemplate bt;

            if (!File.Exists(batchTemplateFilename))
            {
                bt = new BatchTemplate
                {
                    ClientCode = "UMR",
                    OriginCode = "SEMED",
                    Payer835Id = "UMR01",
                    PayerAlias = "MUM25",
                    UserKeyPrefix = "MA",
                    BatchNameOverride = "Test"
                };
                bt.Save(batchTemplateFilename);
            }
            else
            {
                bt = BatchTemplate.Load(batchTemplateFilename);
            }


            return bt;
        }


        #endregion


    }
}
