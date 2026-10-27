using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Pdf.Tools;

namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiVersionNeutral]
    [Produces("application/json")]
    [Route("api/pdfs")]
    [ApiController]
    [DisableRequestSizeLimit]
    public class PdfsController : ControllerBase
    {
        private const string PdfContentType = "application/pdf";

        private readonly IPdfTools _pdfTools;
        private readonly IFileSystem _fileSystem;
        private readonly ITiffOperations _tiffOperations;
        private readonly ILogger _logger;

        public PdfsController(
            IPdfTools pdfTools,
            IFileSystem fileSystem,
            ITiffOperations tiffOperations,
            ILogger<PdfsController> logger)
        {
            _pdfTools = pdfTools;
            _fileSystem = fileSystem;
            _tiffOperations = tiffOperations;
            _logger = logger;
        }

        /// <summary>
        /// Merges a list of pdfs into a new pdf
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/pdfs/merge
        ///     {
        ///        "ouputFileName": "output.pdf",
        ///        "files": [ "file1.pdf", "file2.pdf" ]
        ///     }
        ///
        /// </remarks>
        /// <param name="pdfParam"></param>
        /// <returns>An object with merge file location and name and if it successfully merged</returns>
        /// <response code="200">Returns an object with merge file location and name and if it successfully merged</response>
        [HttpPost("merge")]
        [ProducesResponseType(typeof(GenericPdfResponse), 200)]
        public async Task<IActionResult> PostMerge(MergePdfParam pdfParam)
        {
            _logger.LogInformation(1001, "Merging {COUNT} pdfs into {OUTPUT}", pdfParam.Files.Count, pdfParam.OutputFileName);
            var result = await _pdfTools.MergeAsync(pdfParam.OutputFileName, pdfParam.Files);

            return Ok(new GenericPdfResponse() { IsSuccessful = result, OutputFileName = pdfParam.OutputFileName });
        }

        /// <summary>
        /// Stamps a pdf page(s) onto an existing pdf.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST api/pdfs/stamp
        ///     {
        ///        "ouputFileName": "output.pdf",
        ///        "fileName": "file.pdf",
        ///        "stampFileName": "fileToUseAsStamp.pdf",
        ///        "useAllPagesOfStampFile": false
        ///     }
        ///
        /// </remarks>
        /// <param name="pdfParam"></param>
        /// <returns>An object with stamped pdf's file location and name and if it successfully stamped</returns>
        /// <response code="200">Returns an object with stamped pdf's file location and name and if it successfully stamped</response>
        [HttpPost("stamp")]
        [ProducesResponseType(typeof(GenericPdfResponse), 200)]
        public async Task<IActionResult> PostStamp(StampPdfParam pdfParam)
        {
            var result = await _pdfTools.StampAsync(pdfParam.OutputFileName, pdfParam.FileName, pdfParam.StampFileName);

            return Ok(new GenericPdfResponse() { IsSuccessful = result, OutputFileName = pdfParam.OutputFileName });
        }

        /// <summary>
        /// Returns the page count of a given pdf file
        /// </summary>
        /// <param name="fileName">pdf file location</param>
        /// <returns>An object with pdf page count</returns>
        [HttpPost("pageCount")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> PageCount([FromBody]string fileName)
        {
            var result = await _pdfTools.GetNumberOfPagesAsync(fileName);

            return Ok(result);
        }

        [HttpPost("convertToTiff")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [Produces("application/json", "application/octet-stream", "text/plain")]
        public async Task<IActionResult> ConvertToTiff(
            IFormFile pdfFile,
            [MaxLength(256)] string tiffFileName = null)
        {
            // 1. Validate if a file was selected
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequest("No file selected.");
            }

            if (string.IsNullOrWhiteSpace(tiffFileName))
            {
                tiffFileName = $"{Path.GetFileNameWithoutExtension(pdfFile.FileName)}.tiff";
            }

            // 2. Enforce the PDF content type
            if (!pdfFile.ContentType.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only PDF files are allowed.");
            }

            // 3. Enforce maximum file size (e.g., 5MB)
            const long MaxFileSize = 5 * 1024 * 1024;
            if (pdfFile.Length > MaxFileSize)
            {
                return BadRequest("File size exceeds the 5MB limit.");
            }

            try
            {
                var uploadFolder = CreateTemporaryDirectory();

                // 4. Generate a unique, safe file name
                string uploadFileName = $"{Guid.NewGuid()}_{Path.GetFileName(pdfFile.FileName)}";
                string pdfFilePath = Path.Combine(uploadFolder, uploadFileName);

                // 5. Save the PDF file stream asynchronously
                using (var stream = new FileStream(pdfFilePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                // 6. Convert the PDF to TIFF using the ITiffOperations service
                var tiffFilePath = await _tiffOperations.CreateTiffFromPdf(pdfFilePath);

                // 7. Open TIFF file and copy to memory stream
                var memoryStream = new MemoryStream();
                using (FileStream fileStream = new FileStream(tiffFilePath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.CopyTo(memoryStream);
                    fileStream.Close();
                }
                memoryStream.Position = 0;

                DeleteTemporaryDirectory(uploadFolder);

                // 8. Return FileStreamResult from memory stream
                return File(memoryStream, PdfContentType, tiffFileName);
            }
            catch (Exception ex)
            {
                return InvalidOperationProblemDetail($"Unable to convert PDF to TIFF: {ex.Message}");
            }
        }

        private string CreateTemporaryDirectory()
        {
            var tempDirectory = $"/tmp/{Guid.NewGuid()}";

            _fileSystem.Directory.CreateDirectory(tempDirectory);
            _logger.LogDebug("Created directory {Directory}", tempDirectory);

            return tempDirectory;
        }

        private string DeleteTemporaryDirectory(string tempDirectory)
        {
            _fileSystem.Directory.Delete(tempDirectory, true);
            _logger.LogDebug("Deleted directory {Directory}", tempDirectory);
            return tempDirectory;
        }

        private IActionResult InvalidOperationProblemDetail(string detail)
        {
            return Problem
            (
                title: "Invalid Operation",
                type: "https://httpstatuses.com/500",
                statusCode: 500,
                detail: detail
            );
        }
    }
}
