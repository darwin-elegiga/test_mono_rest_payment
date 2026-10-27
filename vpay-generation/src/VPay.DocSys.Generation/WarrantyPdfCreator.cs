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
    public class WarrantyPdfCreator : IWarrantyPdfCreator
    {
        private static XName SourceAttributeXName => XName.Get("src");

        private readonly IStaticFileApiClient _staticFileApiClient;
        private readonly IFopApiClient _fopApiClient;
        private readonly FopBaseXsl _fopBaseXsl;
        private readonly ILogger<WarrantyPdfCreator> _logger;

        private readonly string _baseUri;
        private readonly XmlNamespaceManager _namespaceManager;

        public WarrantyPdfCreator(
            IStaticFileApiClient staticFileApiClient,
            IFopApiClient fopApiClient,
            FopBaseXsl fopBaseXsl,
            ApiSettings apiSettings,
            ILogger<WarrantyPdfCreator> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(staticFileApiClient, nameof(staticFileApiClient));
            ArgumentHelpers.ThrowIf.IsNull(fopApiClient, nameof(fopApiClient));
            ArgumentHelpers.ThrowIf.IsNull(fopBaseXsl, nameof(fopBaseXsl));
            ArgumentHelpers.ThrowIf.IsNull(apiSettings, nameof(apiSettings));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _staticFileApiClient = staticFileApiClient;
            _fopApiClient = fopApiClient;
            _fopBaseXsl = fopBaseXsl;
            _logger = logger;

            _baseUri = apiSettings[StaticFileApiClient.ServiceName].GetUri().AbsoluteUri;

            _namespaceManager = new XmlNamespaceManager(new NameTable());
            _namespaceManager.AddNamespace("fo", "http://www.w3.org/1999/XSL/Format");
        }

        public async Task<MemoryStream> CreatePdfStream(string xslUrlPath, XNode xNode)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(xslUrlPath, nameof(xslUrlPath));
            ArgumentHelpers.ThrowIf.IsNull(xNode, nameof(xNode));

            Stream tpaXsl;

            try
            {
                tpaXsl = await _staticFileApiClient.GetContentStream(xslUrlPath);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not fetch {xslUrlPath}, this is not a WARRANTY fax xml", e);
            }

            // first transform takes the fax xml
            // and the calculated tpa specific xsl
            // to return a custom intermediate xml
            _logger.LogInformation($"Transform xml using {xslUrlPath}");
            xNode = TransformXml(xNode, tpaXsl);

            // second transform takes the custom intermediate xml
            // and the fo base xsl
            // to generates the xsl-fo for the fop server
            _logger.LogInformation($"Transform xml using {FopBaseXsl.ResourceName}");
            xNode = TransformXml(xNode, _fopBaseXsl.GetXslContentStream());

            UpdateImageSources(xNode);

            return (await _fopApiClient.PostFile(xNode.ToString())) as MemoryStream;
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
                .Select(_ => _.Attribute(SourceAttributeXName))
                .Where(_ => _ != null && !string.IsNullOrWhiteSpace(_.Value));

            foreach (var source in externalGraphicSources)
            {
                var value = source.Value
                    // .../filename.ext => .../images/filename.ext
                    .Insert(source.Value.LastIndexOf("/", StringComparison.Ordinal), "/images")
                    // /clients/... => http:url/...
                    .Replace("clients/", _baseUri);

                value = $"url('{value}')";

                _logger.LogInformation($"Update {source.Value} to {value}");
                source.Value = value;
            }
        }
    }
}
