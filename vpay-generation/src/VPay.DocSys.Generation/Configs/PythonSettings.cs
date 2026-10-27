using System;
using System.Collections.Generic;

namespace VPay.DocSys.Generation.Configs
{
    public class PythonSettings
    {
        public string DefaultPythonExecutable {get; set; }
        public Dictionary<string, string> PythonExecutableMap {get; set;}
    }
}
