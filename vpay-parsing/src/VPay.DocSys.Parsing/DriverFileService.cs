using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;
using VPay.DocSys.Storage;
using VPay.Queuing;

namespace VPay.DocSys.Parsing
{
    /// <summary>
    /// Provides common operations associated with red card driver (.drv) files.
    /// </summary>
    public class DriverFileService : IDriverFileService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<DriverFileService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DriverFileService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        public DriverFileService(IFileSystem fileSystem, ILogger<DriverFileService> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        /// Returns a value indicating whether or the .lock file associated with the specified <paramref name="driverFile" /> exists.
        /// </summary>
        /// <param name="driverFile">The driver file to check.</param>
        /// <returns>
        /// <c>true</c> if the .lock file exists; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="FileNotFoundException"></exception>
        public bool IsLocked(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            // get the lock file name
            var lockFileName = this.GetLockFileName(driverFile);

            // the driver file is locked if the .lock file exists
            return _fileSystem.File.Exists(lockFileName);
        }

        /// <summary>
        /// Attempts to create the .lock file associated with the specified <paramref name="driverFile" /> if it does not already exists.
        /// </summary>
        /// <param name="driverFile">The driver file to get a lock for.</param>
        /// <returns>
        /// <c>true</c> if the .lock file is successfully created; <c>false</c> if the associated .lock or .log file already exists or if unable to create the .lock file.
        /// </returns>
        public bool GetLock(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            var lockFileName = this.GetLockFileName(driverFile);
            var logFileName = this.GetLogFileName(driverFile);

            // if either the lock file or the log file exists, return false
            if(_fileSystem.File.Exists(lockFileName) || _fileSystem.File.Exists(logFileName))
            {
                return false;
            }

            try
            {
                // attempt to create the lock file - FileMode.CreateNew will throw exception if the file already exists
                using(_fileSystem.File.Open(lockFileName, FileMode.CreateNew, FileAccess.Write))
                {
                    return true;
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "An error occurred while trying to create the .lock file.");
            }

            return false;
        }

        /// <summary>
        /// Attempts to delete the .lock file associated with the specified <paramref name="driverFile" /> if it exists.
        /// </summary>
        /// <param name="driverFile">The driver file to release the lock for.</param>
        /// <returns>
        /// <c>true</c> if the .lock does not exist or is successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public bool ReleaseLock(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            try
            {
                _fileSystem.File.Delete(this.GetLockFileName(driverFile));

                return true;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "An error occurred while trying to delete the .lock file.");
            }

            return false;
        }

        /// <summary>
        /// Attempts to write the specified <paramref name="content" /> to the .log file associated with the specified <paramref name="driverFile" />.
        /// </summary>
        /// <param name="driverFile">The driver file to release the lock for.</param>
        /// <param name="content">The content to be written to the log file.</param>
        /// <returns>
        /// <c>true</c> if the .log file is successfully written; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// If the .log file already exists it is overwritten with the specified <paramref name="content" />.
        /// </remarks>
        public bool WriteLogFile(string driverFile, string content)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));
            ArgumentHelpers.ThrowIf.IsNull(content, nameof(content));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            var logFileName = this.GetLogFileName(driverFile);

            try
            {
                _fileSystem.File.WriteAllText(logFileName, content);

                return true;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "An error occurred while trying to write the .log file.");
            }

            return false;
        }

        /// <summary>
        /// Attempts to delete the .log file associated with the specified <paramref name="driverFile" /> if it exists.
        /// </summary>
        /// <param name="driverFile">The driver file to delete the .log file for.</param>
        /// <returns>
        /// <c>true</c> if the .log does not exist or is successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public bool DeleteLogFile(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            try
            {
                _fileSystem.File.Delete(this.GetLogFileName(driverFile));

                return true;
            }
            catch(Exception exception)
            {
                _logger.LogError(exception, "An error occurred while trying to delete the .log file.");
            }

            return false;
        }

        /// <summary>
        /// Gets the name of the .lock file associated with the specified <paramref name="driverFile" />.
        /// </summary>
        /// <param name="driverFile">The driver file to get the .lock file name for.</param>
        /// <returns>
        /// The full path and file name of the .lock file associated with the specified <paramref name="driverFile" />.
        /// </returns>
        public string GetLockFileName(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            return _fileSystem.Path.ChangeExtension(driverFile, "lock");
        }

        /// <summary>
        /// Gets the name of the .log file associated with the specified <paramref name="driverFile" />.
        /// </summary>
        /// <param name="driverFile">The driver file to get the .log file name for.</param>
        /// <returns>
        /// The full path and file name of the .log file associated with the specified <paramref name="driverFile" />.
        /// </returns>
        public string GetLogFileName(string driverFile)
        {
            ArgumentHelpers.ThrowIf.IsNullOrEmpty(driverFile, nameof(driverFile));

            if(!_fileSystem.File.Exists(driverFile))
            {
                throw new FileNotFoundException();
            }

            return _fileSystem.Path.ChangeExtension(driverFile, "log");
        }
    }
}
