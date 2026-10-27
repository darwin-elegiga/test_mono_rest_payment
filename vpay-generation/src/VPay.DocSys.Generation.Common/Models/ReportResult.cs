using System;
using System.Text;

namespace VPay.DocSys.Generation.Common
{
    public class ReportResult
    {
        public long ReportResultId { get; set; }

        public long? ReportRequestId { get; set; }

        public string Tpa { get; set; }

        public string StageFileKey { get; set; }

        public long? TransactionId { get; set; }

        public short ComparisonResultId { get; set; }

        public string ComparisonResult { get; set; }

        public bool IsSame { get; set; }

        public long? LeftArchiveFileId { get; set; }

        public string LeftSystemName { get; set; }

        public string LeftZipPath { get; set; }

        public string LeftEntryName { get; set; }

        public string LeftEntryDescription { get; set; }

        public long? RightArchiveFileId { get; set; }

        public string RightSystemName { get; set; }

        public string RightZipPath { get; set; }

        public string RightEntryName { get; set; }

        public string RightEntryDescription { get; set; }

        public DateTime ComparisonDateTime { get; set; }

        public int? TestDurationMs { get; set; }

        public string ExceptionMessage { get; set; }

        public string ToCsv()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( $"{ReportResultId}," );
            sb.Append( $"{ReportRequestId}," );
            sb.Append( $"{Tpa}," );
            sb.Append( $"{WrapForExcel( StageFileKey )}," );
            sb.Append( $"{WrapForExcel(TransactionId)}," );
            sb.Append( $"{ComparisonResultId}," );
            sb.Append( $"{ComparisonResult}," );
            sb.Append( $"{(IsSame?1:0)}," );
            sb.Append( $"{LeftArchiveFileId}," );
            sb.Append( $"{LeftSystemName}," );
            sb.Append( $"{LeftZipPath}," );
            sb.Append( $"{LeftEntryName}," );
            sb.Append( $"{LeftEntryDescription}," );
            sb.Append( $"{RightArchiveFileId}," );
            sb.Append( $"{RightSystemName}," );
            sb.Append( $"{RightZipPath}," );
            sb.Append( $"{RightEntryName}," );
            sb.Append( $"{RightEntryDescription}," );
            sb.Append( $"{ComparisonDateTime:yyyy-MM-dd HH:mm:ss}," );
            sb.Append( $"{TestDurationMs}," );
            sb.Append( $"{ExceptionMessage}" );
            return sb.ToString();

        }

        private static string WrapForExcel(object o)
        {
            return string.Format( "\"=\"\"{0}\"\"\"", o);

        }

        public static string CsvHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "ReportResultId," );
            sb.Append( "ReportRequestId," );
            sb.Append( "Tpa," );
            sb.Append( "StageFileKey," );
            sb.Append( "TransactionId," );
            sb.Append( "ComparisonResultId," );
            sb.Append( "ComparisonResult," );
            sb.Append( "IsSame," );
            sb.Append( "LeftArchiveFileId," );
            sb.Append( "LeftSystemName," );
            sb.Append( "LeftZipPath," );
            sb.Append( "LeftEntryName," );
            sb.Append( "LeftEntryDescription," );
            sb.Append( "RightArchiveFileId," );
            sb.Append( "RightSystemName," );
            sb.Append( "RightZipPath," );
            sb.Append( "RightEntryName," );
            sb.Append( "RightEntryDescription," );
            sb.Append( "ComparisonDateTime," );
            sb.Append( "TestDurationMs," );
            sb.Append( "ExceptionMessage" );
            return sb.ToString();

        }


    }
}
 
