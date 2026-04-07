using ClosedXML.Excel;

namespace C2E4XML
{

    /// <summary>
    /// 「シート名 → 複数の表」を Excel に書き込む。
    /// 1 シートに複数の表を縦に並べて出力する。
    /// </summary>
    internal class ExcelExporter
    {
        public void Export(string filePath,
            Dictionary<string, List<List<Dictionary<string, string>>>> sheets)
        {
            using var wb = new XLWorkbook();

            foreach (var (sheetName, tables) in sheets)
            {
                var ws = wb.Worksheets.Add(sheetName);

                int rowIndex = 1;

                foreach (var table in tables)
                {
                    if (table.Count == 0)
                    {
                        rowIndex++;
                        continue;
                    }

                    // 列名（キー）を収集
                    var columns = table
                        .SelectMany(r => r.Keys)
                        .Distinct()
                        .ToList();

                    // --- ヘッダー行 ---
                    for (int c = 0; c < columns.Count; c++)
                        ws.Cell(rowIndex, c + 1).Value = columns[c];

                    rowIndex++;

                    // --- データ行 ---
                    foreach (var row in table)
                    {
                        for (int c = 0; c < columns.Count; c++)
                        {
                            string col = columns[c];
                            row.TryGetValue(col, out string? value);
                            ws.Cell(rowIndex, c + 1).Value = value ?? "";
                        }
                        rowIndex++;
                    }

                    // 表間に 2 行空ける
                    rowIndex += 2;
                }
            }

            wb.SaveAs(filePath);
        }
    }
}
