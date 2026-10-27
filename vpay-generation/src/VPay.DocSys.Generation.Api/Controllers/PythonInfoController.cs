using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Controllers
{
    public class PythonInfo
    {
        public string VersionNumber { get; private set; }
        public string VersionData { get; private set; }
        public string Executable { get; private set; }
        public List<string> Packages { get; private set; }

        public PythonInfo(string versionData, string versionNumber, string executable, List<string> packages)
        {
            this.VersionNumber = versionNumber;
            this.VersionData = versionData;
            this.Executable = executable;
            this.Packages = packages;
        }

        public static async Task<PythonInfo> BuildPythonInfo(string versionNumber, IPythonRunner pythonRunner)
        {
            var versionData = await pythonRunner.GetVersionInfo(versionNumber);
            var executable = pythonRunner.GetPythonExecutable(versionNumber);
            var packages = await pythonRunner.GetPythonPackageList(versionNumber);
            var info = new PythonInfo(versionData, versionNumber, executable, packages);
            return info;
        }
    }

    [ApiVersionNeutral]
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class PythonInfoController : ControllerBase
    {

        private readonly IPythonRunner _pythonRunner;

        public PythonInfoController(IPythonRunner pythonRunner)
        {
            this._pythonRunner = pythonRunner;
        }

        [HttpGet("{versionNumber}")]
        [ProducesResponseType(typeof(PythonInfo), 200)]
        public async Task<ActionResult> GetDetail(string versionNumber)
        {
            return Ok(await PythonInfo.BuildPythonInfo(versionNumber, this._pythonRunner));
        }
    }
}
