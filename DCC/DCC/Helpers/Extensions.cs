using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace DCC.Helpers
{
    public static class ExtensionsMethods
    {
        public static bool HasRows(this DataTable dataTable)
        {
            return dataTable.Rows.Count != 0;
        }
        public static bool HasTables(this DataSet dataSet)
        {
            return dataSet.Tables.Count != 0;
        }

        public static object GetColumnValueOrNull(this DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) ? row[column] : null;

        }

        public static T GetValueOrDefault<T>(this DataRow row, string column)
        {
            try
            {

            return row.Table.Columns.Contains(column) ? (row[column].GetType() != typeof(DBNull) ? (T)row[column] : default(T)) : default(T);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}