using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class BundledFaxSupportController : ControllerBase
    {
        private readonly IBundledFaxService _bundledFaxService;
        private readonly IFileSystem _fileSystem;

        public BundledFaxSupportController(
            IBundledFaxService bundledFaxService,
            IFileSystem fileSystem)
        {
            _bundledFaxService = bundledFaxService;
            _fileSystem = fileSystem;
        }

        [HttpPost("CoverPage")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateReminderFaxCoverPage(BundledFaxParams request)
        {
            var result = await _bundledFaxService.CreateCoverPage(request).ConfigureAwait(false);

            return File(_fileSystem.File.ReadAllBytes(result), "application/pdf", $"TEST_BFAX_Cover_sheet-{DateTime.Now:yyyymmddhhMMss}.pdf");
        }
    }
}
