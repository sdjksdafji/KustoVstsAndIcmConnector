using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Oasys.BugClient;
using Microsoft.Oasys.Bugs.BugMaintenance;
using Microsoft.Oasys.Bugs.Schema;
using Kusto.Ingest;

namespace VSTSConnector
{
	class Program
	{

	static void Main(string[] args)
		{
			try
			{
					if (args[0] == "vsts")  //run the vsts query if 0 else ICM query
					{
						GetResolvedShipBlockingBugs("16.0.6630.1000");
					}
					else
					{
						ICMQueryManager icm = new ICMQueryManager();

					}
			}
			catch (Exception ex)
			{
					Console.WriteLine("please provide valid argument like vsts or icm");
				Console.WriteLine(ex);
			}
	}

		private static void GetResolvedShipBlockingBugs(string currentBuild)
		{
			// create a new BugClient to the VSTS instance for Office
			BugClient client = new BugClient(BugClient.Server.Live);

			// build a query to say Open Build = current build
			BugQuery query =
				new BugQuery(new[] {Field.ID, Field.OpenedBy, Field.Area, Field.AssignedTo, Field.OpenedDate, Field.OpenedRev},
				new QueryCondition(Field.OpenedRev, Comparison.Equals, currentBuild));

			BugClientResult result = client.Query("OfficeVSO", query);

			//write to a file
			StreamWriter file = new StreamWriter("ShipBlocking.csv",false);
			for (int i = 0; i < result.Bugs.Count; i++)
			{
				file.Write("\"");
                file.Write(result.Bugs[i].Fields["ID"]);
				file.Write("\",\"");
				file.Write(result.Bugs[i].Fields["OpenedBy"]);
				file.Write("\",\"");
				file.Write(result.Bugs[i].Fields["Area"]);
				file.Write("\",\"");
				file.Write(result.Bugs[i].Fields["OpenedDate"]);
				file.Write("\",\"");
				file.Write(result.Bugs[i].Fields["OpenedRev"]);
				file.Write("\",");
				file.WriteLine();
			}
			file.Flush();
			file.Close();
			Console.WriteLine("Now try to upload to Kusto syncly");
			IngestToKusto();
            Console.WriteLine("Uploading succeeded");
		}

		private static void IngestToKusto()
		{
			var kustoConnectionStringBuilderEngine = new Kusto.Data.KustoConnectionStringBuilder(@"https://wacprod.kusto.windows.net")
			{
				FederatedSecurity = true,
			};

			//Create a disposable client that will execute the ingestion
			using (IKustoIngestClient client = new KustoDirectIngestClient(kustoConnectionString: kustoConnectionStringBuilderEngine))
			{

				string kustoDatabase = "ULS";
				string kustoTable = "ShipBlockingBugs";
				var kustoIngestionProperties = new KustoIngestionProperties(databaseName: kustoDatabase, tableName: kustoTable);

				client.IngestFromSingleFile(@"C:\Users\shuywang\Documents\Visual Studio 2015\Projects\VSTSConnector\VSTSConnector\bin\Debug\ShipBlocking.csv", false, kustoIngestionProperties);
            }
		}

		private static void ActiveShipBlockingBugs(string currentBuild)
		{
			throw new NotImplementedException();
		}
	}
}
