using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrsHelper
{
    public interface ICommandFormatter
    {

        string Extension { get; }

        string[] scriptHeader(Options options);

        string echo(string text);
        string comment(string text);
        string curl_post(string url, string[] headers, string data);
        string scp(string sourceFilename, string userName, string serverName, string destFolder);
        string ssh(string userName, string serverName, string cmd);
        string pause(string prompt = "Press enter to continue...");
    }
}
