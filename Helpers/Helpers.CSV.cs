using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Helpers.CSV
{
    /// <summary>
    /// Helper for work with CSV file
    /// </summary>
    public class CSVFile : IDisposable
    {
        #region Result fields
        /// <summary>
        /// Result table
        /// </summary>
        public DataTable Table { get; private set; }
        /// <summary>
        /// Total row count
        /// </summary>
        public int TotalRowCount { get; private set; }
        /// <summary>
        /// Processed row count
        /// </summary>
        public int ProcessedRowCount { get; private set; }
        /// <summary>
        /// File path
        /// </summary>
        public string FilePath { get; private set; }

        #endregion

        private CSVFile() { }
        /// <summary>
        /// Load CSV table from lines
        /// </summary>
        /// <param name="lines">CSV file lines</param>
        /// <param name="tableName">Result table name</param>
        /// <param name="filePath">Result file path</param>
        /// <param name="hasColumns">Is first row is column row</param>
        /// <param name="delimiter">Separator between data</param>
        /// <param name="verboseLogAction">Action to verbose load action</param>
        /// <param name="columnRenamer">Action to rename columns</param>
        /// <param name="tableValidator">Validate table before load</param>
        /// <param name="rowFilter">Validate each row when load it</param>
        /// <returns>CSV file load info</returns>
        public static CSVFile Load(
            IEnumerable<string> lines,
            string tableName = "{virtual}",
            string filePath = "{virtual}",
            bool hasColumns = true,
            string delimiter = ";",
            Action<string> verboseLogAction = null,
            Func<string, string> columnRenamer = null,
            Action<DataTable> tableValidator = null,
            Expression<Func<DataRow, bool>> rowFilter = null)
        {
            if (lines == null)
                throw new ArgumentNullException("lines");

            if (delimiter == null)
                throw new ArgumentNullException("delimiter");

            if (string.IsNullOrWhiteSpace(delimiter))
                throw new ArgumentException("delimiter");

            verboseLogAction = verboseLogAction ?? new Action<string>((s) => { });
            columnRenamer = columnRenamer ?? new Func<string, string>((s) => s);
            tableValidator = tableValidator ?? new Action<DataTable>((table) => { });

            Expression<Func<DataRow, bool>> defFilter = d => false;
            rowFilter = rowFilter ?? defFilter;

            verboseLogAction(string.Format("start load. Total lines in lines array: '{0}'", lines.Count()));

            var res = new CSVFile()
            {
                Table = new DataTable(tableName ?? string.Empty),
                FilePath = filePath ?? string.Empty,
                ProcessedRowCount = 0,
                TotalRowCount = lines.Count()
            };

            var linesArr = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            try
            {
                var rows = Enumerable.Range(0, linesArr.Length)
                    .AsParallel()
                    .Select(i => new { Index = i, Line = linesArr[i] })
                    .Select(i => new { i.Index, Fields = GetCsvFields(i.Line, delimiter).ToArray() })
                    .OrderBy(i => i.Index)
                    .ToArray();

                var firstRow = rows.FirstOrDefault();
                if (firstRow != null)
                {
                    #region Columns
                    if (hasColumns)
                    {
                        verboseLogAction(string.Format("read columns"));
                        res.Table.Columns.AddRange(
                            Enumerable.Range(0, firstRow.Fields.Length)
                                .Select(i => new { ColumnName = firstRow.Fields[i], Index = i })
                                .Select(c => new { ColumnName = columnRenamer(c.ColumnName.ToLower().Trim()), c.Index })
                                .Select(c => new { ColumnName = string.IsNullOrWhiteSpace(c.ColumnName) ? "column" : c.ColumnName, c.Index })
                                .GroupBy(c => c.ColumnName)
                                .SelectMany(g => g.Select(i => new { ColumnName = i.ColumnName + (g.Count() == 1 ? string.Empty : "_" + i.Index.ToString()), Index = i.Index }))
                                .OrderBy(i => i.Index)
                                .Select(c => c.ColumnName)
                                .Select(c => new DataColumn(c, typeof(string)))
                                .ToArray()
                            );
                    }
                    else
                    {
                        verboseLogAction(string.Format("generate columns"));
                        for (int i = 0; i < firstRow.Fields.Length; i++)
                            res.Table.Columns.Add(string.Format("column_{0}", i), typeof(string));
                    }
                    verboseLogAction(string.Format("read columns done. columns count: '{0}'", res.Table.Columns.Count));
                    verboseLogAction("validate table");
                    tableValidator(res.Table);
                    verboseLogAction("table validation done");
                    #endregion

                    var tableColumns = res.Table
                        .Columns
                        .OfType<DataColumn>()
                        .Select(c => c.ColumnName)
                        .ToArray();

                    var exprFilter = rowFilter.Compile();

                    var dataRows = rows
                        .Skip(hasColumns ? 1 : 0)
                        .Select(i => new { i.Fields, i.Index, Row = res.Table.NewRow() })
                        .AsParallel()
                        .Select(item =>
                            {
                                var minLength = Math.Min(item.Fields.Length, tableColumns.Length);
                                for (int n = 0; n < minLength; n++)
                                    item.Row[n] = item.Fields[n];
                                return new { item.Index, item.Row };
                            })
                        .OrderBy(r => r.Index)
                        .ToArray()
                        .Select(i => new
                        {
                            i.Index,
                            i.Row,
                            IsFiltered = exprFilter(i.Row)
                        })
                        .ToArray();

                    foreach (var dr in dataRows.Where(r => r.IsFiltered))
                        verboseLogAction(string.Format("column validation error on index: {0}, row: '{1}'", dr.Index, lines.ElementAt(dr.Index)));

                    var validRows = dataRows.Where(r => !r.IsFiltered);
                    foreach (var dr in validRows)
                        res.Table.Rows.Add(dr.Row);

                    res.ProcessedRowCount = validRows.Count() + (hasColumns ? 1 : 0);
                }
                else
                    throw new Exception(Resource.Helpers_CSV_Load_NoOneRowFound);
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
                verboseLogAction(string.Format("import end. Imported '{0}' from '{1}' rows.", res.Table.Rows.Count, res.TotalRowCount));
            }
            return res;
        }

        /// <summary>
        /// Load CSV table from file
        /// </summary>
        /// <param name="filePath">File path to load file</param>
        /// <param name="fileEncoding">File encoding</param>
        /// <param name="tableName">Result table name</param>
        /// <param name="hasColumns">Is first row is column row</param>
        /// <param name="delimiter">Separator between data</param>
        /// <param name="verboseLogAction">Action to verbose load action</param>
        /// <param name="columnRenamer">Action to rename columns</param>
        /// <param name="tableValidator">Validate table before load</param>
        /// <param name="rowFilter">Filter each row when load it (true for add row in data table)</param>
        /// <returns>CSV file load info</returns>
        public static CSVFile Load(
            string filePath,
            Encoding fileEncoding = null,
            string tableName = null,
            bool hasColumns = true,
            string delimiter = ";",
            Action<string> verboseLogAction = null, 
            Func<string,string> columnRenamer = null,
            Action<DataTable> tableValidator = null,
            Expression<Func<DataRow, bool>> rowFilter = null)
        {
            verboseLogAction = verboseLogAction ?? new Action<string>((s) => { });

            if (!File.Exists(filePath))
                throw new Exception(string.Format("File '{0}' not exists", filePath));

            verboseLogAction(string.Format("file '{0}' exists", filePath));

            var lines = File.ReadAllLines(filePath, fileEncoding ?? Encoding.Default);

            verboseLogAction(string.Format("total lines readed from file: '{0}'", lines.Length));

            return Load(lines: lines, tableName: tableName ?? Path.GetFileName(filePath), filePath: filePath, verboseLogAction: (s) => { verboseLogAction(string.Format("load from lines: {0}", s)); }, hasColumns: hasColumns, delimiter: delimiter, columnRenamer: columnRenamer, tableValidator: tableValidator, rowFilter: rowFilter);
        }

        /// <summary>
        /// Get lines for CSV file from DataTable
        /// </summary>
        /// <param name="table">Table with data for CSV file</param>
        /// <param name="hasColumns">Write column line</param>
        /// <param name="delimiter">Separator between data</param>
        /// <param name="verboseLogAction">Action for verbose saving</param>
        /// <param name="columnRenamer">Function for rename columns before save</param>
        /// <param name="excludeColumn">Function for exclude columns</param>
        /// <returns>CSV file lines</returns>
        public static IEnumerable<string> Save(
            DataTable table, 
            bool hasColumns = true, 
            string delimiter = ";", 
            Action<string> verboseLogAction = null, 
            Func<string, string> columnRenamer = null, 
            Func<DataColumn, bool> excludeColumn = null)
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

        /// <summary>
        /// Save DataTable to CSV file
        /// </summary>
        /// <param name="table">Table with data for CSV file</param>
        /// <param name="filePath">File path to save file</param>
        /// <param name="encoding">File encoding</param>
        /// <param name="hasColumns">Write column line</param>
        /// <param name="delimiter">Separator between data</param>
        /// <param name="verboseLogAction">Action for verbose saving</param>
        /// <param name="columnRenamer">Function for rename columns before save</param>
        /// <param name="excludeColumn">Function for exclude columns</param>
        /// <returns></returns>
        public static CSVFile Save(
            DataTable table, 
            string filePath, 
            Encoding encoding = null, 
            bool hasColumns = true, 
            string delimiter = ";", 
            Action<string> verboseLogAction = null, 
            Func<string, string> columnRenamer = null, 
            Func<DataColumn, bool> excludeColumn = null)
        {
            verboseLogAction = verboseLogAction ?? new Action<string>(s => { });
            encoding = encoding ?? Encoding.Default;

            verboseLogAction("get lines for export files...");

            var lines = Save(table: table, hasColumns: hasColumns, delimiter: delimiter, verboseLogAction: (s) => { verboseLogAction(string.Format("save to lines: {0}", s)); }, columnRenamer: columnRenamer, excludeColumn: excludeColumn);

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

        #region Additional private
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
        private static IEnumerable<string> GetCsvFields(string line, string delimiter)
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

            return result.Select(s => ClearField(s));
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
        #endregion

        /// <summary>
        /// Merge tables to one result table
        /// </summary>
        /// <param name="dataTables">Tables to merge</param>
        /// <param name="columnsId">Identifiers column names</param>
        /// <returns>Merged data table</returns>
        public static DataTable MergeTables(IEnumerable<DataTable> dataTables, IEnumerable<string> columnsId)
        {
            if (dataTables == null)
                throw new ArgumentNullException("dataTables");

            columnsId = columnsId ?? new string[] { };
            var result = new DataTable();

            var columnNames = dataTables
                .SelectMany(dt => dt.Columns.OfType<DataColumn>().Select(c => new { c.ColumnName, c.DataType }))
                .Distinct()
                .GroupBy(c => c.ColumnName)
                .Select(g => new { g.FirstOrDefault().ColumnName, ColumnType = g.Count() > 1 ? typeof(string) : g.FirstOrDefault().DataType })
                .ToArray();
            result.Columns.AddRange(columnNames.Select(cn => new DataColumn(cn.ColumnName, cn.ColumnType)).ToArray());

            if (columnsId.Count() > 0)
                result.PrimaryKey = result.Columns.OfType<DataColumn>().Where(c => columnsId.Contains(c.ColumnName)).ToArray();

            foreach (var dt in dataTables)
                result.Merge(dt);

            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.Table != null)
                    {
                        Table.Dispose();
                        Table = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
