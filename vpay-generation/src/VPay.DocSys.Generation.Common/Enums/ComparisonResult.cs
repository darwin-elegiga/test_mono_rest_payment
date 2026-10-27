namespace VPay.DocSys.Generation.Common
{
	/// <summary>
	/// Results of Comparing two documents
	/// </summary>
	public enum ComparisonResult
	{
		/// <summary>
		/// Has not been run
		/// </summary>
		NotRun = 0,

		/// <summary>
		/// The document on the left does not exist in the database
		/// </summary>
		Detail1IsNull = 1,
		/// <summary>
		/// The document on the right does not exist in the database
		/// </summary>
		Detail2IsNull = 2,
		/// <summary>
		/// The document on the left does not exist in the archive
		/// </summary>
		Detail1DataIsNull = 3,
		/// <summary>
		/// The document on the right does not exist in the archive
		/// </summary>
		Detail2DataIsNull = 4,
		
		/// <summary>
		/// Both documents have been evaluated and are ready to be sent to the API for comparison
		/// </summary>
		ReadyToCompare = 10,
		/// <summary>
		/// http API call failed
		/// </summary>
		HttpCallFailed = 20,

		/// <summary>
		/// Winner, winner, chicken dinner!
		/// </summary>
		DetailsAreSame = 100,

		/// <summary>
		/// They have been weighed, measured and found wanting.
		/// </summary>
		DetailsAreDifferent = 101,

		/// <summary>
		/// Something bad happened.
		/// </summary>
		ErrorCondition = 999
	}
}