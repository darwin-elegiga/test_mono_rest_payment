using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class DocSysConfig
    {

        public DocSysConfig(DocSysConfiguration config)
        {
            if (config == null)
            {
                return;
            }
            SePhoneNumber = GetSafeDictionaryValue(config.EnvironmentSettings, "se_phone_number");
            ReturnAddress = GetSafeDictionaryValue(config.AppSettings, "return_address") ?? "";
            TpaFullName = GetSafeDictionaryValue(config.EnvironmentSettings, "tpa_fullname");
            CoverDate = GetSafeDictionaryValue(config.EnvironmentSettings, "cover_date");
            CoverMakerProgramName = GetSafeDictionaryValue(config.EnvironmentSettings, "covermaker_progname");
            TpaConfigFile = GetSafeDictionaryValue(config.EnvironmentSettings, "tpa_config_file");
            NoEobRptFlag = GetSafeDictionaryValue(config.EnvironmentSettings, "no_eob_rpt_flag");
            EobMakerProgramName = GetSafeDictionaryValue(config.EnvironmentSettings, "rpt_progname");
            RptPythonVersion = GetSafeDictionaryValue(config.EnvironmentSettings, "rpt_python_version") ?? "3";
            CoverMakerPythonVersion = GetSafeDictionaryValue(config.EnvironmentSettings, "covermaker_python_version") ?? "3";
            EobOverlayer = GetSafeDictionaryValue(config.EnvironmentSettings, "eob_overlayer");
        }

        public string SePhoneNumber { get; set; }

        public string ReturnAddress { get; set; }

        public string TpaFullName { get; set; }

        public string CoverDate { get; set; }

        public string CoverMakerProgramName { get; set; }

        public string TpaConfigFile { get; set; }

        public string NoEobRptFlag { get; set; }

        public string EobMakerProgramName { get; set; }

        public string RptPythonVersion { get; set; }

        public string CoverMakerPythonVersion { get; set; }

        public string EobOverlayer { get; set; }

        private static string GetSafeDictionaryValue(IDictionary<string, string> dic, string key)
        {
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }

            return null;
        }
    }
}
