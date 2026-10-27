using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrsHelper.WinForms
{
    public partial class BatchTemplateControl : UserControl
    {
        private BatchMananger _batchMananger;

        public BatchMananger BatchMananger { get { return _batchMananger; } }


        public BatchTemplateControl()
        {
            InitializeComponent();
            _batchMananger = BatchMananger.LoadFromSettings();
        }

        private void BatchTemplate_Load(object sender, EventArgs e)
        {
            //propertyGridOptions.SelectedObject = _batchMananger.Options;
            //propertyGrid1.SelectedObject = _batchMananger.BatchTemplate;
        }
    }
}
