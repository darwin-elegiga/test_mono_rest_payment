using System.Configuration;

namespace PrsHelper.WinForms
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _batchMananger = BatchMananger.LoadFromSettings();
            pgridOptions.SelectedObject = _batchMananger.Options;
            BatchSummaryPropertyGrid.SelectedObject = _batchMananger.BatchTemplate;
        }


        BatchMananger _batchMananger;

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabOptions.Focus();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnOptionsSave_Click(object sender, EventArgs e)
        {

        }

        private void btnOptionsCancel_Click(object sender, EventArgs e)
        {

        }

        private void BatchSummaryPropertyGrid_Click(object sender, EventArgs e)
        {

        }

        private void openOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO Open
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenBatchSummaryFile();
        }

        private void OpenBatchSummaryFile()
        {
            WarnCurrentBatchSummaryHasChanges();

            // First,
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files|*.json";
            var result = openFileDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                OpenBatchSummaryFile(openFileDialog.FileName);
            }
        }

        private void OpenBatchSummaryFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }
            _batchMananger.LoadBatchTemplate(fileName);
 
        }

        private void WarnCurrentBatchSummaryHasChanges()
        {
            //TODO: add change tracking so we can know this
            //TODO: offer to save/save as/cancel
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsBtnExecute_Click(object sender, EventArgs e)
        {
            Execute();
        }

        private void executeF5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Execute();
        }

        private void Execute()
        {
            MessageBox.Show("Hello");
        }

        private void MainToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
