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

using DS = VPay.DocSys.Data;
using System.IO;

namespace VPay.DocSys.Generation.Common.Tests
{
    public class Edi835ExtensionTest
    {
        [Fact]
        public void TempFilename_should_return()
        {
            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345
            };
            var expectedTempFilename = "0123456789_SEMED_HEQTN_HEQ12345_ABC123_12345_DD_0000000000";
            var actualTempFilename = edi.GetTempFilename();
            Assert.Equal(expectedTempFilename, actualTempFilename);

        }

        [Fact]
        public void TempFilename_should_return_with_extension()
        {
            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345
            };
            var expectedTempFilename = "0123456789_SEMED_HEQTN_HEQ12345_ABC123_12345_DD_0000000000.docs";
            var actualTempFilename = edi.GetTempFilename("docs");
            Assert.Equal(expectedTempFilename, actualTempFilename);

        }

        [Fact]
        public void TempFilename_should_return_with_extension_2()
        {
            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345
            };
            var expectedTempFilename = "0123456789_SEMED_HEQTN_HEQ12345_ABC123_12345_DD_0000000000.docs";
            var actualTempFilename = edi.GetTempFilename(".docs");
            Assert.Equal(expectedTempFilename, actualTempFilename);

        }

        [Fact]
        public void GetExpectedDocsFolder_should_throw_error_on_blank_PaymentStatusFile()
        {
            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345
            };
            try
            {
                var actualFolder = edi.GetExpectedDocsFolder("blah");
                Assert.False(true, "Getting to this point is a failed test");
            }
            catch (Exception argEx)
            {
                Assert.IsType<ArgumentException>(argEx);
                Assert.Equal("PaymentStatusFile must exist and contain '_'", argEx.Message);
            }
            

        }

        [Fact]
        public void GetExpectedDocsFolder_should_throw_error_on_invalid_PaymentStatusFile()
        {
            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345,
                PaymentStatusFile = "_"
            };
            try
            {
                var actualFolder = edi.GetExpectedDocsFolder("blah");
                Assert.False(true, "Getting to this point is a failed test");
            }
            catch (Exception argEx)
            {
                Assert.IsType<ArgumentException>(argEx);
                Assert.Equal("PaymentStatusFile must have iSeries filename component.", argEx.Message);
            }
        }


        [Fact]
        public void GetExpectedDocsFolder_should_return_expected_folder()
        {
            var rootFolder = Path.Combine("SRVFS", "bogner", "Edi835", "docs");


            var edi = new DS.Edi835
            {
                TaxId = "0123456789",
                OriginCode = "SEMED",
                ClientCode = "HEQTN",
                BillingEntityCode = "HEQ12345",
                UserKey = "ABC123",
                TransactionId = 12345,
                PaymentStatusFile =
                                "/SRVFS/bogner/transfer/SEMED/HEQTN/2021/07/13/B12345678_FS87654321.sts".Replace('/', Path.DirectorySeparatorChar)
            };
            try
            {
                var expectedFolder = Path.Combine(rootFolder, "HEQTN", "HEQ12345", "FS87654321");
                var actualFolder = edi.GetExpectedDocsFolder(rootFolder);
                Assert.Equal(expectedFolder, actualFolder);
            }
            catch (Exception argEx)
            {
                Assert.IsType<ArgumentException>(argEx);
                Assert.Equal("PaymentStatusFile must have iSeries filename component.", argEx.Message);
            }

        }




    }
}
