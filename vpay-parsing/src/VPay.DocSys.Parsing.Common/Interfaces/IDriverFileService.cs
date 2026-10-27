using System;
using System.Collections.Generic;
using System.Text;

namespace VPay.DocSys.Parsing.Common.Interfaces
{
    /// <summary>
    /// Provides common operations associated with red card driver (.drv) files.
    /// </summary>
    public interface IDriverFileService
    {
        /// <summary>
        /// Returns a value indicating whether or the .lock file associated with the specified <paramref name="driverFile"/> exists.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to check.</param>
        /// <returns>
        ///   <c>true</c> if the .lock file exists; otherwise, <c>false</c>.
        /// </returns>
        bool IsLocked(string driverFile);
        
        /// <summary>
        /// Attempts to create the .lock file associated with the specified <paramref name="driverFile"/> if it does not already exists.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to get a lock for.</param>
        /// <returns>
        /// <c>true</c> if the .lock file is successfully created; <c>false</c> if the associated .lock or .log file already exists or if unable to create the .lock file.
        /// </returns>
        /// <remarks>
        /// <see cref="GetLock"/> will return false if the .log file associated with the specified <paramref name="driverFile"/> exists.
        /// </remarks>
        bool GetLock(string driverFile);

        /// <summary>
        /// Attempts to delete the .lock file associated with the specified <paramref name="driverFile"/> if it exists.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to release the lock for.</param>
        /// <returns>
        /// <c>true</c> if the .lock does not exist or is successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        bool ReleaseLock(string driverFile);

        /// <summary>
        /// Attempts to write the specified <paramref name="content"/> to the .log file associated with the specified <paramref name="driverFile" />.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to write the log file for.</param>
        /// <param name="content">The content to be written to the log file.</param>
        /// <returns>
        ///   <c>true</c> if the .log file is successfully written; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// If the .log file already exists it is overwritten with the specified <paramref name="content"/>.
        /// </remarks>
        bool WriteLogFile(string driverFile, string content);

        /// <summary>
        /// Attempts to delete the .log file associated with the specified <paramref name="driverFile"/> if it exists.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to delete the .log file for.</param>
        /// <returns>
        /// <c>true</c> if the .log does not exist or is successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        bool DeleteLogFile(string driverFile);

        /// <summary>
        /// Gets the name of the .lock file associated with the specified <paramref name="driverFile"/>.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to get the .lock file name for.</param>
        /// <returns>
        /// The full path and file name of the .lock file associated with the specified <paramref name="driverFile"/>.
        /// </returns>
        string GetLockFileName(string driverFile);

        /// <summary>
        /// Gets the name of the .log file associated with the specified <paramref name="driverFile"/>.
        /// </summary>
        /// <param name="driverFile">The full path to the driver file to get the .log file name for.</param>
        /// <returns>
        /// The full path and file name of the .log file associated with the specified <paramref name="driverFile"/>.
        /// </returns>
        string GetLogFileName(string driverFile);

    }
}
