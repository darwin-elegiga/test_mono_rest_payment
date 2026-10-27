using System.Data;
using System.Text;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace PrsHelper
{
    public class PdfHelper
    {
        public static IEnumerable<string> CreatePdfs(List<PaymentRecordDto> payments, string tempFolder, bool isSubPayerSpecific)
        {
            foreach (var p in payments.Where(p => p.TransactionType == "835"))
            {
                var pdfName = isSubPayerSpecific
                    ? $"{p.PayerSubpayerID}_{p.UnconsolPaymentNbr}.pdf"
                    : $"{p.UnconsolPaymentNbr}.pdf";

                var copyToPdfFilename = Path.Combine(tempFolder, pdfName);

                if (!File.Exists(copyToPdfFilename))
                {
                    CreateRotatedPdf(copyToPdfFilename,
                        p.UnconsolPaymentNbr,
                        $"OptumBatchID: {p.BatchID}",
                        $"PayerAlias: {p.PayerAlias}",
                        $"Payer835ID: {p.Payer835ID}",
                        $"ProvTIN: {p.ProvTIN}",
                        $"PayerSubpayerID: {p.PayerSubpayerID}",
                        $"UconslPaymentAmount: {p.UconslPaymentAmount}"
                        );
                }
                yield return pdfName;
            }
        }


        private static void CreateRotatedPdf(string filePath, params string[] content)
        {
            // 72 points / inch... raw numbers appear to be in points...
            // 1/2" = 36pt
            // 1/4" = 18pt
            // 1/8" = 9pt
            using var document = new PdfDocument();

            AddPage(document, PageOrientation.Portrait, content);

            AddPage(document, PageOrientation.Landscape, content);

            document.Save(filePath);
        }

        private static void AddPage(PdfDocument document, PageOrientation orientation, params string[] content)
        {

            var page = document.AddPage();
            page.Orientation = orientation;
            var gfx = XGraphics.FromPdfPage(page);

            var borderRect = new XRect(0, 0, page.Width, page.Height);

            var lines = content.ToList();

            lines.Add($"Incoming Orientation: {orientation}");
            lines.Add("");
            lines.Add($"Page # {document.PageCount}");

            DrawBorder(gfx, borderRect, 18, 2);
            DrawText(gfx, page, lines.ToArray());
            gfx.Dispose();

        }



        private static void DrawText(XGraphics gfx, PdfPage page, params string[] content)
        {
            var font = new XFont("Arial", 20, XFontStyle.Bold);
            var blackBrush = XBrushes.Black;
            var layout = new XRect(0, 0, page.Width, page.Height);
            var format = XStringFormats.Center;


            double currentLine = 80;
            double spacing = 9;
            foreach (var line in content)
            {
                var lineHeight = gfx.MeasureString(line, font).Height;
                gfx.DrawString(line, font, blackBrush, layout.Center.X, currentLine, format);
                currentLine += (lineHeight + spacing);

            }

        }



        /// <summary>
        /// Draws a border around the outside of a page, offset in point with a line width also in points.
        /// </summary>
        /// <param name="gfx"></param>
        /// <param name="rectX"></param>
        /// <param name="offset"></param>
        /// <param name="lineWidth"></param>
        private static void DrawBorder(XGraphics gfx, XRect rectX, double offset = 18, double lineWidth = 2)
        {
            // "shrink" the page size as the border...
            var rect = new XRect(offset, offset, rectX.Width - (offset * 2), rectX.Height - (offset * 2));
            var pen = new XPen(XColors.Black, lineWidth);

            gfx.DrawLine(XPens.Black, rect.TopLeft, rect.TopRight);
            gfx.DrawLine(XPens.Black, rect.TopRight, rect.BottomRight);
            gfx.DrawLine(XPens.Black, rect.BottomRight, rect.BottomLeft);
            gfx.DrawLine(XPens.Black, rect.BottomLeft, rect.TopLeft);

        }
    }
}
