using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;
using VPay.DocSys.Generation.Models;

namespace VPay.DocSys.Generation
{
    public class EftFaxPdfCreator : IEftFaxPdfCreator
    {
        private static XName SourceAttributeXName => XName.Get("src");


        private readonly IStaticFileApiClient _staticFileApiClient;
        private readonly IFopApiClient _fopApiClient;
        private readonly EftFaxFopXsl _eftFaxFopXsl;
        private readonly ILogger<EftFaxPdfCreator> _logger;

        private readonly string _baseUri;
        private readonly XmlNamespaceManager _namespaceManager;

        public EftFaxPdfCreator(
            IStaticFileApiClient staticFileApiClient,
            IFopApiClient fopApiClient,
            EftFaxFopXsl eftFaxFopXsl,
            ApiSettings apiSettings,
            ILogger<EftFaxPdfCreator> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(staticFileApiClient, nameof(staticFileApiClient));
            ArgumentHelpers.ThrowIf.IsNull(fopApiClient, nameof(fopApiClient));
            ArgumentHelpers.ThrowIf.IsNull(eftFaxFopXsl, nameof(eftFaxFopXsl));
            ArgumentHelpers.ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _staticFileApiClient = staticFileApiClient;
            _fopApiClient = fopApiClient;
            _eftFaxFopXsl = eftFaxFopXsl;
            _logger = logger;

            _baseUri = apiSettings[StaticFileApiClient.ServiceName].GetUri().AbsoluteUri;

            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("fo", "http://www.w3.org/1999/XSL/Format");
        }

        public async Task<MemoryStream> CreatePdfStream(XNode xNode)
        {
            ArgumentHelpers.ThrowIf.IsNull(xNode, nameof(xNode));

            using (var xslContentStream = _eftFaxFopXsl.GetXslContentStream())
            {
                // transform takes the custom intermediate xml
                // and the fo base xsl
                // to generates the xsl-fo for the fop server
                _logger.LogInformation($"Transform xml using {EftFaxFopXsl.ResourceName}");
                xNode = AddTransactionPeriodElement(xNode);
                xNode = TransformXml(xNode, xslContentStream);
            }
            UpdateImageSources(xNode);
            return (await _fopApiClient.PostFile(xNode.ToString())) as MemoryStream;
        }

        private XNode AddTransactionPeriodElement(XNode xNode)
        {
            DateTime lastDayLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            string transactionPeriod = lastDayLastMonth.ToString("MMMM - yyyy");
            XElement notificationRequestElement = xNode.XPathSelectElement("/notificationRequest/control");
            XElement transactionPeriodElement = new XElement("transactionPeriod", transactionPeriod);

            try
            {
                notificationRequestElement.AddAfterSelf(transactionPeriodElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred in EftFaxPdfCreator - trying to add transaction period to XML string.");
            }

            return xNode;
        }

        private static XDocument TransformXml(XNode xNode, Stream xslContentStream)
        {

            var xslTransform = new XslCompiledTransform();
            var output = new XDocument();

            using (xslContentStream)
            using (var xslReader = XmlReader.Create(xslContentStream))
            using (var xmlReader = xNode.CreateReader())
            using (var xmlWriter = output.CreateWriter())
            {
                xslTransform.Load(xslReader);
                xslTransform.Transform(xmlReader, xmlWriter);
            }

            return output;
        }

        private void UpdateImageSources(XNode xNode)
        {
            // src is in the format "clients/clientCode/filename.ext" or "clients/filename.ext"
            var externalGraphicSources = xNode
                .XPathSelectElements(".//fo:external-graphic", _namespaceManager)
                .Select(_ => _.Attribute(SourceAttributeXName));

            foreach (var source in externalGraphicSources)
            {
                var value = source.Value
                    .Replace("clients/", _baseUri);
                value = $"url('{value}')";

                _logger.LogInformation($"Update {source.Value} to {value}");
                source.Value = value;
            }
        }
    }
}
