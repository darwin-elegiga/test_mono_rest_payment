using System;

namespace VPay.DocSys.Parsing.Common
{
	/// <summary>
	/// Indicates the type of file received from an EDI vendor.
	/// </summary>
	public enum Inbound835FileType
	{
		/// <summary>
		/// The file type is unknown
		/// </summary>
		Unknown,

		/// <summary>
		/// The 835 (Remittance Advice) file from the vendor (*.835)
		/// </summary>
		RemittanceAdvice,

		/// <summary>
		/// The BRN (Blue Remittance Notification) file from the vendor (*.brn)
		/// </summary>
		BlueRemittanceNotification,

		/// <summary>
		/// File from vendor that acknowledges the receipt of 835 request (*.ack, *.txt)
		/// </summary>
		Acknowledgment,

		/// <summary>
		/// Vendor response file indicating payment has been posted to vendor portal (*.835.txt)
		/// </summary>
		PaymentPosted,
	}
}
