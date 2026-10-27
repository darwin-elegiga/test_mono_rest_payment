using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Xunit;

namespace VPay.DocSys.Parsing.Common.Tests
{
    public class TarArchiveReaderTests
    {
        [Fact]
        public void CanProcessTarFilesCorrectly()
        {
            var randomData1024 = RandomString(1024);
            var randomData5MB = RandomString(1024 * 1024 * 5);
            var randomData10MB = RandomString(1024 * 1024 * 10);
            var longFileName = RandomString(200);

            var tarFileName = Path.ChangeExtension(Path.GetTempFileName(), "tar");
            var innerTarFileName = Path.ChangeExtension(Path.GetTempFileName(), "tar");
            try
            {
                using (var innerTarStream = File.Create(innerTarFileName))
                {
                    using (var tarOutputStream = new TarOutputStream(innerTarStream, Encoding.UTF8))
                    {
                        AddFileToTar(tarOutputStream, "test.xml", randomData5MB);
                        AddFileToTar(tarOutputStream, longFileName, randomData10MB);
                        tarOutputStream.CloseEntry();
                    }
                }

                using (var innerTarStream = File.Open(innerTarFileName, FileMode.Open))
                {
                    using (var outStream = File.Create(tarFileName))
                    {
                        using (var tarOutputStream = new TarOutputStream(outStream, Encoding.UTF8))
                        {
                            AddFileToTar(tarOutputStream,
                                "FS7U200573_processed/eob/20200730.FS7U200573.0ad2cc65-d9b1-49d3-b39b-58724ad09d8f.eob.xml",
                                innerTarStream);
                            AddFileToTar(tarOutputStream, "test.xml", randomData5MB);
                            AddFileToTar(tarOutputStream, longFileName, randomData10MB);
                            tarOutputStream.CloseEntry();
                        }
                    }
                }

                using var mmTarFile = MemoryMappedFile.CreateFromFile(tarFileName, FileMode.Open);
                var tarArchive = new TarArchiveReader(mmTarFile, tarFileName, new FileInfo(tarFileName).Length);
                Assert.Equal(3, tarArchive.Files.Count);
                Assert.Equal(randomData5MB, tarArchive.Files[1].GetTextValue());
                Assert.Equal(randomData10MB, tarArchive.Files[2].GetTextValue());
                Assert.Equal(longFileName, tarArchive.Files[2].Name);

                // Inner File
                using var mmInnerTarFile = tarArchive.Files[0].GetViewStream();
                var innerTarFile = new TarArchiveReader(mmTarFile, mmInnerTarFile, "inner", tarArchive.Files[0].FilePos);
                Assert.Equal(2, innerTarFile.Files.Count);
                Assert.Equal(randomData5MB, innerTarFile.Files[0].GetTextValue());
                Assert.Equal(randomData10MB, innerTarFile.Files[1].GetTextValue());
            }
            finally
            {
                if (File.Exists(tarFileName))
                    File.Delete(tarFileName);
                if (File.Exists(innerTarFileName))
                    File.Delete(innerTarFileName);
            }
        }

        private static void AddFileToTar(TarOutputStream tarOutputStream, string fileName, string fileData)
        {
            var tarEntry = TarEntry.CreateTarEntry(fileName);
            tarEntry.Size = fileData.Length;
            tarOutputStream.PutNextEntry(tarEntry);
            tarOutputStream.Write(Encoding.ASCII.GetBytes(fileData), 0, fileData.Length);
        }

        private static void AddFileToTar(TarOutputStream tarOutputStream, string fileName, FileStream fileData)
        {
            var tarEntry = TarEntry.CreateTarEntry(fileName);
            fileData.Position = 0;
            tarEntry.Size = fileData.Length;
            tarOutputStream.PutNextEntry(tarEntry);
            fileData.CopyTo(tarOutputStream);
        }

        private static string RandomString(int length)
        {
            var strBuilder = new StringBuilder();
            var random = new Random();
            for (var i = 0; i < length; i++)
            {
                // Generate floating point numbers
                var myFloat = random.NextDouble();
                // Generate the char
                var myChar = Convert.ToChar(Convert.ToInt32(Math.Floor(25 * myFloat) + 65));
                strBuilder.Append(myChar);
            }
            return strBuilder.ToString();
        }
    }
}
