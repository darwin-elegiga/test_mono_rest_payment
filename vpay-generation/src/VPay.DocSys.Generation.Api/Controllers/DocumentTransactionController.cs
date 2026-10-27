using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace VPay.DocSys.Generation.Api.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class DocumentTransactionController : ControllerBase
    {
        private readonly ILogger<DocumentTransactionController> _logger;
        private readonly IDocumentPaymentTransactionService _documentPaymentTransactionService;
        public DocumentTransactionController(ILogger<DocumentTransactionController> logger, IDocumentPaymentTransactionService documentPaymentTransactionService) {

            _logger = logger;
            _documentPaymentTransactionService = documentPaymentTransactionService;
        }


        [HttpGet("GetPaymentByTransactionID/{transactionId}")]
        public async Task<IActionResult> GetPaymentByTransaction([Required] long transactionId, CancellationToken cancellationToken)
        {
            var result = await _documentPaymentTransactionService.GetDocumentPaymentDetails(transactionId);

            if (result == null)
            {
                _logger.LogInformation("No Payment Record found for transactionId: {transactionId}", transactionId);
                return NotFound(new
                {
                    Status = 404,
                    Title = "Not Found",
                    Detail = $"No Payment Record found for transactionId: {transactionId}."
                });
            }

            return Ok(result);
        }

    }
}
