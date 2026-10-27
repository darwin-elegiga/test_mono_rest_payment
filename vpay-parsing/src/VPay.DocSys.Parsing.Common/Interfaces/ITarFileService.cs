using System;
using System.Collections.Generic;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    public interface ITarFileService
    {
        List<Guid> CreateTarFile(string tarFilePath, List<TarFileRecord> tarRecords);

        /// <summary>
        /// Gets a list of all tar files records from the given tar file
        /// </summary>
        /// <param name="tarFilePath">file path of the tar</param>
        /// <returns>list of <see cref="TarFileRecord"/></returns>
        List<TarFileRecord> GetAllFiles(string tarFilePath);

        /// <summary>
        /// Enumerate through the tar file and yield a <see cref="TarFileRecord" />
        /// </summary>
        /// <param name="tarFilePath"></param>
        /// <returns>enumerable of <see cref="TarFileRecord"/></returns>
        IEnumerable<TarFileRecord> EnumerateAllFiles(string tarFilePath);
    }
}
