using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    /// <summary>
    /// Python Runner
    /// </summary>
    public interface IPythonRunner
    {
        /// <summary>
        /// Create VPayge Document
        /// </summary>
        /// <param name="scriptName">python script name</param>
        /// <param name="method">method in python script</param>
        /// <param name="outputFile">output file name string</param>
        /// <param name="coverPageData">data for creating VPayge</param>
        /// <param name="tpaConfigFile">TPA config file name</param>
        /// <param name="pythonVersion">The version of python to use. Either "2" or "3"</param>
        /// <returns>file path for VPayge document</returns>
        Task<string> CreateVPayge(string scriptName, string method, string outputFile, CoverPageData coverPageData, string tpaConfigFile, string pythonVersion);

        /// <summary>
        /// Overlay extra info on the EOB
        /// </summary>
        /// <param name="scriptName">python script name</param>
        /// <param name="pdfFile">The file to overlay</param>
        /// <param name="isCheck">if <see cref="CheckInfo"/> was used to get overlay information</param>
        /// <param name="overlayId">Id value to overlay</param>
        /// <param name="overlayDate">Date value to overlay</param>
        /// <returns>file path for modified EOB document</returns>
        Task<string> OverlayEob(string scriptName, string pdfFile, bool isCheck, string overlayId, DateTime overlayDate);

        /// <summary>
        /// Create EOB Document
        /// </summary>
        /// <param name="scriptName">python script name</param>
        /// <param name="outputFile">output file name string</param>
        /// <param name="eobXmlFile">EOB xml data file name</param>
        /// <param name="auxFiles">"aux_files" setting from <see cref="DocSysConfiguration"/>.EnvironmentSettings</param>
        /// <param name="payType">PAYTYPE from <see cref="StatusRecord"/></param>
        /// <param name="pythonVersion">The version of python to use. Either "2" or "3"</param>
        /// <param name="overlayId">Id value to overlay</param>
        /// <param name="overlayDate">Date value to overlay</param>
        /// <param name="isCheck">if <see cref="CheckInfo"/> was used to get overlay information</param>
        /// <returns>file path for EOB document</returns>
        Task<string> CreateEob(string scriptName, string outputFile, string eobXmlFile, string auxFiles, string payType, string pythonVersion, string overlayId, DateTime overlayDate, bool isCheck);

        /// <summary>
        /// Create Custom SendBack Document
        /// </summary>
        /// <param name="checkInfoXmlFilePath">file containing <see cref="SendBackCheckXmlFileWriter"/> data</param>
        /// <returns>file path for clear check</returns>
        Task<string[]> CreateCustomSendBacks(string checkInfoXmlFilePath);

        Task<string> GetVersionInfo(string versionNumber);
        string GetPythonExecutable(string pythonEnvVersion);
        Task<List<string>> GetPythonPackageList(string pythonEnvVersion);
    }
}
