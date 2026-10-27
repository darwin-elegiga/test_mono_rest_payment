using System;
using System.IO.Abstractions;

namespace VPay.DocSys.Generation.Common.Edi835
{
    public class CompareFiles
    {
        private readonly IFileSystem _fileSystem;

        public CompareFiles(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        public FileCompareResult Compare(string fileA, string fileB)
        {

            FileCompareResult result = new FileCompareResult(fileA, fileB);
            if ( string.IsNullOrEmpty(fileA) ) {
                result.Differences.Add($"FileA is null or empty.");
                return result;
            }
            var fiA = _fileSystem.FileInfo.New(fileA);
            if (string.IsNullOrEmpty(fileB))
            {
                result.Differences.Add($"FileB is null or empty.");
                return result;
            }
            var fiB = _fileSystem.FileInfo.New(fileB);

            // both_files_should_exist
            if (!(fiA.Exists && fiB.Exists))
            {
                result.Differences.Add($"FileA {(fiA.Exists ? "exists" : "does not exist")}.");
                result.Differences.Add($"FileB {(fiB.Exists ? "exists" : "does not exist")}.");
                return result;
            }

            // extensions_should_match
            if (!fiA.Extension.Equals(fiB.Extension, StringComparison.InvariantCulture))
            {
                result.Differences.Add($"Extensions do not match.");
            }

            // prefix_should_match
            // Everything before the timestamp should be the same.
            int prefixLength = fiA.Name.LastIndexOf('_');
            if (!fiA.Name.Substring(0, prefixLength).Equals(fiB.Name.Substring(0, prefixLength), StringComparison.InvariantCulture))
            {
                result.Differences.Add($"File prefixes do not match.");
            }

            // length_should_match
            if (fiA.Length != fiB.Length)
            {
                result.Differences.Add($"File lengths do not match.");

            }

            // if anything doesn't match, don't do the more expensive content comparison.
            if (result.Differences.Count > 0) { return result; }
            // content_should_match
            int byteA, byteB;
            long position = 0;
            using (var fsA = fiA.OpenRead())
            using (var fsB = fiB.OpenRead())
            {
                do
                {
                    byteA = fsA.ReadByte();
                    byteB = fsB.ReadByte();
                    if (byteA != byteB)
                    {
                        result.Differences.Add($"Files do not match at position {position}.");
                    }
                    position++;
                } while ((byteA == byteB) && (byteA != -1));
            }

            return result;
        }

    }

}
