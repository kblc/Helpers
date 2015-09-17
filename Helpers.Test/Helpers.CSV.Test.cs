using System;
using Helpers.CSV;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Helpers.CSV.Test
{
    [TestClass]
    public class HelpersCSVTest
    {
        [TestMethod]
        public void HelperCSV_LoadAndSave()
        {
            var tableName = "test";
            var filePath = "{virtual}";

            var sb = new List<string>();
            sb.Add("test0_column;test_column;test_column;");
            sb.Add("data0;data1;;data3");
            sb.Add(string.Empty);

            var res0 = CSVFile.Load(lines: sb.ToArray(), tableName: tableName, filePath: filePath, verboseLogAction: (s) => Console.WriteLine(s));
            Assert.AreEqual(3, res0.TotalRowCount, "Total row cound must be 3");
            Assert.AreEqual(2, res0.ProcessedRowCount, "Processed row count must be 2");
            Assert.AreEqual(4, res0.Table.Columns.Count, "Column count must be 4");
            Assert.AreEqual(1, res0.Table.Rows.Count, "Data row count must be 1");
            Assert.AreEqual(filePath, res0.FilePath, "File path must equals");
            Assert.AreEqual(tableName, res0.Table.TableName, "Table name must equals");
            Assert.AreEqual("data0", res0.Table.Rows[0][0], "Row data must equals");

            var res1 = CSVFile.Save(res0.Table, verboseLogAction: (s) => Console.WriteLine(s)).ToArray();

            foreach (var r in res1)
                Console.WriteLine(r);

            Assert.AreEqual(2, res1.Length, "Lines count must be 2");
            Assert.AreEqual(sb[1],res1[1], "Data lines must equals");

            var res2 = CSVFile.Save(res0.Table, filePath: "test.csv", encoding: Encoding.UTF8, verboseLogAction: Console.WriteLine);
            var res3 = CSVFile.Load(filePath: "test.csv", fileEncoding: Encoding.UTF8, verboseLogAction: Console.WriteLine);

            Assert.AreEqual(res3.Table.Columns.Count, res0.Table.Columns.Count, "Column count must equals");
            Assert.AreEqual(res3.Table.Rows.Count, res0.Table.Rows.Count, "Row count must equals");
            for(int i=0; i< Math.Min(res3.Table.Columns.Count, res0.Table.Columns.Count); i++)
                Assert.AreEqual(res0.Table.Columns[i].ColumnName,res3.Table.Columns[i].ColumnName, "Columns name must equals");
            
            for(int n = 0; n<Math.Min(res3.Table.Rows.Count, res0.Table.Rows.Count);n++)
                for (int i = 0; i < Math.Min(res3.Table.Columns.Count, res0.Table.Columns.Count); i++)
                    Assert.AreEqual(res0.Table.Rows[n][i].ToString(), res3.Table.Rows[n][i].ToString(), "Rows data must equals");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void HelperCSV_TableValidator()
        {
            var tableName = "test";
            var filePath = "{virtual}";
            var badColumn = "exception_column";

            var validator0 = new Action<DataTable>((table) => 
            {
                if (table.Columns.OfType<DataColumn>().Select(c => c.ColumnName).Contains(badColumn))
                    throw new ArgumentException("column", string.Format("Table contains '{0}' column", badColumn));
            });

            var validator1 = new Action<DataTable>((table) =>
            {
                if (table.Columns.OfType<DataColumn>().Select(c => c.ColumnName).Contains(badColumn))
                    throw new Exception(string.Format("Table contains '{0}' column", badColumn));
            });

            var sb = new List<string>();
            sb.Add("test0_column;test_column;test_column;;");

            var rnd = new Random(1);
            rnd.Next();

            for(var i=0; i < 10000; i++)
                sb.Add(string.Format("data{0};data{1};;data{2};", rnd.Next(0, 100), rnd.Next(0, 100), rnd.Next(0, 100)));

            sb.Add(string.Empty);

            var res0 = CSVFile.Load(lines: sb,
                tableName: tableName,
                filePath: filePath,
                verboseLogAction: (s) => Console.WriteLine(s),
                tableValidator: validator0);

            var sb1 = new List<string>();
            sb1.Add("test0_column;test_column;test_column;;" + badColumn);
            sb1.Add("data0;data1;;data3;");
            sb1.Add(string.Empty);

            var res1 = CSVFile.Load(lines: sb1,
                tableName: tableName,
                filePath: filePath,
                verboseLogAction: (s) => Console.WriteLine(s),
                tableValidator: validator1);
        }

        [TestMethod]
        public void HelperCSV_RowFilter()
        {
            var tableName = "test";
            var filePath = "{virtual}";
            var badColumn = "exception_column";
            var badData = "exception_data";

            var filter0 = new Func<DataRow, bool>((row) =>
            {
                return false;
            });

            var filter1 = new Func<DataRow, bool>((row) =>
            {
                foreach (var c in row.Table.Columns.OfType<DataColumn>())
                    if (row[c].ToString().Contains(badData))
                        return true;
                return false;
            });

            var sb = new List<string>();
            sb.Add("test0_column;test_column;test_column;;");
            var rnd = new Random(1);
            rnd.Next();

            for (var i = 0; i < 10000; i++)
            { 
                sb.Add(string.Format("data{0};data{1};;data{2};", rnd.Next(0, 100), rnd.Next(0, 100), rnd.Next(0, 100)));
            }
            sb.Add(string.Empty);

            var res0 = CSVFile.Load(
                lines: sb,
                tableName: tableName,
                filePath: filePath,
                verboseLogAction: (s) => Console.WriteLine(s), 
                
                rowFilter: filter0 );

            var sb1 = new List<string>();
            sb1.Add("test0_column;test_column;test_column;;" + badColumn);
            for (var i = 0; i < 10000; i++)
                sb1.Add(string.Format("data{0};data{1};;data{2};", rnd.Next(0, 100), rnd.Next(0, 100), rnd.Next(0, 100)) + (i % 2 == 0 ? badData : "good_data") );

            var res1 = CSVFile.Load(lines: sb1,
                tableName: tableName,
                filePath: filePath,
                verboseLogAction: (s) =>
                {
                    Console.WriteLine(s);
                    if (s.Contains("good_data"))
                        throw new Exception("Good data filtered!");
                },
                rowFilter: filter1);

            var tableColumns = res1.Table
                    .Columns
                    .OfType<DataColumn>()
                    .Select(c => c.ColumnName)
                    .ToArray();

            var badCount = res1.Table.Rows.OfType<DataRow>().Count(dr => tableColumns.Any(c => dr[c].ToString().Contains(badData)));
            Assert.AreEqual(10000 / 2, res1.Table.Rows.Count, "Filtered rows count must be 5000");
            Assert.AreEqual(0, badCount, "Filtered bad rows count must be 0");
        }

        [TestMethod]
        public void HelperCSV_MergeTables()
        {
            var dt0 = new DataTable();
            dt0.Columns.Add(new DataColumn() { ColumnName = "id", DataType = typeof(int) });
            dt0.Columns.Add(new DataColumn() { ColumnName = "name", DataType = typeof(string) });
            dt0.PrimaryKey = new DataColumn[] { dt0.Columns[0] };
            var dr0 = dt0.NewRow();
            dr0["id"] = 1;
            dr0["name"] = "test name";
            dt0.Rows.Add(dr0);

            var dt1 = new DataTable();
            dt1.Columns.Add(new DataColumn() { ColumnName = "id", DataType = typeof(int) });
            dt1.Columns.Add(new DataColumn() { ColumnName = "last_name", DataType = typeof(string) });
            dt1.PrimaryKey = new DataColumn[] { dt1.Columns[0] };
            var dr1 = dt1.NewRow();
            dr1["id"] = 1;
            dr1["last_name"] = "test last name";
            dt1.Rows.Add(dr1);

            var res = CSV.CSVFile.MergeTables(new DataTable[] { dt0, dt1 }, new string[] { "id" });
            Assert.AreEqual(1, res.Rows.Count, "Row count must be 1");
            Assert.AreEqual(3, res.Columns.Count, "Column count must be 3");

            var dr2 = dt1.NewRow();
            dr2["id"] = 2;
            dr2["last_name"] = "test last name";
            dt1.Rows.Add(dr2);

            var res2 = CSV.CSVFile.MergeTables(new DataTable[] { dt0, dt1 }, new string[] { "id" });
            Assert.AreEqual(2, res2.Rows.Count, "Row count must be 2");
            Assert.AreEqual(3, res2.Columns.Count, "Column count must be 3");
        }

        [TestMethod]
        public void HelperCSV_MergeBigTables()
        {
            var dt0 = new DataTable();
            dt0.Columns.Add(new DataColumn() { ColumnName = "id", DataType = typeof(int) });
            dt0.Columns.Add(new DataColumn() { ColumnName = "name", DataType = typeof(string) });
            dt0.PrimaryKey = new DataColumn[] { dt0.Columns[0] };

            var dt1 = new DataTable();
            dt1.Columns.Add(new DataColumn() { ColumnName = "id", DataType = typeof(int) });
            dt1.Columns.Add(new DataColumn() { ColumnName = "last_name", DataType = typeof(string) });
            dt1.PrimaryKey = new DataColumn[] { dt1.Columns[0] };
            
            for(int i=0; i<10000; i++)
            { 
                var dr0 = dt0.NewRow();
                dr0["id"] = i + 1;
                dr0["name"] = string.Format("test name {0}", i);
                dt0.Rows.Add(dr0);

                var dr1 = dt1.NewRow();
                dr1["id"] = i + 1;
                dr1["last_name"] = string.Format("test last name {0}", i);
                dt1.Rows.Add(dr1);
            }

            var res = CSV.CSVFile.MergeTables(new DataTable[] { dt0, dt1 }, new string[] { "id" });
            Assert.AreEqual(10000, res.Rows.Count, "Row count must be 10000");
            Assert.AreEqual(3, res.Columns.Count, "Column count must be 3");
        }
    }
}
