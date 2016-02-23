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
			List<ActiveShipBlockingBug> dataToIngest = new List<ActiveShipBlockingBug>();


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
				//dataToIngest.Add(new ActiveShipBlockingBug(Int64.Parse(result.Bugs[i].Fields["ID"]),
				//	result.Bugs[i].Fields["OpenedBy"],
				//	result.Bugs[i].Fields["Area"],
				//	Convert.ToDateTime(result.Bugs[i].Fields["OpenedDate"]),
				//	result.Bugs[i].Fields["OpenedRev"]));
				//            foreach (KeyValuePair<string, string> field	 in result.Bugs[i].Fields)
				//{

				//	file.Write(/*field.Key + ":" + */field.Value);
				//	file.Write(",");
				//}
				file.Write(result.Bugs[i].Fields["ID"]);
				file.Write(",");
				file.Write(result.Bugs[i].Fields["OpenedBy"]);
				file.Write(",");
				file.Write(result.Bugs[i].Fields["Area"]);
				file.Write(",");
				file.Write(result.Bugs[i].Fields["OpenedDate"]);
				file.Write(",");
				file.Write(result.Bugs[i].Fields["OpenedRev"]);
				file.Write(",");
				file.WriteLine();
			}
			Console.WriteLine("Length: " + dataToIngest.Count);
			file.Flush();
			file.Close();
			Console.WriteLine("Now try to upload to Kusto sycnly");
			Console.ReadKey();
			IngestToKusto(dataToIngest);
            Console.WriteLine("Uploading succeeded");
			Console.ReadKey();
		}

		private static void IngestToKusto(IEnumerable<ActiveShipBlockingBug> dataToIngest)
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

	class ActiveShipBlockingBug
	{
		public ActiveShipBlockingBug()
		{

		}


		//var csvMapping = new List<Kusto.Data.Common.CsvColumnMapping>();
		//csvMapping.Add(new Kusto.Data.Common.CsvColumnMapping() { ColumnName = "ID", Ordinal = 0, CslDataType = "int" });
		//	csvMapping.Add(new Kusto.Data.Common.CsvColumnMapping() { ColumnName = "OpenedBy", Ordinal = 1, CslDataType = "string" });
		//	csvMapping.Add(new Kusto.Data.Common.CsvColumnMapping() { ColumnName = "Area", Ordinal = 2, CslDataType = "string" });
		//	csvMapping.Add(new Kusto.Data.Common.CsvColumnMapping() { ColumnName = "OpenedDate", Ordinal = 3, CslDataType = "datetime" });
		//	csvMapping.Add(new Kusto.Data.Common.CsvColumnMapping() { ColumnName = "OpenedRev", Ordinal = 4, CslDataType = "string" });
		//	string csvMappingString = Newtonsoft.Json.JsonConvert.SerializeObject(csvMapping);

public ActiveShipBlockingBug(long id, string openedBy, string area, DateTime openedDate, string openedRev)
		{
			ID = id;
			OpenedBy = openedBy;
			Area = area;
			OpenedDate = openedDate;
			OpenedRev = openedRev;
		}

		public long ID { get; set; }

		public string OpenedBy { get; set; }

		public string Area { get; set; }

		public DateTime OpenedDate { get; set; }

		public string OpenedRev { get; set; }
	}
}
