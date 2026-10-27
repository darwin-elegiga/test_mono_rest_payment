using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO.Abstractions;
using VPay.DocSys.Common;

using Xunit;
using VPay.DocSys.Generation.Common.Edi835;
using System.Text;
using VPay.DocSys.Generation.Edi835;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class Edi835FileCompareTests
    {
        private readonly IFileSystem _fileSystem = new FileSystem();

        private CompareFiles SUT;
        public Edi835FileCompareTests()
        {
            SUT = new CompareFiles(_fileSystem);
            SetUpMatchingFiles();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private string _filePrefix = "270810641_IC_SWK_SWKPF01271_K0000040540MEMAX09_805224793_NEW_";
        private string _timestampA = "20200602165259";
        private string _timestampB = "20210122113459";

        private string _docsContent = @"Line1
line 2
LINE three";
        private string _trnContent = @"TRN*1*999999*999999999~                                                        ";
        private string _payfileContent = @"999999999|ABC";
        private string _pdfContent = @"Fake PDF";


        private string _swkEdi835CreationMessage = @"
{
'statusFilePath': '/SRVFS/bogner/transfer/IC/SWK/2021/03/25/B002460531_FS3P212692.sts',
'inputFile': 'FS3P212692',
'rawFilePath': 'swk_rawfile_scrubbed.txt',
'originCode': 'IC',
'clientCode': 'SWK',
'billingEntityCode': 'SWKPF01001',
'taxIdentificationNumber': '411861374',
'checkStatus': '',
'transactionId': 998664432,
'nonFundedPaymentDetailId': 195040448,
'userKey': 'M0000012920LBOADB01',
'paymentTypeCode': 'CLCCK',
'claimId': '4095187498',
'claimDate': '20210326',
'payeeName': 'INFINITE HEALTH COLL',
'amount': 119.21,
'loadDate': '20210325',
'timeStamp': '2021-04-01T19:51:27.1402161+00:00',
'fetchPdfInNonProd': false
}";



        private void SetUpMatchingFiles()
        {
            SetUpFile($"{_filePrefix}{_timestampA}.docs", _docsContent);
            SetUpFile($"{_filePrefix}{_timestampB}.docs", _docsContent);

            SetUpFile($"{_filePrefix}{_timestampA}.trn", _trnContent);
            SetUpFile($"{_filePrefix}{_timestampB}.trn", _trnContent);

            SetUpFile($"{_filePrefix}{_timestampA}.payfile", _payfileContent);
            SetUpFile($"{_filePrefix}{_timestampB}.payfile", _payfileContent);

            SetUpFile($"{_filePrefix}{_timestampA}.pdf", _pdfContent);
            SetUpFile($"{_filePrefix}{_timestampB}.pdf", _pdfContent);




        }

        private void SetUpFile(string filename, string content)
        {
            using (var wellActuallyNotAVirtualFile = _fileSystem.File.CreateText(filename))
            {
                wellActuallyNotAVirtualFile.Write(content);
            }
        }


        [Fact]
        public void CompareFiles_null_FileA_should_always_compare_false()
        {
            string fileA = null;
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }
        [Fact]
        public void CompareFiles_null_FileB_should_always_compare_false()
        {
            string fileA = $"{_filePrefix}{_timestampA}.docs";
            string fileB = null;
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }

        [Fact]
        public void CompareFiles_empty_FileA_should_always_compare_false()
        {
            var fileA = string.Empty;
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }
        [Fact]
        public void CompareFiles_empty_FileB_should_always_compare_false()
        {
            string fileA = $"{_filePrefix}{_timestampA}.docs";
            var fileB = string.Empty;
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }


        [Theory]
        [InlineData(".docs")]
        [InlineData(".trn")]
        [InlineData(".payfile")]
        [InlineData(".pdf")]
        public void CompareFiles_both_files_should_exist(string fileExtension)
        {
            var fileA = $"{_filePrefix}{_timestampA}{fileExtension}";
            var fileB = $"{_filePrefix}{_timestampB}{fileExtension}";
            var result = SUT.Compare(fileA, fileB);
            Assert.True(result.NoDifferencesFound);
            Assert.True(result.Differences.Count == 0);

        }

        [Theory]
        [InlineData(".docs")]
        [InlineData(".trn")]
        [InlineData(".payfile")]
        [InlineData(".pdf")]
        public void CompareFiles_both_files_should_exist_negative_test(string fileExtension)
        {
            var fileA1 = $"{_filePrefix}{_timestampA}{fileExtension}";
            var fileB1 = $"{_filePrefix}{_timestampB}_WONT_EXIST_{fileExtension}";
            var result1 = SUT.Compare(fileA1, fileB1);
            Assert.False(result1.NoDifferencesFound);
            Assert.True(result1.Differences.Count > 1);
            Assert.Contains("FileA exists.", result1.Differences);
            Assert.Contains("FileB does not exist.", result1.Differences);

            var fileA2 = $"{_filePrefix}{_timestampA}_WONT_EXIST_{fileExtension}";
            var fileB2 = $"{_filePrefix}{_timestampB}{fileExtension}";
            var result2 = SUT.Compare(fileA2, fileB2);
            Assert.False(result2.NoDifferencesFound);
            Assert.True(result2.Differences.Count > 1);
            Assert.Contains("FileA does not exist.", result2.Differences);
            Assert.Contains("FileB exists.", result2.Differences);

            var fileA3 = $"{_filePrefix}{_timestampA}_WONT_EXIST_{fileExtension}";
            var fileB3 = $"{_filePrefix}{_timestampB}_WONT_EXIST_{fileExtension}";
            var result3 = SUT.Compare(fileA3, fileB3);
            Assert.False(result3.NoDifferencesFound);
            Assert.True(result3.Differences.Count > 1);
            Assert.Contains("FileA does not exist.", result3.Differences);
            Assert.Contains("FileB does not exist.", result3.Differences);

        }

        [Theory]
        [InlineData(".docs", ".docs", true)]
        [InlineData(".trn", ".trn", true)]
        [InlineData(".payfile", ".payfile", true)]
        [InlineData(".pdf", ".pdf", true)]

        [InlineData(".docs", ".DOCS", false)]
        [InlineData(".trn", ".TRN", false)]
        [InlineData(".payfile", ".PAYFILE", false)]
        [InlineData(".pdf", ".PDF", false)]

        [InlineData(".docs", ".x", false)]
        [InlineData(".trn", ".x", false)]
        [InlineData(".payfile", ".x", false)]
        [InlineData(".pdf", ".x", false)]

        public void CompareFiles_extensions_should_match(string extA, string extB, bool shouldMatch)
        {

            var fileA = $"{_filePrefix}{_timestampA}{extA}";
            var fileB = $"{_filePrefix}{_timestampB}{extB}";
            var result = SUT.Compare(fileA, fileB);
            Assert.Equal(shouldMatch, result.NoDifferencesFound);
        }

        [Fact]
        public void CompareFiles_prefix_should_match()
        {
            var fileA = $"{_filePrefix}{_timestampA}.docs";
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.True(result.NoDifferencesFound);
        }

        [Fact]
        public void CompareFiles_prefix_should_match_negative_test()
        {
            var fileA = $"NoUnderscoreEither.docs";
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }

        [Fact]
        public void CompareFiles_length_should_match()
        {

            var fileA = $"NoUnderscoreEither.docs";
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);
        }

        [Fact]
        public void CompareFiles_length_should_match_negative_test()
        {
            SetUpFile($"{_filePrefix}_X_{_timestampA}.docs", _docsContent);
            SetUpFile($"{_filePrefix}_X_{_timestampB}.docs", _docsContent + ".");

            var fileA = $"{_filePrefix}_X_{_timestampA}.docs";
            var fileB = $"{_filePrefix}_X_{_timestampB}.docs";

            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);

            Assert.Contains(result.Differences, _ => _.StartsWith("File lengths do not match."));
        }

        [Fact]
        public void CompareFiles_content_should_match()
        {

            var fileA = $"{_filePrefix}{_timestampA}.docs";
            var fileB = $"{_filePrefix}{_timestampB}.docs";
            var result = SUT.Compare(fileA, fileB);
            Assert.True(result.NoDifferencesFound);

        }

        [Fact]
        public void CompareFiles_content_should_match_negative_test()
        {


            SetUpFile($"{_filePrefix}_X_{_timestampA}.docs", _docsContent + " ");
            SetUpFile($"{_filePrefix}_X_{_timestampB}.docs", _docsContent + ".");

            var fileA = $"{_filePrefix}_X_{_timestampA}.docs";
            var fileB = $"{_filePrefix}_X_{_timestampB}.docs";

            var result = SUT.Compare(fileA, fileB);
            Assert.False(result.NoDifferencesFound);

            Assert.Contains(result.Differences, _ => _.StartsWith("Files do not match at position"));

        }


        [Fact]
        public void Edi835File_Should_Accept_Unicode_Content()
        {
            var docsFile = new DocsFile();
            docsFile.Content = "Bank of America Workers’ Compensation";

            string filename = _fileSystem.Path.GetTempFileName();
            using (var fs = _fileSystem.File.Create(filename))
            {
                docsFile.ContentAsStream().CopyTo(fs);
            }

            var writtenContent = _fileSystem.File.ReadAllText(filename);

            Assert.Equal(docsFile.Content, writtenContent);

            if (_fileSystem.File.Exists(filename))
            {
                _fileSystem.File.Delete(filename);
            }
        }

        [Fact]
        public async Task Extracted_swk_docs_file_should_exactly_match_file_on_disk()
        {
            string expectedFilename = "swk_expected_docs.docs";

            string actualFilename = _fileSystem.Path.GetTempFileName();
            var parser = new DocsParserSwk(_fileSystem, new Common.Edi835.Edi835ServiceSettings());
            var docsFile = new DocsFile();
            var creationMessage = JsonConvert.DeserializeObject<Edi835CreationMessage>(_swkEdi835CreationMessage);
            docsFile.Content = (await parser.CreateContent(creationMessage)).Replace("\r", "");

            using (var fs = _fileSystem.File.Create(actualFilename))
            {
                docsFile.ContentAsStream().CopyTo(fs);
            }
            var expectedContent = _fileSystem.File.ReadAllText(expectedFilename, Encoding.GetEncoding(1252)).Replace("\r","");
 
            Assert.Equal(expectedContent, docsFile.Content);

            if (_fileSystem.File.Exists(actualFilename))
            {
                _fileSystem.File.Delete(actualFilename);
            }
        }

    }
}
