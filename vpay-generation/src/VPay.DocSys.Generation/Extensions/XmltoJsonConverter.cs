using System.IO;
using System.Text.Json;
using System.Xml.Xsl;
using System.Xml;
using VPay.DocSys.Common;
using System.Reflection;
using System.Xml.XPath;

namespace VPay.DocSys.Generation.Extensions
{
    public static class XmlToJsonConverter
    {
        public static string TransformXmlToJson(XPathDocument xpathDoc, StatusRecord str, string xsltResourceName)
        {
            var xslt = new XslCompiledTransform();
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(xsltResourceName))
            using (var reader = XmlReader.Create(stream!))
            {
                xslt.Load(reader);
            }

            // Step 2: Apply XSLT Transformation
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter);
            xslt.Transform(xpathDoc, null, xmlWriter);

            // Step 3: Load Transformed XML
            var transformedXml = stringWriter.ToString();
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(transformedXml);

            // Step 3: Convert XML to JSON
            string json = JsonSerializer.Serialize(new
            {
                PatientName = xmlDoc.SelectSingleNode("/Transform/PatientName")?.InnerText,
                PatientDOB = xmlDoc.SelectSingleNode("/Transform/PatientDOB")?.InnerText,
                ClaimId = xmlDoc.SelectSingleNode("/Transform/ClaimId")?.InnerText,
                PatientAccountNumber = xmlDoc.SelectSingleNode("/Transform/PatientAccountNumber")?.InnerText,
                Amount = xmlDoc.SelectSingleNode("/Transform/Amount")?.InnerText,
                ClaimDate = xmlDoc.SelectSingleNode("/Transform/ClaimDate")?.InnerText,
                SubscriberName = xmlDoc.SelectSingleNode("/Transform/SubscriberName")?.InnerText,
                MainGovtIdNumber = !string.IsNullOrWhiteSpace(xmlDoc.SelectSingleNode("/Transform/MainGovtIdNumber")?.InnerText)
                   ? xmlDoc.SelectSingleNode("/Transform/MainGovtIdNumber")!.InnerText.Trim() : str.TIN.Trim(),
                PayeeName = xmlDoc.SelectSingleNode("/Transform/PayeeName")?.InnerText,
                PayeeAddress1 = xmlDoc.SelectSingleNode("/Transform/PayeeAddress1")?.InnerText,
                PayeeCity = xmlDoc.SelectSingleNode("/Transform/PayeeCity")?.InnerText,
                PayeeState = xmlDoc.SelectSingleNode("/Transform/PayeeState")?.InnerText,
                PayeePostalCode = xmlDoc.SelectSingleNode("/Transform/PayeePostalCode")?.InnerText
            }, new JsonSerializerOptions { WriteIndented = true });

            return json;
        }
    }
}
