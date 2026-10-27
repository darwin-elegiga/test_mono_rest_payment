using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Generation.Tests.TestModels
{
    public class TestCustomPdfFile
    {

        public string Name { get; set; }

        public bool Exists { get; set; } = true;
        
        public override string ToString()
        {
            return $"{Name} - Exists = {Exists}";
        }
    }

    public class TestCustomPdfFileList: List<TestCustomPdfFile>
    {

        public override string ToString()
        {
            var sb = new StringBuilder();

            this.ForEach(x => sb.AppendLine(x.ToString()));

            return sb.ToString();
        }
    }
}
