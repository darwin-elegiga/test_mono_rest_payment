using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using ImageMagick;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using VPay.DocSys.Common;
using VPay.DocSys.Generation.Common;

namespace VPay.DocSys.Generation
{
    public class TiffOperations : ITiffOperations
    {
        private readonly IProcessRunner _processRunner;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TiffOperations> _logger;

        public TiffOperations(
            IProcessRunner processRunner,
            IFileSystem fileSystem,
            ILogger<TiffOperations> logger)
        {
            ArgumentHelpers.ThrowIf.IsNull(processRunner, nameof(processRunner));
            ArgumentHelpers.ThrowIf.IsNull(fileSystem, nameof(fileSystem));
            ArgumentHelpers.ThrowIf.IsNull(logger, nameof(logger));

            _processRunner = processRunner;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public async Task<string> CreateTiffFromPdf(string pdfFilePath)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                {"Method", nameof(CreateTiffFromPdf)},
                {"InputFile", pdfFilePath },
            }))
            {
                _logger.LogInformation($"Processing {nameof(TiffOperations)}.{nameof(CreateTiffFromPdf)}");

                var path = _fileSystem.Path.GetDirectoryName(pdfFilePath);
                var tiffFilePath = _fileSystem.Path.ChangeExtension(pdfFilePath, ".tiff");

                //await CreateUsingGsCmd(path, pdfFilePath, tiffFilePath);
                //CreateUsingGsNet(pdfFilePath, tiffFilePath);
                //CreateUsingImageMag(path, tiffFilePath);
                CreateUsingSkia(pdfFilePath, tiffFilePath);

                    _logger.LogInformation("Create {File}", tiffFilePath);
                    return tiffFilePath;
            }
        }


        /// <summary>
        /// Create TIFF file using Ghostscript command line for better quality and control over the conversion process.
        /// (requires gs command line to exist in Linux container)
        /// </summary>
        /// <param name="sourcePdfPath"></param>
        /// <param name="pdfFilePath"></param>
        /// <param name="tiffFilePath"></param>
        /// <returns></returns>
        private async Task CreateUsingGsCmd(string sourcePdfPath, string pdfFilePath, string tiffFilePath)
        {
            var ghostScript = "gs";
            var args = new[]
            {
                    "-q",                           // quiet
                    "-sDEVICE=tiffg3",              // tiff fax format g3
                    "-dNOPAUSE",                    // disable prompt and pause
                    "-dSAFER=true",                 // only allows files in command to be manipulated
                    "-sPAPERSIZE=a4",               // A4 paper size
                    "-dFIXEDMEDIA",                 // center, rotate, and crop/resize to fit 
                    "-dPDFFitPage",                 // used to fix margins for externally generated files (red card pdf)
                    "-dBATCH",                      // quit after command
                    "-r209.10x196",                 // resolution
                    $"-sOutputFile={tiffFilePath}", // output file
                    $"{pdfFilePath}"                // input pdf file
                };

            var cmdDisplay = $"{ghostScript} {string.Join(" ", args)}";
            _logger.LogDebug(cmdDisplay);

            try
            {
                var result = await _processRunner.RunProcessAsync(ghostScript, sourcePdfPath, args);

                if (!string.IsNullOrEmpty(result.Output))
                {
                    _logger.LogDebug("Shell process: {Result}", result);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception when running {Error}", cmdDisplay);
                throw;
            }
        }

        /*
        /// <summary>
        /// Create TIFF file using Ghostscript.NET library for better quality and control over the conversion process.
        /// NOTE: This options works like a charm but requires an expensive annual license for Ghostscript.NET library in production environment.
        /// (does not required gs command line to exist in Linux container when Ghostscript.NativeAssets library is also included)
        /// </summary>
        /// <param name="pdfFilePath"></param>
        /// <param name="tiffFilePath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>Requires the following two NuGet packages:
        ///     <PackageReference Include="Ghostscript.NativeAssets" Version="10.7.1" />
        ///     <PackageReference Include="Ghostscript.NET" Version="1.3.4" />
        /// </remarks>
        public static void CreateUsingGsNet(string pdfFilePath, string tiffFilePath)
        {
            var versionInfo = Ghostscript.NET.GhostscriptVersionInfo.GetLastInstalledVersion();
            if (versionInfo is null)
            {
                throw new InvalidOperationException("No installation was found for Ghostscript.NET.");
            }

            var args = new[]
            {
                    "-q",                           // quiet
                    "-sDEVICE=tiffg3",              // tiff fax format g3
                    "-dNOPAUSE",                    // disable prompt and pause
                    "-dSAFER=true",                 // only allows files in command to be manipulated
                    "-sPAPERSIZE=a4",               // A4 paper size
                    "-dFIXEDMEDIA",                 // center, rotate, and crop/resize to fit 
                    "-dPDFFitPage",                 // used to fix margins for externally generated files (red card pdf)
                    "-dBATCH",                      // quit after command
                    "-r209.10x196",                 // resolution
                    $"-sOutputFile={tiffFilePath}", // output file
                    $"{pdfFilePath}"                // input pdf file
            };

            try
            {
                using var processor = new Ghostscript.NET.Processor.GhostscriptProcessor(versionInfo, true);
                processor.StartProcessing(args, null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert PDF to TIFF using Ghostscript.NET: {ex.Message}", ex);
            }
        }
        */

        /// <summary>
        /// Create TIFF file using ImageMagick library for better quality and control over the conversion process.
        /// NOTE: This method may not work in Linux containers without Ghostscript installed, as ImageMagick relies on Ghostscript for PDF rendering.
        /// (requires gs command line to exist in Linux container)
        /// </summary>
        /// <param name="sourcePdfPath"></param>
        /// <param name="targetTiffPath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>Requires the following NuGet package:
        ///     <PackageReference Include="Magick.NET-Q8-AnyCPU" Version="14.14.0" />
        /// </remarks>
        public static void CreateUsingImageMag(string sourcePdfPath, string targetTiffPath)
        {
            try
            {
                // Define setup properties for crisp rendering quality
                var settings = new MagickReadSettings
                {
                    // 300 DPI ensures text and lines remain sharp
                    Density = new Density(300, 300)
                };

                // Initialize the collection to handle multi-page structures
                using var collection = new MagickImageCollection();

                // Read all pages from the PDF into the collection
                collection.Read(sourcePdfPath, settings);

                // Optional: Force the output format profile explicitly
                foreach (var image in collection)
                {
                    image.Format = MagickFormat.Tiff;
                }

                // Writing the collection directly creates a multi-page TIFF file
                collection.Write(targetTiffPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert PDF to TIFF using ImageMagick: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create TIFF file using SkiaSharp and PDFiumSharp libraries for better quality and control over the conversion process.
        /// NOTE: This method seems to work correctly on Linux containers without Ghostscript installed,
        /// without the Linux font libraries (libfreetype6, libfontconfig1, libharfbuzz0b) installed,
        /// and without the Linux GDI+ library (libgdiplus) installed,
        /// as it uses SkiaSharp for rendering, BitMiracle for TIFF parsing, and PDFiumSharp for PDF parsing.
        /// </summary>
        /// <param name="sourcePdfPath"></param>
        /// <param name="targetTiffPath"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>Requires the following NuGet packages:
        ///     <PackageReference Include="BitMiracle.LibTiff.NET" Version="2.4.660" />
        ///     <PackageReference Include="PDFium.LinuxV2" Version="1.1.4" />
        ///     <PackageReference Include="PDFiumCore" Version="152.0.7961" />
        ///     <PackageReference Include="PDFiumSharp.NativeBinaries" Version="1.4660.0" />
        ///     <PackageReference Include="PDFiumSharpV2" Version="1.1.4" />
        ///     <PackageReference Include="SkiaSharp" Version="4.151.1" />
        ///     <PackageReference Include="SkiaSharp.NativeAssets.Linux.NoDependencies" Version="4.151.1" />
        /// </remarks>
        public static void CreateUsingSkia(string sourcePdfPath, string targetTiffPath)
        {
            try
            {
                PdfToFaxTiffConverter.Convert(sourcePdfPath, targetTiffPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert PDF to TIFF using SkiaSharp: {ex.Message}", ex);
            }
        }

        public static class PdfToFaxTiffConverter
        {
            private const float XDpi = 209.10f;
            private const float YDpi = 196.0f;

            // A4 dimensions in inches
            private const float A4WidthInches = 8.27f;
            private const float A4HeightInches = 11.69f;

            public static void Convert(
                string sourcePdfPath,
                string targetTiffPath)
            {
                int targetWidth =
                    (int)Math.Round(A4WidthInches * XDpi);

                int targetHeight =
                    (int)Math.Round(A4HeightInches * YDpi);

                using var document = new PDFiumSharp.PdfDocument(sourcePdfPath);
                using var tiff = Tiff.Open(targetTiffPath, "w");

                for (int pageIndex = 0;
                     pageIndex < document.Pages.Count;
                     pageIndex++)
                {
                    using SKBitmap pageBitmap =
                        RenderToA4Page(
                            document,
                            pageIndex,
                            targetWidth,
                            targetHeight);

                    byte[] monoData =
                        ConvertTo1Bit(pageBitmap);

                    WriteTiffPage(
                        tiff,
                        monoData,
                        targetWidth,
                        targetHeight);

                    if (pageIndex < document.Pages.Count - 1)
                    {
                        tiff.WriteDirectory();
                    }
                }
            }

            private static SKBitmap RenderToA4Page(
                PDFiumSharp.PdfDocument document,
                int pageIndex,
             int targetWidth,
                int targetHeight)
         {
var page = document.Pages[pageIndex];

    float pageWidthPoints = (float)page.Width;
        float pageHeightPoints = (float)page.Height;

              double sourceWidth =
            pageWidthPoints / 72.0 * XDpi;

     double sourceHeight =
          pageHeightPoints / 72.0 * YDpi;

     double scale =
           Math.Min(
  targetWidth / sourceWidth,
              targetHeight / sourceHeight);

      int renderWidth =
  (int)Math.Round(sourceWidth * scale);

      int renderHeight =
   (int)Math.Round(sourceHeight * scale);

   var rendered = new SKBitmap(renderWidth, renderHeight, SKColorType.Bgra8888, SKAlphaType.Opaque);

  // Create a PDFiumBitmap to render the PDF page into
                using (var pdfiumBitmap = new PDFiumSharp.PDFiumBitmap(renderWidth, renderHeight, false))
         {
          // Render the PDF page into the PDFiumBitmap
   page.Render(pdfiumBitmap, (0, 0, renderWidth, renderHeight));

        // Copy raw BGRA pixel data from PDFiumBitmap into the SKBitmap
       int byteCount = pdfiumBitmap.Stride * renderHeight;
      var srcBytes = new byte[byteCount];
   Marshal.Copy(pdfiumBitmap.Scan0, srcBytes, 0, byteCount);
           srcBytes.CopyTo(rendered.GetPixelSpan());
                }

                var destination =
                    new SKBitmap(
                        targetWidth,
                        targetHeight);

                using (var canvas =
                       new SKCanvas(destination))
                {
                    canvas.Clear(SKColors.White);

                    int x =
                        (targetWidth - renderWidth) / 2;

                    int y =
                        (targetHeight - renderHeight) / 2;

                    var options = new SKSamplingOptions();
                    canvas.DrawBitmap(rendered, x, y, options, null);
                }

                return destination;
            }

            private static byte[] ConvertTo1Bit(
                SKBitmap bitmap)
            {
                int width = bitmap.Width;
                int height = bitmap.Height;

                int stride =
                    (width + 7) / 8;

                byte[] output =
                    new byte[stride * height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        SKColor pixel =
                            bitmap.GetPixel(x, y);

                        int gray =
                            ((pixel.Red * 299) +
                             (pixel.Green * 587) +
                             (pixel.Blue * 114))
                             / 1000;

                        // PHOTOMETRIC.MINISWHITE: bit=0 means white, bit=1 means black.
                        // Set the bit only for dark pixels so they render as black.
                        bool isDark = gray <= 128;

                        if (isDark)
                        {
                            output[(y * stride) + (x / 8)] |= (byte)(0x80 >> (x % 8));
                        }
                    }
                }

                return output;
            }

            private static void WriteTiffPage(
                Tiff tiff,
                byte[] imageData,
                int width,
                int height)
            {
                int stride =
                    (width + 7) / 8;

                tiff.SetField(
                    TiffTag.IMAGEWIDTH,
                    width);

                tiff.SetField(
                    TiffTag.IMAGELENGTH,
                    height);

                tiff.SetField(
                    TiffTag.BITSPERSAMPLE,
                    1);

                tiff.SetField(
                    TiffTag.SAMPLESPERPIXEL,
                    1);

                tiff.SetField(
                    TiffTag.PHOTOMETRIC,
                    Photometric.MINISWHITE);

                tiff.SetField(
                    TiffTag.COMPRESSION,
                    Compression.CCITTFAX3);

                tiff.SetField(
                    TiffTag.FILLORDER,
                    FillOrder.MSB2LSB);

                tiff.SetField(
                    TiffTag.XRESOLUTION,
                    XDpi);

                tiff.SetField(
                    TiffTag.YRESOLUTION,
                    YDpi);

                tiff.SetField(
                    TiffTag.RESOLUTIONUNIT,
                    ResUnit.INCH);

                tiff.SetField(
                    TiffTag.ROWSPERSTRIP,
                    height);

                tiff.SetField(
                    TiffTag.GROUP3OPTIONS,
                    Group3Opt.ENCODING2D);

                byte[] rowBuffer =
                    new byte[stride];

                for (int row = 0;
                     row < height;
                     row++)
                {
                    Buffer.BlockCopy(
                        imageData,
                        row * stride,
                        rowBuffer,
                        0,
                        stride);

                    tiff.WriteScanline(
                        rowBuffer,
                        row);
                }
            }
        }
    }
}

/*
using System;
using System.IO;
using BitMiracle.Docotic.Pdf;

// Requires GDI+ for rendering, so this may not work in Linux containers without additional setup.
// RUN apt-get update && apt-get install -y libgdiplus && rm -rf /var/lib/apt/lists/*

class Program
{
    static void Main(string[] args)
    {
        // Validate arguments
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <input.pdf> <output.tiff>");
            Environment.Exit(1);
        }

        string inputPdf = args[0];
        string outputTiff = args[1];

        if (!File.Exists(inputPdf))
        {
            Console.WriteLine($"Error: File '{inputPdf}' not found.");
            Environment.Exit(1);
        }

        try
        {
            // License setup (optional for trial)
            // PdfDocument.SetLicenseKey("your-license-key");

            using (var pdf = new PdfDocument(inputPdf))
            {
                var saveOptions = new PdfDrawOptions
                {
                    BackgroundColor = PdfDrawOptions.WhiteBackground,
                    Resolution = 300, // DPI
                    Compression = TiffCompression.Ccitt4 // Good for B/W docs
                };

                pdf.SaveAsTiff(outputTiff, saveOptions);
            }

            Console.WriteLine($"TIFF created successfully: {outputTiff}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting PDF to TIFF: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
 
using BitMiracle.LibTiff.Classic;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;

// Requires System.Drawing.Common which is not fully supported in Linux containers without additional setup.

public static class PdfToTiffConverter
{
    /// <summary>
    /// Create TIFF file using BitMiracle.LibTiff.NET library for better quality and control over the conversion process.
    /// </summary>
    /// <param name="args"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <remarks>Requires the following two NuGet packages:
    ///     <PackageReference Include="System.Drawing" Version="8.0.0" />
    ///     <PackageReference Include="BitMiracle.LibTiff.NET" Version="2.4.660" />
    ///     <PackageReference Include="PdfiumViewer" Version="" />
    /// </remarks>

    public static void ConvertPdfToTiff(string pdfPath, string tiffPath, int dpi = 300)
    {
        using var pdfDocument = PdfDocument.Load(pdfPath);

        Tiff output = Tiff.Open(tiffPath, "w");

        if (output == null)
            throw new Exception("Unable to create TIFF file.");

        for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
        {
            var pageSize = pdfDocument.PageSizes[pageIndex];

            int width = (int)(pageSize.Width / 72.0 * dpi);
            int height = (int)(pageSize.Height / 72.0 * dpi);

            using Bitmap bitmap = pdfDocument.Render(
                pageIndex,
                width,
                height,
                dpi,
                dpi,
                PdfRenderFlags.ForPrinting);

            SaveBitmapToTiffPage(output, bitmap, pageIndex);
        }

        output.Close();
    }

    private static void SaveBitmapToTiffPage(
        Tiff tiff,
        Bitmap bitmap,
        int pageNumber)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            tiff.SetField(TiffTag.IMAGEWIDTH, width);
            tiff.SetField(TiffTag.IMAGELENGTH, height);
            tiff.SetField(TiffTag.SAMPLESPERPIXEL, 3);
            tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
            tiff.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
            tiff.SetField(TiffTag.ROWSPERSTRIP, height);
            tiff.SetField(TiffTag.XRESOLUTION, 300.0);
            tiff.SetField(TiffTag.YRESOLUTION, 300.0);
            tiff.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
            tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);

            // LZW compression
            tiff.SetField(TiffTag.COMPRESSION, Compression.LZW);

            int stride = data.Stride;
            byte[] buffer = new byte[stride * height];

            System.Runtime.InteropServices.Marshal.Copy(
                data.Scan0,
                buffer,
                0,
                buffer.Length);

            for (int row = 0; row < height; row++)
            {
                int offset = row * stride;
                tiff.WriteScanline(buffer, offset, row, 0);
            }

            tiff.WriteDirectory();
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
 */
