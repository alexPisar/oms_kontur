using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using UtilitesLibrary.ConfigSet;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
	public partial class EdiDbContext
	{
		public string SelectSingleValue(string Sql)
		{
			OracleDataReader reader;
			string retVal = "";
			using (OracleCommand command = new OracleCommand())
			{
				command.Connection = (OracleConnection)GetDefaultConnection();
				command.CommandType = CommandType.Text;
				command.CommandText = Sql;

				if (command.Connection.State != ConnectionState.Open)
					command.Connection.Open();

				reader = command.ExecuteReader();

				while (reader.Read())
				{
					retVal = reader[0].ToString();
				}
			}
			return retVal;
		}
		
		private static DbConnection GetDefaultConnection()
		{
			DbConnection connection = Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance.CreateConnection();
            var connStr = new DataBaseConnection(Config.GetInstance().EdiDataBaseIpAddress, Config.GetInstance().EdiDataBaseSid, Config.GetInstance().GetDataBaseUser(), Config.GetInstance().GetDataBasePassword()).GetConnectionString();
            connection.ConnectionString = connStr;
			return connection;
		}

	}
}

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
	public partial class AbtDbContext
	{
		public string SelectSingleValue(string Sql)
		{
			OracleDataReader reader;
			string retVal = "";
			using (OracleCommand command = new OracleCommand())
			{
				command.Connection = (OracleConnection)this?.Database?.Connection ?? (OracleConnection)GetDefaultConnection();
				command.CommandType = CommandType.Text;
				command.CommandText = Sql;

				if (command.Connection.State != ConnectionState.Open)
					command.Connection.Open();

				reader = command.ExecuteReader();

				while (reader.Read())
				{
					retVal = reader[0].ToString();
				}
			}
			return retVal;
		}

        public void ExecuteProcedure(string procedureName, params object[] parameters)
        {
            var commandText = string.Empty;

            if (parameters?.Count() > 0)
                foreach (var par in parameters)
                    commandText += string.IsNullOrEmpty(commandText) ? $"{((OracleParameter)par).ParameterName} => :{((OracleParameter)par).ParameterName}"
                        : $", {((OracleParameter)par).ParameterName} => :{((OracleParameter)par).ParameterName}";

            commandText = $"begin {procedureName}({commandText});END;";
            Database.ExecuteSqlCommand(commandText, parameters);
        }

        private static DbConnection GetDefaultConnection()
		{
			DbConnection connection = Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance.CreateConnection();
            var connStr = new DataBaseConnection(Config.GetInstance().AbtDataBaseIpAddress, Config.GetInstance().AbtDataBaseSid, Config.GetInstance().GetDataBaseUser(), Config.GetInstance().GetDataBasePassword()).GetConnectionString();
			connection.ConnectionString = connStr;
			return connection;
		}

		private static DbConnection GetDefaultConnection(string connStr)
		{
			DbConnection connection = Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance.CreateConnection();
			connection.ConnectionString = connStr;
			return connection;
		}

		public AbtDbContext(string nameOrConnectionString, bool unknown) : base( GetDefaultConnection( nameOrConnectionString ), unknown )
		{
			Configure();
		}


	}
}
