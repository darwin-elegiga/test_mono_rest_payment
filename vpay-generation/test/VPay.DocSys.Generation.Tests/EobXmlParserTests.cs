using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VPay.DocSys.Generation.Tests
{
    public class EobXmlParserTests
    {

        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithoutOrderElement_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }            

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithoutDuplexPrintElement_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs></eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }


        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithBlankOrderElement_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <order></order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", null },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithOrderElement_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <order>1</order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", "/home/bogner/eig/C36_modified.pdf" }
                            }
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithMultipleFiles_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <order>1</order>
    <files>
      <file>/home/bogner/eig/C36_modified.pdf</file>
      <file>/home/bogner/eig/C37_modified.pdf</file>
    </files>
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", new List<string>{"/home/bogner/eig/C36_modified.pdf", "/home/bogner/eig/C37_modified.pdf" } }
                            }
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsync_CustomPdfWithBlankFilesElement_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <order>1</order>
    <files />
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }


            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", "1" },
                        {
                            "files", null
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }


        [Fact]
        public async Task EobToDictionaryAsync_WithDecimalValue_ShouldReturnValueAsDecimal()
        {

            string eobXml = @"<eob_xml>
  <address1>123 Main St.</address1>
  <custom_pdfs>
    <method>APPEND</method>
    <duplex_print>Yes</duplex_print>
    <order>1.0</order>
    <files />
  </custom_pdfs>
</eob_xml>";

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }
            
            var expected = new Dictionary<string, dynamic>
            {
                {
                    "address1", "123 Main St."
                },
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "APPEND" },
                        { "duplex_print", "Yes" },
                        { "order", 1m },
                        {
                            "files", null
                        }
                    }
                }
            };

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsyncNew_XmlWithAListOfComplexObjects_ReturnsElement()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>REPLACE</method>
    <duplex_print>No</duplex_print>
    <order>2</order>
    <files>
      <file><value>/home/bogner/eig/C36_modified.pdf</value><eob1></eob1></file>
      <file><value>/home/bogner/eig/C37_modified.pdf</value><eob1></eob1></file>
    </files>
  </custom_pdfs>
