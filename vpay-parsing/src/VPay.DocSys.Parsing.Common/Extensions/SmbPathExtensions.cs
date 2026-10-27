using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VPay.DocSys.Parsing.Common
{
    public static class SmbPathExtensions
    {
        /// <summary>
        /// Values that cannot be path components in an smb share or windows directory
        /// </summary>
        private static readonly List<string> _invalid_Smb_Names = new List<string>
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>
        /// If the name value equals any of the 
        /// <see cref="_invalid_Smb_Names"/> (case insensitive), 
        /// append 1 to the name
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string EnsureIsSmbSafeName(this string nameOfFileOrFolder)
        {
            if (_invalid_Smb_Names.Any(p => p.Equals(nameOfFileOrFolder,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                return nameOfFileOrFolder + "1";
            }

            return nameOfFileOrFolder;
        }

        /// <summary>
        /// If any of the path components equal any of the 
        /// <see cref="_invalid_Smb_Names"/> (case insensitive),
        /// append 1 to the value of the component (recursive for every instance)
        /// </summary>
        /// <param name="path">file or directory path or name to verify</param>
        /// <returns></returns>
        public static string EnsureIsSmbSafePath(this string path)
        {
            // match a path or filename with or without an extension which contains an
            // invalid smb directory name or file name
            var regex = new Regex($@"(^|\\|/)({string.Join("|", _invalid_Smb_Names)})(?=$|\.|\\|/)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            return regex.Replace(path, EnsureIsSafeSmbPathMatchEvaluator);
        }

        /// <summary>
        /// The .net equivalent of $01 so that $0 is retained
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private static string EnsureIsSafeSmbPathMatchEvaluator(Match match)
        {
            return match.Groups[0] + "1";
        }
    }
}
