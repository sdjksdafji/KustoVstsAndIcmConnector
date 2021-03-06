﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using Kusto.Ingest;

namespace VSTSConnector
{
	public class ICMQueryManager
	{
		// need a connection object
		private SqlConnection connection;

		// need a query

		// need to write to a file

		public ICMQueryManager()
		{
			SqlDataReader reader;
			// get connection string
			GetSqlConnectionString();

			SqlCommand command = new SqlCommand(SqlQuery, connection);

			// execute the query
			
					connection.Open();
					reader = command.ExecuteReader();
			//connection.Close();


			using (StreamWriter file = new StreamWriter("ICMIncidents.csv", false))
			{
				while (reader.Read())
				{
					Console.WriteLine(reader.FieldCount);
					for (int j = 0; j < reader.FieldCount; j++)
					{
						file.Write("\"");
						file.Write(reader[j].ToString());
						file.Write("\",");
					}
					file.Flush();
					file.WriteLine();
				}
				reader.Close();
			}

			IngestToKusto();
        }

		private void GetSqlConnectionString()
		{
			connection = new SqlConnection();
			connection.ConnectionString = "Data Source = aesv0zny1o.database.windows.net;" +
										   "Initial Catalog = IcMDataWarehouse;" +
										   "User id = username;" +
										   "Password = password;";
		}

		private void IngestToKusto()
		{
			var kustoConnectionStringBuilderEngine = new Kusto.Data.KustoConnectionStringBuilder(@"https://wacprod.kusto.windows.net")
			{
				FederatedSecurity = true,
			};

			//Create a disposable client that will execute the ingestion
			using (IKustoIngestClient client = new KustoDirectIngestClient(kustoConnectionString: kustoConnectionStringBuilderEngine))
			{

				string kustoDatabase = "ULS";
				string kustoTable = "IcmIncidents";
				var kustoIngestionProperties = new KustoIngestionProperties(databaseName: kustoDatabase, tableName: kustoTable);

				client.IngestFromSingleFile(@"C:\Users\shuywang\Documents\Visual Studio 2015\Projects\VSTSConnector\VSTSConnector\bin\Debug\ICMIncidents.csv", false, kustoIngestionProperties);
			}
		}


		/// <summary>
		/// <remarks>
		/// This query returns the active incidents associated with WAC environments for the last 14 days
		/// </remarks>
		///</summary>
		private const String SqlQuery =
			"select	[IncidentId]," +
			"[Title]," +
			"[SourceName]," +
			"[CreateDate] As OpenDate," +
			"[ResolveDate] As ResolveDate, " +
			"[SourceCreatedBy] As InitiatedBy," +
			"[OccurringEnvironment]," +
			"[OccurringDatacenter]," +
			"[OwningteamName] As TeamAssignedTo, " +
			"[OwningContactAlias] As AssignedTo, " +
			"[ParentIncidentId]	As ParentIncidentId, " +
			"[Severity]," +
			"[IsNoise]," +
			"[IsCustomerImpacting]" +
			"from	[WarehouseIncidents] " +
			"where	[ResponsibleTenantName] = 'Office Online'" +
			"and	[status] = 'Active'" +
			"and	[SourceCreatedBy] <> 'ActiveMonitoring'" +
			"and	[Severity] <=2 " +
			"and	[Isnoise] = 0 " +
			"and	((SourceCreateDate >= GETDATE() - 14 AND WarehouseIncidents.status = 'Active') OR (SourceCreateDate >= GETDATE() - 14 AND WarehouseIncidents.status = 'RESOLVED') OR (SourceCreateDate >= GETDATE() - 14 AND WarehouseIncidents.status = 'MITIGATED'))"+
			"order by OpenDate";
	}
}
