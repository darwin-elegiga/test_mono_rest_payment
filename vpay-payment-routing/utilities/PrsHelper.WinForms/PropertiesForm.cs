using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace PrsHelper.WinForms
{
    public partial class PropertiesForm : Form
    {
        private ITemplateFile _selectedObject;
        private string _title;
        public PropertiesForm(string title, ITemplateFile selectedObject)
        {
            InitializeComponent();

            _title = title;
            this.Tag = title;
            this.Text = title;
            _selectedObject = selectedObject;
        }

        private void PropertiesForm_Load(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _selectedObject;
            if (propertyGrid1.SelectedObject is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)propertyGrid1.SelectedObject).PropertyChanged += PropertiesForm_PropertyChanged;
            }
        }

        private bool _needsSave = false;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool NeedsSave
        {
            get { return _needsSave; }
            set
            {

                _needsSave = value;
                if (_needsSave)
                {
                    this.Text = $"{_title} *";
                }
                else
                {
                    this.Text = _title;
                }
            }
        }
        private void PropertiesForm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            NeedsSave = true;
        }

        private void PropertiesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (NeedsSave)
            {
                DialogResult result = MessageBox.Show($"{Tag.ToString()} has changes.  Do you want to save your changes?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel);
                if (result.Equals(DialogResult.Yes))
                {
                    if (ShowSaveDialog() != DialogResult.OK)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                if (result.Equals(DialogResult.Cancel))
                {
                    e.Cancel = true;
                }
            }
        }

        private DialogResult ShowSaveDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.InitialDirectory = GetSaveFolder();
            saveFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            var result = saveFileDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                string FileName = saveFileDialog.FileName;

            }
            return result;
        }

        private string GetSaveFolder()
        {
            var lastFolder = string.Empty;
            switch (this.Tag.ToString())
            {
                case "Options":
                    lastFolder = Path.GetDirectoryName(UserSettings.Default.LastOptionFilename);
                    break;
                case "Batch Template":
                    lastFolder = Path.GetDirectoryName(UserSettings.Default.LastBatchTemplateFilename);
                    break;
                default:
                    lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    break;
            }
            return lastFolder;
        }
    }
}
