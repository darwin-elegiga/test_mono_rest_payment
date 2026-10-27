using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrsHelper.Formatters
{
    public class DosCommandFormatter : ICommandFormatter
    {

        public string Extension => ".cmd";



        public string comment(string text)
        {
            return $"REM {text}";
        }

        public string curl_post(string url, string[] headers, string data)
        {
            if (data.StartsWith("@"))
            {
                data = EnsureValidPathSparators(data);
            }

            var formattedHeaders = string.Join(' ', headers.Select(_ => $"-H \"{_}\""));
            return $"curl -X POST \"{url}\" -k {formattedHeaders} -d \"{data.Replace("\"", "\\\"")}\"";
        }

        private string EnsureValidPathSparators(string data)
        {


            if (!string.IsNullOrWhiteSpace(data))
            {
                var justFilename = Path.GetFileName(data);
                if (!data.Equals(justFilename))
                {
                    if (!Path.IsPathFullyQualified(data))
                    {
                        data = $".{Path.DirectorySeparatorChar}{data}";
                    }
                }
            }

            return data.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);



        }

        public string echo(string text)
        {
            return $"echo {text}";
        }

        public string scp(string sourceFilename, string userName, string serverName, string destFolder)
        {
            var justFilename = Path.GetFileName(sourceFilename);

            var scpDest = $"{userName}@{serverName}:{destFolder}";
            return $"scp {EnsureValidPathSparators(sourceFilename)} {scpDest}/{justFilename}";
        }

        public string ssh(string userName, string serverName, string cmd)
        {
            var sshDest = $"{userName}@{serverName}";
            return $"ssh {sshDest} \"{cmd}\"";
        }

        public string pause(string prompt = "Press any key to continue...")
        {
            return $"pause";
        }

        public string[] scriptHeader(Options options)
        {
            List<string> script = new List<string>();

            script.Add("@echo off");
            script.Add("");

            return script.ToArray();
        }
    }
}
