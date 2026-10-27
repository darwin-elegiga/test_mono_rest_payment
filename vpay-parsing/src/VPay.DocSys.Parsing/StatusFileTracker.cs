using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Extensions.Logging;
using VPay.DocSys.Common;
using VPay.DocSys.Data;
using VPay.DocSys.Parsing.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing
{
    public class StatusFileTracker : IStatusFileTracker
    {
        private readonly string _connectionString;
        private readonly SqlServerPolicyHolder _sqlServerPolicies;
        private readonly Edi835ConfigurationCache _edi835ConfigurationCache;

        public StatusFileTracker(
            DocSysConnectionSettings connectionSettings,
            Edi835ConfigurationCache edi835ConfigurationCache,
            ILogger<StatusFileTracker> logger)
        {
            _connectionString = connectionSettings.GetConnectionString();
            _edi835ConfigurationCache = edi835ConfigurationCache ?? throw new ArgumentNullException(nameof(edi835ConfigurationCache));
            _sqlServerPolicies = new SqlServerPolicyHolder(logger);
        }

        public int InsertStatusFile(string fileName, string filePath, string tpa, string originCode, string billingEntity)
        {
            string procName = "dbo.usp_PaymentStatusFile_Insert";

            return _sqlServerPolicies.InsertPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(procName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@StatusFileName", fileName));
                    cmd.Parameters.Add(new SqlParameter("@StatusFilePath", filePath));
                    cmd.Parameters.Add(new SqlParameter("@TPA", tpa));
                    cmd.Parameters.Add(new SqlParameter("@OriginCode", originCode));
                    cmd.Parameters.Add(new SqlParameter("@BillingEntityCode", billingEntity));
                    cmd.Connection.Open();

                    int fileId = Convert.ToInt32(cmd.ExecuteScalar());
                    cmd.Connection.Close();
                    return fileId;
                }

            }, procName);
        }
        public FileStatus GetFileStatus(int fileId)
        {
            string procName = "dbo.usp_PaymentStatusFile_GetStatus";

            return _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(procName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PaymentStatusFileId", fileId);
                    cmd.Connection.Open();

                    int statusId = Convert.ToInt32(cmd.ExecuteScalar());
                    cmd.Connection.Close();
                    return (FileStatus)statusId;
                }

            }, procName);
        }


        public List<dynamic> GetDocumentStatus(string fileName)
        {
            var returnValues = new List<dynamic>();
            string procName = "dbo.usp_PaymentStatusFile_GetDocumentStatus";

            _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(procName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@FileName", fileName);
                    cmd.Connection.Open();

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var TPA = reader.GetString("TPA");
                        DateTime? DocsCompleted = reader.GetValue("DocsCompleted") as DateTime?;
                        DateTime? FileReceived = reader.GetValue("FileReceived") as DateTime?;

                        var result = new
                        {
                            FileName = fileName,
                            TPA = TPA,
                            DocsCompleted = DocsCompleted,
                            FileReceived = FileReceived
                        };
                        returnValues.Add(result);
                    }
                    cmd.Connection.Close();
                }

            }, procName);

            return returnValues;
        }

        public long GetFileId(string filePath)
        {
            string path = Path.GetDirectoryName(filePath);
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }

            string fileName = Path.GetFileName(filePath);
            long fileId = 0;
            string sql = @"
        SELECT paymentStatusFileId
        FROM PaymentStatusFile
        WHERE StatusFilePath=@StatusFilePath
              AND
              StatusFileName=@StatusFileName";

            _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@StatusFilePath", path);
                    cmd.Parameters.AddWithValue("@StatusFileName", fileName);
                    cmd.Connection.Open();

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        fileId = Convert.ToInt64(result);
                    }

                    cmd.Connection.Close();
                }

            }, nameof(GetFileId));

            return fileId;
        }

        public string GetStatusFileNameFromTransaction(long transactionId)
        {
            string statusFileName = null;

            var sql = @"SELECT PSF.StatusFilePath + PSF.StatusFileName as StatusFileName
                FROM[dbo].[PaymentStatusFile] AS PSF
                INNER JOIN[dbo].[DocumentTransaction] AS DT ON DT.PaymentStatusFileId = PSF.PaymentStatusFileId
                WHERE DT.TransactionId = @TransactionID";

            _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@TransactionID", transactionId);
                    cmd.Connection.Open();

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        statusFileName = result.ToString();
                    }

                    cmd.Connection.Close();
                }

            }, nameof(GetStatusFileNameFromTransaction));

            return statusFileName;
        }

        public void UpdateFileStatus(int fileId, int statusId)
        {
            string procName = "dbo.usp_PaymentStatusFile_InsertStatus";

            _sqlServerPolicies.UpdatePolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(procName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PaymentStatusFileId", fileId);
                    cmd.Parameters.AddWithValue("@StatusCode", statusId);
                    cmd.Connection.Open();

                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }

            }, procName);
        }

        public void CleanTransactionStatus(IList<long> transactionIds)
        {
            var idMetaData = new SqlMetaData[1]
            {
                    new SqlMetaData("TransactionId", SqlDbType.BigInt)
            };
            var idRecords = transactionIds.Distinct().Select(t =>
            {
                var row = new SqlDataRecord(idMetaData);
                row.SetValue(0, t); // TransactionId
                return row;
            });

            string sql = @"
                BEGIN TRANSACTION

                DELETE ts
                FROM TransactionStatus ts
                INNER JOIN @tvpTransactionIds ti
                    ON ti.TransactionId = ts.TransactionId;

                DELETE rcts
                FROM RedCardTransactionStatus rcts
                INNER JOIN @tvpTransactionIds ti
                    ON ti.TransactionId = rcts.TransactionId;

                DELETE dd
                FROM DocumentDisposition dd
                INNER JOIN @tvpTransactionIds ti
                    ON ti.TransactionId = dd.TransactionId;

                COMMIT TRANSACTION
                ";

            _sqlServerPolicies.DeletePolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var idTvp = cmd.Parameters.AddWithValue("@tvpTransactionIds", idRecords);
                    idTvp.SqlDbType = SqlDbType.Structured;
                    idTvp.TypeName = "[dbo].[DocumentTransactionID]";

                    cmd.CommandType = CommandType.Text;
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }

            }, nameof(CleanTransactionStatus));
        }
        public void InsertFileTransactions(int fileId, List<DocumentTransactionMessage> messages)
        {
            _sqlServerPolicies.InsertPolicy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {

                    conn.Open();
                    using (SqlBulkCopy copier = new SqlBulkCopy(conn))
                    {
                        try
                        {
                            var dataTable = CreateDataTable(fileId, messages, _edi835ConfigurationCache);
                            foreach (var column in dataTable.Columns.Cast<DataColumn>())
                            {
                                copier.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                            }
                            copier.DestinationTableName = "DocumentTransaction";
                            copier.WriteToServer(dataTable);
                        }
                        catch (SqlException ex)
                        {
                            throw new ApplicationException("Attempt to re-run transaction without cleandocs.", ex);
                        }
                    }
                }

            }, nameof(InsertFileTransactions));
        }

        public virtual IDictionary<string, object> GetStatusFileSummary(string stsFilePath)
        {
            var procName = "[dbo].[usp_PaymentStatusFile_GetSummary]";

            return _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(procName, connection))
                {
                    connection.Open();

                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@FullStatusFilePath", stsFilePath));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var dictionary = new Dictionary<string, object>(reader.FieldCount);

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                dictionary.Add(reader.GetName(i), reader.GetValue(i));
                            }

                            return (IDictionary<string, object>)dictionary;
                        }
                        else
                        {
                            throw new FileNotFoundException();
                        }
                    }
                }

            }, procName);
        }

        public bool CanConnect()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this._connectionString)
            {
                //Since this is a connectivity check, we don't need to wait 30 seconds to consider it a failure.
                ConnectTimeout = 2
            };
            using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (SqlException) { }
                return conn.State == ConnectionState.Open;
            }

        }
        private static DataTable CreateDataTable(int fileId, List<DocumentTransactionMessage> messages, Edi835ConfigurationCache cache)
        {
            DataTable table = new DataTable();
            DataRow row;
            #region Create Columns
            table.Columns.Add("TransactionId", typeof(Int64));
            table.Columns.Add("PaymentStatusFileId", typeof(Int64));
            table.Columns.Add("OrigDistType", typeof(char));
            table.Columns.Add("IsEdi835Needed", typeof(bool));
            table.Columns.Add("ClaimDate", typeof(DateTime));
            table.Columns.Add("ClaimId", typeof(string));
            table.Columns.Add("IsReissue", typeof(bool));
            table.Columns.Add("LoadDate", typeof(DateTime));
            table.Columns.Add("NonFundedPaymentDetailId", typeof(Int64));
            table.Columns.Add("PayeeName", typeof(string));
            table.Columns.Add("PaymentAmount", typeof(decimal));
            table.Columns.Add("PaymentGuid", typeof(string));
            table.Columns.Add("PaymentId", typeof(string));
            table.Columns.Add("PaymentTypeCode", typeof(string));
            table.Columns.Add("TaxId", typeof(string));
            table.Columns.Add("UserKey", typeof(string));
            #endregion
            foreach (var msg in messages)
            {
                row = table.NewRow();
                row["TransactionId"] = Convert.ToInt64(msg.TransactionId.RemoveLeadingZeroes());
                row["PaymentStatusFileId"] = fileId;
                row["OrigDistType"] = msg.StatusRecord.DIST;
                row["IsEdi835Needed"] = msg.IsEdi835Needed(cache);
                row["ClaimDate"] = msg.StatusRecord.CLAIMDATE;
                row["ClaimId"] = msg.StatusRecord.CLAIMID;
                row["IsReissue"] = !msg.GenerateEOB;
                row["LoadDate"] = msg.StatusRecord.LOADDATE;
                row["NonFundedPaymentDetailId"] = Convert.ToInt64(msg.StatusRecord.NFPYDT_ID.RemoveLeadingZeroes());
                row["PayeeName"] = msg.StatusRecord.PAYEENAME;
                row["PaymentAmount"] = msg.StatusRecord.AMOUNT;
                row["PaymentGuid"] = msg.StatusRecord.GUID;
                row["PaymentId"] = msg.StatusRecord.USERKEY;
                row["PaymentTypeCode"] = msg.StatusRecord.PAYTYPE;
                row["TaxId"] = msg.StatusRecord.TIN;
                row["UserKey"] = msg.StatusRecord.USERKEY;
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Get STP payment by Transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction Id.</param>
        /// <returns>The STP payment for the Transaction Id if record exists; Null otherwise.</returns>
        public StpPayment? GetStpPayment(long transactionId)
        {
            string procName = "[dbo].[usp_StpPayment_GetByTransactionId]";
            var returnValues = new List<StpPayment>();

            _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(procName, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Connection.Open();

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var result = new StpPayment
                    {
                        TransactionId = reader.GetInt64("TransactionId"),
                        NetworkName = reader.GetString("NetworkName"),
                        EnrollType = reader.GetString("EnrollType"),
                        Edi835Type = reader.GetString("Edi835Type"),
                        RemitValid = reader.GetBoolean("RemitValid"),
                        Edi835Avail = reader.GetBoolean("Edi835Avail"),
                        PaymentSent = reader.GetBoolean("PaymentSent"),
                        PmbMessageSent = reader.GetBoolean("PmbMessageSent"),
                        RemitValidDateTime = reader.GetValue("RemitValidDateTime") as DateTime?,
                        Edi835AvailDateTime = reader.GetValue("Edi835AvailDateTime") as DateTime?,
                        PaymentSentDateTime = reader.GetValue("PaymentSentDateTime") as DateTime?,
                        PmbMessageSentDateTime = reader.GetValue("PmbMessageSentDateTime") as DateTime?,
                        PaymentSentRetries = reader.GetInt32("PaymentSentRetries"),
                        CreatedUser = reader.GetString("CreatedUser"),
                        CreatedDateTime = reader.GetDateTime("CreatedDateTime"),
                        LastModifiedUser = reader.GetString("LastModifiedUser"),
                        LastModifiedDateTime = reader.GetDateTime("LastModifiedDateTime")
                    };
                    returnValues.Add(result);
                }
                cmd.Connection.Close();

            }, procName);

            return returnValues.FirstOrDefault();
        }

        /// <summary>
        /// Get list of STP payments by Transaction IDs.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids for the payments to return.</param>
        /// <returns>The list of STP payments for the provided Transaction Ids, if any; Empty list otherwise.</returns>
        public IEnumerable<StpPayment> GetStpPayments(IEnumerable<long> transactionIds)
        {
            string procName = "[dbo].[usp_StpPayment_GetByTransactionIds]";
            var returnValues = new List<StpPayment>();

            _sqlServerPolicies.SelectPolicy.Execute(() =>
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(procName, conn);

                cmd.CommandType = CommandType.StoredProcedure;
                var dataTable = new DataTable();
                dataTable.Columns.Add("TransactionId", typeof(long));
                foreach (long transactionId in transactionIds)
                {
                    dataTable.Rows.Add(transactionId);
                }
                SqlParameter parameter = cmd.Parameters.Add("@TransactionIds", SqlDbType.Structured);
                parameter.Value = dataTable;
                parameter.TypeName = "[dbo].[DocumentTransactionId]";

                cmd.Connection.Open();

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var result = new StpPayment
                    {
                        TransactionId = reader.GetInt64("TransactionId"),
                        NetworkName = reader.GetString("NetworkName"),
                        EnrollType = reader.GetString("EnrollType"),
                        Edi835Type = reader.GetString("Edi835Type"),
                        RemitValid = reader.GetBoolean("RemitValid"),
                        Edi835Avail = reader.GetBoolean("Edi835Avail"),
                        PaymentSent = reader.GetBoolean("PaymentSent"),
                        PmbMessageSent = reader.GetBoolean("PmbMessageSent"),
                        RemitValidDateTime = reader.GetValue("RemitValidDateTime") as DateTime?,
                        Edi835AvailDateTime = reader.GetValue("Edi835AvailDateTime") as DateTime?,
                        PaymentSentDateTime = reader.GetValue("PaymentSentDateTime") as DateTime?,
                        PmbMessageSentDateTime = reader.GetValue("PmbMessageSentDateTime") as DateTime?,
                        PaymentSentRetries = reader.GetInt32("PaymentSentRetries"),
                        CreatedUser = reader.GetString("CreatedUser"),
                        CreatedDateTime = reader.GetDateTime("CreatedDateTime"),
                        LastModifiedUser = reader.GetString("LastModifiedUser"),
                        LastModifiedDateTime = reader.GetDateTime("LastModifiedDateTime")
                    };
                    returnValues.Add(result);
                }
                cmd.Connection.Close();

            }, procName);

            return returnValues;
        }

        /// <summary>
        /// Insert multiple STP payments into database table using SQL Bulk Copy.
        /// All payments will be created with the same network name, enrollment type, and EDI 835 type.
        /// </summary>
        /// <param name="transactionIds">The list of transaction Ids for the payments to insert.</param>
        /// <param name="networkName">The network partner name. Required.</param>
        /// <param name="enrollType">The enrollment type: B2B or C2B. Required.</param>
        /// <param name="edi835Type">The type of EDI 835 to use: SDS or PRS. Required.</param>
        /// <param name="remitValid">The remit valid status. Default is false.</param>
        /// <param name="edi835Avail">The EDI 835 available status. Default is false.</param>
        /// <param name="paymentSent">The payment sent status. Default is false.</param>
        /// <param name="pmbMessageSent">The PMB message sent status. Default is false.</param>
        /// <exception cref="DataException"></exception>
        public void BulkInsertStpPayments(
            IEnumerable<long> transactionIds,
            string networkName,
            string enrollType,
            string edi835Type,
            bool remitValid = false,
            bool edi835Avail = false,
            bool paymentSent = false,
            bool pmbMessageSent = false)
        {
            _sqlServerPolicies.InsertPolicy.Execute(() =>
            {
                using SqlConnection conn = new(_connectionString);
                conn.Open();
                using SqlBulkCopy copier = new(conn);

                try
                {
                    var dataTable = CreateDataTable(
                        transactionIds,
                        networkName,
                        enrollType,
                        edi835Type,
                        remitValid,
                        edi835Avail,
                        paymentSent,
                        pmbMessageSent);

                    foreach (var column in dataTable.Columns.Cast<DataColumn>())
                    {
                        copier.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                    copier.DestinationTableName = "StpPayment";
                    copier.WriteToServer(dataTable);
                }
                catch (SqlException ex)
                {
                    throw new DataException("Error occurred while bulk inserting STP payments.", ex);
                }
            }, nameof(BulkInsertStpPayments));
        }

        private static DataTable CreateDataTable(
            IEnumerable<long> transactionIds,
            string networkName,
            string enrollType,
            string edi835Type,
            bool remitValid,
            bool edi835Avail,
            bool paymentSent,
            bool pmbMessageSent)
        {
            DataTable table = new();
            DataRow row;

            table.Columns.Add("TransactionId", typeof(long));
            table.Columns.Add("NetworkName", typeof(string));
            table.Columns.Add("EnrollType", typeof(string));
            table.Columns.Add("Edi835Type", typeof(string));
            table.Columns.Add("RemitValid", typeof(bool));
            table.Columns.Add("Edi835Avail", typeof(bool));
            table.Columns.Add("PaymentSent", typeof(bool));
            table.Columns.Add("PmbMessageSent", typeof(bool));

            foreach (var transactionId in transactionIds)
            {
                row = table.NewRow();
                row["TransactionId"] = transactionId;
                row["NetworkName"] = networkName;
                row["EnrollType"] = enrollType;
                row["Edi835Type"] = edi835Type;
                row["RemitValid"] = remitValid;
                row["Edi835Avail"] = edi835Avail;
                row["PaymentSent"] = paymentSent;
                row["PmbMessageSent"] = pmbMessageSent;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
