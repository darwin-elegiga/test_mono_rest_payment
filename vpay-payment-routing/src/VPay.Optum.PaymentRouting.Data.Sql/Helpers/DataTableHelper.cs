using System.Collections.Generic;
using System.Data;

namespace VPay.Optum.PaymentRouting.Data.Sql.Helpers
{
    public static class DataTableHelper
    {
        public static DataTable ToIdDataTable(this IEnumerable<long> list)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Id");

            foreach (var id in list)
            {
                dataTable.Rows.Add(id);
            }

            return dataTable;
        }
    }
}