</eob_xml>";

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "REPLACE" },
                        { "duplex_print", "No" },
                        { "order", "2" },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                { "file", new List<dynamic>
                                    {
                                        new Dictionary<string, dynamic> { { "value" , "/home/bogner/eig/C36_modified.pdf" }, { "eob1", null } },
                                        new Dictionary<string, dynamic> { { "value" , "/home/bogner/eig/C37_modified.pdf" }, { "eob1", null } },
                                    }
                                }
                            }
                        }
                    }
                }
            };


            var sut = new EobXmlParser();
            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsyncNew_XmlWithSelfClosingTags_ShouldReturnSelfClosingTagElementsAsNull()
        {
            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <method>REPLACE</method>
    <duplex_print>No</duplex_print>
    <order />
    <files>
      <file><value>/home/bogner/eig/C36_modified.pdf</value><eob1 /></file>
      <file><value>/home/bogner/eig/C37_modified.pdf</value><eob1 /></file>
      <selfClosing />
    </files>
  </custom_pdfs>
</eob_xml>";

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "method", "REPLACE" },
                        { "duplex_print", "No" },
                        { "order", null },
                        {
                            "files", new Dictionary<string, dynamic>
                            {
                                {
                                    "file", new List<dynamic>
                                    {
                                        new Dictionary<string, dynamic> { { "value" , "/home/bogner/eig/C36_modified.pdf" }, { "eob1", null } },
                                        new Dictionary<string, dynamic> { { "value" , "/home/bogner/eig/C37_modified.pdf" }, { "eob1", null } },
                                    }
                                },
                                { "selfClosing", null }
                            }
                        }
                    }
                }
            };


            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task EobToDictionaryAsyncNew_WithTrailingZeroDecimals_ShouldKeepDecimalPlaces()
        {

            string eobXml = @"<eob_xml>
  <custom_pdfs>
    <decimal>1.00</decimal>
    <decimal2>1.50</decimal2>
  </custom_pdfs>
</eob_xml>";

            var expected = new Dictionary<string, dynamic>
            {
                {
                    "custom_pdfs", new Dictionary<string, dynamic>
                    {
                        { "decimal", 1.00m },
                        { "decimal2", 1.50m }
                    }
                }
            };


            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            actual.Should().BeEquivalentTo(expected);

            string value = actual["custom_pdfs"]["decimal"].ToString();

            value.Should().BeEquivalentTo("1.00");
        }

        public MemoryStream GenerateStreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }


        [Fact]
        public async Task EobToDictionaryAsync_MultipleEmptyElementsHavingSameNameWithAttributes_ReturnsElement()
        {
            // I manually scrubbed this data.
            #region Big XML
            string eobXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<eob_xml>
    <eob_data>
		<eob>
			<check_number>990003999</check_number>
			<clntno>9999</clntno>
			<clmno>BCCG-9999</clmno>
			<fromDate>03/21/2018</fromDate>
			<thruDate>03/22/2018</thruDate>
		</eob>
	</eob_data>
	<vpay_recipient>
		<line>HEALING HEALTH CENTERS</line>
		<line>1004 EAST THOMAS STREET</line>
		<line>HAMMOND, LA 70401</line>
	</vpay_recipient>
	<vpay_return> </vpay_return>
	<vpay_specific>
		<contact_number>1-800-999-9999</contact_number>
		<contact_name>WellComp</contact_name>
		<tpa_code>YRK</tpa_code>
		<tpa_fullname>WellComp</tpa_fullname>
		<be>YRKPF01065</be>
		<be_fullname>BEVCAP CAPTIVE PROGRAM</be_fullname>
	</vpay_specific>
	<eob>
		<accounts>
			<account accountId=""1256"" clientId=""7040"" title=""YORK RISK SERVICES GROUP, INC., CLIENT ESCROW (D/7040)"" title2=""ACCOUNT ON BEHALF OF ZURICH AMERICAN INSUR. CO -"" title3=""BEVCAP CAPTIVE PROGRAM"">
				<accountReturnAddress addr=""P.O. Box 182364"" city=""Columbus"" state=""OH"" zip=""43218""/>
				<bankBranch bankName=""Wells Fargo Bank, N A"" bankAddress=""190 River Road"" bankCity=""Summit"" bankState=""NJ"" bankZip=""99999-0123"" bankPartialNo=""99-2/999""/>
				<payRequests aggregated=""false"">
					<payRequest id=""49901734"" productType=""Virtual"" payType=""MP"" payCode=""101"" payCodeDesc=""CHIROPRACTIC"" amount=""308.0000"" chkNo=""990003999"" payee1=""HEALING HEALTH CENTERS"" payee2="""" payee3="""" mail1Addr=""1234 EAST THOMAS STREET"" mail2Addr="""" mailCity=""HAMMOND"" mailState=""LA"" mailZip=""70401"" taxId=""222993333"" periodFrom=""03/21/2018"" periodTo=""03/22/2018"" details="""" invoice=""14226366"" refNumber=""BCCG-9999"">
						<claimDetails clmno=""BCCG-9999"" claimId=""9995599"" lossDate=""10/24/2017"" claimant=""Duck, Daffy"" claimantSSN=""434-61-2344"" dob=""12/19/1985"" adjuster=""Andrea Seaton"" adjusterId=""6159"" location=""Champagne Beverage Co."" clntno=""7040""/>
						<EOBHeader PPOName="""" reviewedDate=""04/12/2018"" fromDate=""03/21/2018"" thruDate=""03/22/2018"" amountAllowed=""308.0000"" amountBilled=""324.5000"" batchNo=""412180757"" documentNo=""14226366-M"">
							<EOBDetails billedCode=""99213"" reviewedCode=""99213"" units=""1"" fromDate=""03/21/2018"" thruDate=""03/21/2018"" serviceDescr=""OFC/OUTPT EM ESTAB LOW-MOD 15 MIN"" amtBilled=""76.0000"" amtPaid=""68.0000"" amtPpoReduction=""0.0000"" amtAuditReduction=""8.0000"" amtOtherReduction=""0.0000"" eombCode=""FS001"" eombCode2="""" eombCode3="""" eombCodeDescr=""FEE SCHEDULE REDUCTION - STANDARD"" eombCodeDescr2="""" eombCodeDescr3=""""/>
							<EOBDetails billedCode=""97110"" reviewedCode=""97110"" units=""2"" fromDate=""03/21/2018"" thruDate=""03/21/2018"" serviceDescr=""THERAP 1/&gt; AREAS/15 MIN; EXERCISES"" amtBilled=""90.0000"" amtPaid=""86.0000"" amtPpoReduction=""0.0000"" amtAuditReduction=""4.0000"" amtOtherReduction=""0.0000"" eombCode=""FS001"" eombCode2="""" eombCode3="""" eombCodeDescr=""FEE SCHEDULE REDUCTION - STANDARD"" eombCodeDescr2="""" eombCodeDescr3=""""/>
							<EOBDetails billedCode=""98941"" reviewedCode=""98941"" units=""1"" fromDate=""03/22/2018"" thruDate=""03/22/2018"" serviceDescr=""CHIROPRACTIC MANIP TX; SP 3-4 RGNS"" amtBilled=""68.5000"" amtPaid=""68.0000"" amtPpoReduction=""0.0000"" amtAuditReduction=""0.5000"" amtOtherReduction=""0.0000"" eombCode=""FS001"" eombCode2="""" eombCode3="""" eombCodeDescr=""FEE SCHEDULE REDUCTION - STANDARD"" eombCodeDescr2="""" eombCodeDescr3=""""/>
							<EOBDetails billedCode=""97110"" reviewedCode=""97110"" units=""2"" fromDate=""03/22/2018"" thruDate=""03/22/2018"" serviceDescr=""THERAP 1/&gt; AREAS/15 MIN; EXERCISES"" amtBilled=""90.0000"" amtPaid=""86.0000"" amtPpoReduction=""0.0000"" amtAuditReduction=""4.0000"" amtOtherReduction=""0.0000"" eombCode=""FS001"" eombCode2="""" eombCode3="""" eombCodeDescr=""FEE SCHEDULE REDUCTION - STANDARD"" eombCodeDescr2="""" eombCodeDescr3=""""/>
							<diagnoses>
								<diagCode1 code=""S335XXA"" desc=""Sprain of ligaments of lumbar spine, initial encounter""/>
								<diagCode2 code=""M5416"" desc=""Radiculopathy, lumbar region""/>
								<diagCode3 code=""S233XXA"" desc=""Sprain of ligaments of thoracic spine, initial encounter""/>
								<diagCode4 code=""M546"" desc=""Pain in thoracic spine""/>
								<diagCode5 code=""M62830"" desc=""Muscle spasm of back""/>
							</diagnoses>
							<contact>WellComp Managed Care Services * 800/932-5535 * PO Box 619079, Roseville, CA * 95661</contact>
							<disclaimer>Charges reduced in accordance with base allowances per the applicable fee schedule. Additional reductions per state guidelines or other circumstances (see reduction notes). This bill has been reviewed according to the Louisiana Workers' Compensation Fee Schedule Title 40 Chapter 51 Section 5101. Fees for covered services in excess of the amounts allowable under the terms of this schedule are not recoverable from the employer, insurer, or employee.</disclaimer>
						</EOBHeader>
					</payRequest>
				</payRequests>
			</account>
		</accounts>
	</eob>
	<totals>
		<charge_total>324.5000</charge_total>
		<payment_total>308.0000</payment_total>
	</totals>
</eob_xml>";
            #endregion

            var sut = new EobXmlParser();

            IDictionary<string, dynamic> actual;
            using (var stream = GenerateStreamFromString(eobXml))
            {
                actual = await sut.EobToDictionaryAsync(stream);
            }

            var eobDetails =
                ((Dictionary<string, dynamic>)
                    ((Dictionary<string, dynamic>)
                        ((Dictionary<string, dynamic>)
                            ((Dictionary<string, dynamic>)
                                ((Dictionary<string, dynamic>)
                                    ((Dictionary<string, dynamic>) actual["eob"])
                                    ["accounts"])
                                ["account"])
                            ["payRequests"])
                        ["payRequest"])
                    ["EOBHeader"])["EOBDetails"];

            Assert.NotNull(eobDetails);
            Assert.True(eobDetails.Count == 4);


        }




    }
}
