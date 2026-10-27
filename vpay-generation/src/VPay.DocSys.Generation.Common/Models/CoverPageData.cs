using System.Collections.Generic;
using VPay.DocSys.Common;

namespace VPay.DocSys.Generation.Common
{
    public class CoverPageData
    {

        public OtbRecord OTB_ENTRY { get; set; }

        public string BILLING_ENTITY { get; set; }

        public string DIST { get; set; }

        public string PAYTYPE { get; set; }

        public string DESC { get; set; }

        public string ERR_CODE { get; set; }

        public string ERR_MSG { get; set; }

        public string FIRST_PMT { get; set; }

        public string se_phone_number { get; set; }

        public string return_line_1 { get; set; } = "";
        public string return_line_2 { get; set; } = "";
        public string return_line_3 { get; set; } = "";

        public string mail_to_line_1 { get; set; } = "";
        public string mail_to_line_2 { get; set; } = "";
        public string mail_to_line_3 { get; set; } = "";
        public string mail_to_line_4 { get; set; } = "";
        public string mail_to_line_5 { get; set; } = "";


        public string contact_name { get; set; }
        public string contact_number { get; set; }
        public string tpa_full_name { get; set; }
        public string tpa_name { get; set; }
        public string setxid { get; set; }
        public string claim_id { get; set; }

        public string amount { get; set; }


        public string claim_date { get; set; }
        public string load_date { get; set; }
        public string cover_date { get; set; }

        public string clear_pan { get; set; }
        public string masked_pan { get; set; }
        public string cvc { get; set; }

        public string cc_date { get; set; }
        public string expire_date { get; set; }

        public string pdf_id { get; set; }

        public IDictionary<string, dynamic> eob_xml_dict { get; set; }
        public IDictionary<string, dynamic> ftp_dict { get; set; }

        public string ss_flag { get; set; }
        public string ss_preauth { get; set; }
        public string ss_sid { get; set; }
        public string ss_mid { get; set; }

        public IDictionary<string, dynamic> one_check_dict { get; set; }

        // from Red Card 
        public string TPA_Name { get; set; }
        public string VPay_Phone { get; set; }
        public string be_name { get; set; }
        public string check_date { get; set; }
        public string payment_id { get; set; }
        public string check_num_original { get; set; }
        public string payee_type { get; set; }
    }
}
