using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace DCC.SQLHelpers.Helpers
{
    public class SQLHelper
    {
        private readonly int _companyId;
        public SQLHelper()
        {

        }
        //toCheck
        public SQLHelper(int companyId)
        {
            this._companyId = companyId;
        }
        public DataTable ExecuteSQLForDataTable(string connectionString, string sqlStatement, CommandType commandType = CommandType.StoredProcedure)
        {
            var response = new DataTable();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sqlStatement, sqlConnection)
                    {
                        CommandType = commandType
                    };
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(response);
                }
            }
            catch (Exception ex)
            {
            }
            return response;
        }

        public DataSet ExecuteSQLForDataSet(string connectionString, string sqlStatement, CommandType commandType = CommandType.StoredProcedure)
        {
            var response = new DataSet();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand(sqlStatement, sqlConnection)
                    {
                        CommandType = commandType
                    };
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(response);
                }
            }
            catch (Exception ex)
            {
            }
            return response;
        }

        public void ExecuteSqlDataAdapter(SqlCommand sqlCommand, DataTable dataTable)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand);
                adapter.Fill(dataTable);
            }
            catch (Exception ex)
            {
            }
        }
        public void ExecuteSqlDataAdapter(SqlCommand sqlCommand, DataSet dataSet)
        {
            try
            {
            SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand);
            adapter.Fill(dataSet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public SqlCommand CreateSQLCommand(SqlConnection sqlConnection, CommandType commandType, string commandText)
        {
            return new SqlCommand(commandText, sqlConnection)
            {
                CommandType = commandType
            };
        }
    }
}









