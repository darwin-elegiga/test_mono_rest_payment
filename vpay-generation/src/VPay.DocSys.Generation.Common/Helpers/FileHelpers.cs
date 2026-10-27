using System.IO.Abstractions;
using static VPay.DocSys.Common.ArgumentHelpers;

namespace VPay.DocSys.Generation.Common.Helpers
{
    public static class FileHelpers
    {
        /// <summary>
        /// Uses the underlying <code>WriteAllText</code> method in <see cref="FileSystem.File"/>
        /// </summary>
        /// <param name="fileSystem"><see cref="IFileSystem"/> instance</param>
        /// <param name="path">path string</param>
        /// <param name="fileName">file name string</param>
        /// <param name="text">file content string</param>
        /// <returns>newly created file name and path</returns>
        public static string CreateTextFile(this IFileSystem fileSystem, string path, string fileName, string text = "")
        {
            ThrowIf.IsNullOrEmpty(path, nameof(path));
            ThrowIf.IsNullOrEmpty(fileName, nameof(fileName));

            var fullFileName = fileSystem.Path.Combine(path, fileName);

            fileSystem.File.WriteAllText(fullFileName, text);

            return fullFileName;
        }
    }
}
