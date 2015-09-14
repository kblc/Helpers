using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Helpers.CSV
{
    public class CSVFile
    {
        public DataTable Table { get; private set; }
        public int TotalRowCount { get; private set; }
        public int ProcessedRowCount { get; private set; }
        public string FilePath { get; private set; }

        private CSVFile() { }

        private static string ClearField(string field)
        {
            string result = field;
            if (!string.IsNullOrEmpty(field))
            {
                if (result[0] == '\"')
                    result = result.Remove(0, 1);
                if (result.Length > 0 && result[result.Length - 1] == '\"')
                    result = result.Remove(result.Length - 1, 1);
            }
            return result;
        }

        private static string[] GetCsvFields(string line, string delimiter)
        {
            List<string> result = new List<string>();

            while (true)
            {
                if (line.Length > 0 && line[0] == '"')
                {
                    int ind = -1;
                    if (line.Length > 1)
                        for (int i = 1; i < line.Length; i++)
                            if (line[i] == '"'
                                  && (i == line.Length - 1 || line[i + 1] == delimiter[0])
                                  && (i == 1 || line[i - 1] != '"' || (i > 2 && line[i - 1] == '"' && line[i - 2] == '"'))
                                  && (i == line.Length - 1 || line[i + 1] != '"')
                                )
                            {
                                ind = i;
                                break;
                            }

                    if (ind > 0)
                    {
                        var res = line.Substring(1, ind - 1).Replace("\"\"", "\"");
                        line = line.Remove(0, ind + 1);
                        line = (line == string.Empty) ? null : line.Remove(0, 1);
                        result.Add(res);
                    }
                    else
                    {
                        var res = ReadUnquotedField(ref line, delimiter);
                        if (res != null)
                            result.Add(res);
                        //throw new ApplicationException("Неверный формат входной строки");
                    }
                }
                else
                {
                    var res = ReadUnquotedField(ref line, delimiter);
                    if (res != null)
                        result.Add(res);
                }

                if (line == null)
                    break;
            }

            return result.ToArray();
        }

        private static string ReadUnquotedField(ref string line, string delimiter)
        {
            string result = null;
            if (line != null)
            {
                var ind = line.IndexOf(delimiter);
                if (ind > 0)
                {
                    result = line.Substring(0, ind);
                    line = line.Remove(0, ind + 1);
                }
                else if (ind == 0)
                {
                    line = line.Remove(0, ind + 1);
                    result = string.Empty;
                }
                else
                {
                    result = line;
                    line = null;
                }

                //if (ind < 0 && line == string.Empty && result == string.Empty)
                //    result = null;
            }
            return result;
        }

        public static CSVFile Load(
            string[] lines,
            string tableName,
            string filePath,
            bool hasColumns = true,
            string delimiter = ";",
            Action<string> verboseLogAction = null,
            Func<string, string> columnRenamer = null,
            Action<DataTable> tableValidator = null,
            Action<DataRow> rowValidator = null)
        {
            var log = new Action<string>((s) => { if (verboseLogAction != null) verboseLogAction(s); });
            columnRenamer = columnRenamer ?? new Func<string, string>((s) => s);
            tableValidator = tableValidator ?? new Action<DataTable>((table) => { });
            rowValidator = rowValidator ?? new Action<DataRow>((row) => { });
            log("start load");

            log(string.Format("total lines readed from file: '{0}'", lines.Length));

            var res = new CSVFile()
            {
                Table = new DataTable(tableName),
                FilePath = filePath,
                ProcessedRowCount = 0,
                TotalRowCount = lines.Length
            };

            try
            {
                foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    bool needProceedRow = true;
                    var fields = GetCsvFields(line, delimiter).Select(f => ClearField(f)).ToArray();

                    if (res.Table.Columns.Count == 0)
                    {
                        if (hasColumns)
                        {
                            log(string.Format("read columns"));
                            res.Table.Columns.AddRange(
                                Enumerable.Range(0, fields.Length)
                                    .Select(i => new { ColumnName = fields[i], Index = i })                                
                                    .Select(c => new { ColumnName = columnRenamer(c.ColumnName.ToLower().Trim()), c.Index })
                                    .Select(c => new { ColumnName = string.IsNullOrWhiteSpace(c.ColumnName) ? "column" : c.ColumnName, c.Index })
                                    .GroupBy(c => c.ColumnName)
                                    .SelectMany(g => g.Select(i => new { ColumnName = i.ColumnName + ( g.Count() == 1 ? string.Empty : "_" + i.Index.ToString() ), Index = i.Index } ) )
                                    .OrderBy(i => i.Index)
                                    .Select(c => c.ColumnName)
                                    .Select(c => new DataColumn(c, typeof(string)))
                                    .ToArray()
                                );
                            needProceedRow = false;
                        }
                        else
                        {
                            for (int i = 0; i < fields.Length; i++)
                                res.Table.Columns.Add(string.Format("column_{0}", i), typeof(string));

                        }
                        log(string.Format("read columns done. columns count: '{0}'", res.Table.Columns.Count));
                        log("validate table");
                        tableValidator(res.Table);
                        log("table validation done");
                    }

                    if (needProceedRow)
                    {
                        var row = res.Table.NewRow();
                        for (int i = 0; i < Math.Min(fields.Length, res.Table.Columns.Count); i++)
                            row[res.Table.Columns[i]] = fields[i];
                        rowValidator(row);
                        res.Table.Rows.Add(row);
                    }
                    res.ProcessedRowCount++;
                }
            }
            catch (Exception ex)
            {
                var e = new Exception("Data read exception. See inner exception for details", ex);
                e.Data.Add("Exception thrown at line number", res.ProcessedRowCount);
                e.Data.Add("Exception thrown at line", lines.ElementAt(res.ProcessedRowCount));
                throw e;
            }
            finally
            {
                log(string.Format("import end. Imported '{1}' from '{0}' rows.", res.Table.Rows.Count, res.TotalRowCount));
            }
            return res;
        }

        public static CSVFile Load(
            string filePath,
            Encoding fileEncoding = null,
            string tableName = null,
            bool hasColumns = true,
            string delimiter = ";",
            Action<string> verboseLogAction = null, 
            Func<string,string> columnRenamer = null,
            Action<DataTable> tableValidator = null,
            Action<DataRow> rowValidator = null)
        {
            var log = new Action<string>((s) => { if (verboseLogAction != null) verboseLogAction(s); });

            if (!File.Exists(filePath))
                throw new Exception(string.Format("File '{0}' not exists", filePath));

            log(string.Format("file '{0}' exists", filePath));

            var lines = File.ReadAllLines(filePath, fileEncoding ?? Encoding.Default);

            log(string.Format("total lines readed from file: '{0}'", lines.Length));

            return Load(lines: lines, tableName: tableName ?? Path.GetFileName(filePath), filePath: filePath, verboseLogAction: log, hasColumns: hasColumns, delimiter: delimiter, columnRenamer: columnRenamer, tableValidator: tableValidator, rowValidator: rowValidator);
        }

        public static IEnumerable<string> Save(DataTable table, bool hasColumns = true, string delimiter = ";", Action<string> verboseLogAction = null, Func < string, string> columnRenamer = null, Func < DataColumn, bool> excludeColumn = null)
        {
            verboseLogAction = verboseLogAction ?? new Action<string>(s => { });
            excludeColumn = excludeColumn ?? new Func<DataColumn, bool>(c => false);
            columnRenamer = columnRenamer ?? new Func<string, string>(c => c);

            verboseLogAction("start exporting");

            var lines = new List<string>();
            var wrapParam = new string[] { delimiter, "\"" };

            verboseLogAction("compute exported columns");

            var columns = table.Columns
                .OfType<DataColumn>()
                .Select(c => new
                {
                    Column = c,
                    ColumnName = WrapInQuotesIfContains(columnRenamer(c.ColumnName), wrapParam),
                    Exclude = excludeColumn(c)
                })
                .Where(c => !c.Exclude)
                .ToArray();

            verboseLogAction(string.Format("compute exported columns done. Columns count: '{0}'", columns.Length));

            if (columns.Length > 0)
            {
                if (hasColumns)
                {
                    verboseLogAction("add columns line");
                    var line = string.Empty;
                    foreach (var column_name in columns.Select(c => c.ColumnName))
                        line += (string.IsNullOrEmpty(line) ? string.Empty : delimiter) + column_name;
                    lines.Add(line);
                }
                else
                    verboseLogAction("no column line");
                verboseLogAction("add data lines");
                lines.AddRange(
                    table.Rows
                        .OfType<DataRow>()
                        .Select(r =>
                        {
                            var line = string.Empty;
                            foreach (var c in columns)
                                line += (string.IsNullOrEmpty(line) ? string.Empty : delimiter) + WrapInQuotesIfContains(r[c.Column].ToString(), wrapParam);
                            return line;
                        })
                        .ToArray()
                    );
            }
            else
                verboseLogAction("no one column found for export. Export stoped.");
            return lines;
        }

        public static CSVFile Save(DataTable table, string filePath, Encoding encoding = null, bool hasColumns = true, string delimiter = ";", Action<string> verboseLogAction = null, Func < string, string> columnRenamer = null, Func<DataColumn, bool> excludeColumn = null)
        {
            verboseLogAction = verboseLogAction ?? new Action<string>(s => { });
            encoding = encoding ?? Encoding.Default;

            verboseLogAction("get lines for export files...");

            var lines = Save(table: table, hasColumns: hasColumns, delimiter: delimiter, verboseLogAction: verboseLogAction, columnRenamer: columnRenamer, excludeColumn: excludeColumn);

            verboseLogAction("get lines done");

            var res = new CSVFile()
            {
                FilePath = filePath,
                Table = table,
                TotalRowCount = table.Rows.Count,
                ProcessedRowCount = lines.Count() + (hasColumns ? 1 : 0)
            };

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
                try
                {
                    WritePreamble(fs, encoding);
                    foreach (var line in lines)
                        AddLineToStream(fs, encoding, line);
                }
                finally
                {
                    fs.Flush();
                }

            return res;
        }

        private static void WritePreamble(Stream stream, Encoding encoding)
        {
            byte[] preamble = encoding.GetPreamble();
            if (preamble.Length > 0)
                stream.Write(preamble, 0, preamble.Length);
        }
        private static string WrapInQuotesIfContains(string inString, string[] inSearchString)
        {
            return (inSearchString.Any(s => inString.Contains(s)) ? "\"" + inString.Replace("\"", "\"\"") + "\"" : inString);
        }
        private static void AddLineToStream(Stream stream, Encoding encoding, string line)
        {
            byte[] string_arr = encoding.GetBytes(line + Environment.NewLine);
            stream.Write(string_arr, 0, string_arr.Length);
        }

        public static DataTable MergeTables(IEnumerable<DataTable> dataTables, string[] columnsId)
        {
            columnsId = columnsId ?? new string[] { };
            var resultCSV = new DataTable();

            var columnNames = dataTables
                .SelectMany(dt => dt.Columns.OfType<DataColumn>().Select(c => new { c.ColumnName, c.DataType }))
                .Distinct()
                .GroupBy(c => c.ColumnName)
                .Select(g => new { g.FirstOrDefault().ColumnName, ColumnType = g.Count() > 1 ? typeof(string) : g.FirstOrDefault().DataType })
                .ToArray();
            resultCSV.Columns.AddRange(columnNames.Select(cn => new DataColumn(cn.ColumnName, cn.ColumnType)).ToArray());

            if (columnsId.Length > 0)
                resultCSV.PrimaryKey = resultCSV.Columns.OfType<DataColumn>().Where(c => columnsId.Contains(c.ColumnName)).ToArray();

            foreach (var dt in dataTables)
                resultCSV.Merge(dt);

            resultCSV.AcceptChanges();

            //foreach (DataTable tbl in dataTables)
            //    foreach (DataRow dr in tbl.Rows)
            //    {
            //        string select = string.Empty;
            //        foreach (string colId in columnsId)
            //            select += (select.Length == 0 ? "" : " AND ") + string.Format("{0} = '{1}'", colId, dr[colId].ToString());

            //        DataRow[] drs_for_delete = resultCSV.Select(select);

            //        DataRow newRow = null;
            //        if (drs_for_delete.Length == 1)
            //        {
            //            newRow = drs_for_delete[0];
            //        }
            //        else
            //        {
            //            foreach (DataRow dr_for_delete in drs_for_delete)
            //                resultCSV.Rows.Remove(dr_for_delete);
            //            newRow = resultCSV.NewRow();
            //        }

            //        try
            //        {
            //            foreach (DataColumn col in tbl.Columns)
            //                newRow[col.ColumnName] = dr[col.ColumnName];
            //        }
            //        finally
            //        {
            //            if (newRow.RowState == DataRowState.Detached)
            //                resultCSV.Rows.Add(newRow);
            //        }
            //    }
            
            return resultCSV;
        }
    }
}
