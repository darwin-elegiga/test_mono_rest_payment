using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Xsl;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation
{
    public class ReminderFaxPdfCreator : IReminderFaxPdfCreator
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(CoverSheetParameters));

        private readonly IFopApiClient _fopApiClient;
        private readonly ILogger<ReminderFaxPdfCreator> _logger;

        private readonly XslCompiledTransform _transform;

        public ReminderFaxPdfCreator(
            ReminderFaxBaseXsl reminderFaxBaseXsl,
            IFopApiClient fopApiClient,
            ILogger<ReminderFaxPdfCreator> logger)
        {
            ThrowIf.IsNull(reminderFaxBaseXsl, nameof(reminderFaxBaseXsl));
            ThrowIf.IsNull(fopApiClient, nameof(fopApiClient));
            ThrowIf.IsNull(logger, nameof(logger));

            _fopApiClient = fopApiClient;
            _logger = logger;

            _transform = new XslCompiledTransform();

            using (var xslContentStream = reminderFaxBaseXsl.GetXslContentStream())
            using (var xslReader = XmlReader.Create(xslContentStream))
            {
                _transform.Load(xslReader);
            }
        }

        public async Task<MemoryStream> CreatePdfStream(CoverSheetParameters coverSheetParameters)
        {
            using (_logger.BeginScope(coverSheetParameters.ToDictionary()))
            {
                XNode input;
                var output = new XDocument();

                using (var sw = new StringWriter())
                {
                    _serializer.Serialize(sw, coverSheetParameters ?? new CoverSheetParameters());

                    input = XDocument.Parse(sw.ToString());
                    _logger.LogInformation("Serialize Cover Sheet Parameters: {CoverSheetParameters}", input);
                }

                using (var xmlReader = input.CreateReader())
                using (var xmlWriter = output.CreateWriter())
                {
                    _transform.Transform(xmlReader, xmlWriter);
                    _logger.LogInformation("Transform to fo-xml");
                }

                return (await _fopApiClient.PostFile(output.ToString())) as MemoryStream;
            }
        }
    }
}
