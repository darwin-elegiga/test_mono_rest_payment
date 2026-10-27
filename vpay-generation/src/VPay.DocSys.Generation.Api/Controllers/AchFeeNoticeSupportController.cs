using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation.Api.Controllers
{

    [ApiController]
    [ApiVersionNeutral]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class AchFeeNoticeSupportController : ControllerBase
    {
        private readonly IEftFaxPdfCreator _eftFaxPdfCreator;

        public AchFeeNoticeSupportController(IEftFaxPdfCreator eftFaxPdfCreator)
        {
            _eftFaxPdfCreator = eftFaxPdfCreator;
        }

        /// <summary>
        /// Will create an ACH Fee Notice pdf for the given values in <see cref="AchFeeNoticeRequest"/>
        /// </summary>
        /// <param name="request">values used in the fee notice, no validation</param>
        /// <returns>an ACH fee notice pdf</returns>
        [HttpPost("Sample")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAchFeeNotice(AchFeeNoticeRequest request)
        {
            var xDocument = new XDocument(
                new XDeclaration("1.0", "Windows-1252", "yes"),
                new XElement("notificationRequest",
                    new XElement("control"),
                    new XElement("sendTo",
                        new XElement("name", request.Name),
                        new XElement("addressLine1", request.AddressLine1),
                        new XElement("addressLine2", request.AddressLine2),
                        new XElement("addressLine3", request.AddressLine3)
                    ),
                    new XElement("card",
                        new XElement("loadTransactionID", request.TransactionId),
                        new XElement("loadAmount", request.LoadAmount)
                    ),
                    new XElement("coveredItem",
                        new XElement("userDisplay3", request.PaymentsCount),
                        new XElement("TotalOfEftPayments", request.AchAmount)
                    )
                )
            );

            var result = await _eftFaxPdfCreator.CreatePdfStream(xDocument).ConfigureAwait(false);

            return File(result, "application/pdf", $"TEST_ACH-Fee-Notice-{DateTime.Now:yyyymmddhhMMss}.pdf");
        }
    }
}
