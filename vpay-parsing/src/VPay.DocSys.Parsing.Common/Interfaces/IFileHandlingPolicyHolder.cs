using Polly;

namespace VPay.DocSys.Parsing.Common
{
    public interface IFileHandlingPolicyHolder
    {
        /// <summary>
        /// Gets the policy that should be used for delete directory operations.
        /// </summary>
        ISyncPolicy RemoveDirectoryPolicy { get; }


        /// <summary>
        /// Gets the policy that does not implement retries.
        /// </summary>
        ISyncPolicy NoOpPolicy { get; }

        /// <summary>
        /// Gets the policy to use for Delete/Remove Directory operations.
        /// </summary>
        ISyncPolicy DeletePolicy { get; }
    }
}
