using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PrsHelper.Formatters
{
    public class BashCommandFormatter : ICommandFormatter
    {
        public string Extension => ".sh";

 

        public string comment(string text)
        {
            return $"# {text}";
        }

        public string curl_post(string url, string[] headers, string data)
        {
            if (data.StartsWith("@"))
            {
                data = "@"+ EnsureValidPathSparators(data[1..]);
            }

            var formattedHeaders = string.Join(' ', headers.Select(_ => $"-H '{_}'"));

            return $"curl -X POST \"{url}\" -k {formattedHeaders} -d '{data}'";
        }
        private string EnsureValidPathSparators(string data)
        {
            if ( !string.IsNullOrWhiteSpace(data) )
            {
                var justFilename = Path.GetFileName(data);
                if (! data.Equals(justFilename))
                {
                    if (!Path.IsPathFullyQualified(data))
                    {
                        data = $".{Path.AltDirectorySeparatorChar}{data}";
                    }
                }
            }

            return data.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
            return $"ssh {sshDest} '{EscapeSingleQuotes(cmd)}'";
        }

        public string pause(string prompt = "Press any key to continue..." )
        {
 
            return $"read -p '{prompt}'";
        }

        private string EscapeSingleQuotes(string cmd)
        {
            return cmd.Replace("'", "\\'");
        }

        public string[] scriptHeader(Options options)
        {
            List<string> script = new List<string>();

            if (options.IncludeBinBash)
            {
                script.Add("#!/bin/bash");
                script.Add("");
                if (options.UseVariableForHost)
                {
                    if (options.Verbose)
                    {
                        script.Add(comment("use hostUri to be able to easily manipulate the script to be able to change to a different host."));
                    }
                    script.Add($"hostUri={options.HostUri}");
                    script.Add("");
                }
            }

            return script.ToArray();
        }
    }
}
