using ClosedXML.Excel;

namespace C2E4XML
{
    internal class ExcelExporter
    {
        /// <summary>
        /// XmlTableBuilder が生成した
        /// 「シート名 → (タイトル, 表データ) のリスト」
        /// を Excel に書き込む。
        /// </summary>
        public void Export(
            string filePath,
            Dictionary<string, List<(string Title, List<Dictionary<string, string>> Table)>> sheets)
        {
            using var wb = new XLWorkbook();

            foreach (var (sheetName, tableList) in sheets)
            {
                var ws = wb.Worksheets.Add(sheetName);

                int rowIndex = 1;

                foreach (var (title, table) in tableList)
                {
                    if (table.Count == 0)
                    {
                        rowIndex += 2;
                        continue;
                    }

                    // --- タイトル行（タグパス） ---
                    ws.Cell(rowIndex, 1).Value = title;
                    rowIndex++;

                    // --- ヘッダー行 ---
                    var columns = table
                        .SelectMany(r => r.Keys)
                        .Distinct()
                        .ToList();

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

                    // --- 表間に 2 行空ける ---
                    rowIndex += 2;
                }
            }

            wb.SaveAs(filePath);
        }
    }
}
