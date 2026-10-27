using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class FileCompareResult
    {
        public FileCompareResult(string fileA, string fileB)
        {
            FileA = fileA??"";
            FileB = fileB??"";
        }
        
        /// <summary>
        /// If we get an exception, we want the result of the compare to be false.
        /// This construction encapsulates that functionality.
        /// </summary>
        public FileCompareResult(string errorMessage)
        {
            FileA = "";
            FileB = "";
            Differences.Add(errorMessage);
        }

        public string FileA { get; set; }
        public string FileB { get; set; }

        public bool NoDifferencesFound {
            get
            {
                return _differences.Count == 0;
            }
        }

        private List<string> _differences = new List<string>();
        public List<string> Differences
        {
            get
            {
                return _differences;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FileA: '{(string.IsNullOrEmpty(FileA)?"<empty>":FileA)}'");
            sb.AppendLine($"FileB: '{(string.IsNullOrEmpty(FileB)?"<empty>":FileB)}'");
            if ( NoDifferencesFound ) { sb.AppendLine($" ! Files are Same !"); }
            foreach (var diff in Differences)
            {
                sb.AppendLine($" > {diff}");
            }
            sb.AppendLine("----------");
            return sb.ToString();
        }


    }

}
