using ClosedXML.Excel;

namespace C2E4XML
{
    internal class ExcelExporter
    {
        public static void Export(string filePath, Dictionary<string, List<Dictionary<string, string>>> tables)
        {
            using var workbook = new XLWorkbook();

            // 1. シート名（ルート直下タグ）ごとにグルーピング
            var groups = tables.GroupBy(kv => GetSheetKey(kv.Key));

            foreach (var group in groups)
            {
                string sheetName = SanitizeSheetName(group.Key);
                var sheet = workbook.Worksheets.Add(sheetName);

                int currentRow = 1;

                // 2. 同じシートに属する表を順に書き込む
                foreach (var kv in group.OrderBy(g => g.Key))
                {
                    string fullPath = kv.Key;
                    var rows = kv.Value;

                    // 表タイトル行（フルパス）
                    sheet.Cell(currentRow, 1).Value = fullPath;

                    // ★ 追加：表タイトル行の背景色（ヘッダーより濃いグレー）
                    var titleRange = sheet.Range(currentRow, 1, currentRow, 1);
                    titleRange.Style.Fill.BackgroundColor = XLColor.Gray;   // ← 濃いグレー

                    currentRow++;

                    if (rows.Count > 0)
                    {
                        var header = rows[0].Keys.ToList();

                        // ヘッダ行
                        for (int col = 0; col < header.Count; col++)
                            sheet.Cell(currentRow, col + 1).Value = header[col];

                        // ★ 追加：ヘッダー行のスタイル（背景グレー＋太字）
                        var headerRange = sheet.Range(currentRow, 1, currentRow, header.Count);
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                        currentRow++;

                        // データ行
                        int dataStartRow = currentRow; // データ開始行を記録

                        for (int r = 0; r < rows.Count; r++)
                        {
                            var data = rows[r];
                            for (int c = 0; c < header.Count; c++)
                            {
                                string key = header[c];
                                data.TryGetValue(key, out string? value);
                                sheet.Cell(currentRow, c + 1).Value = value ?? "";
                            }
                            currentRow++;
                        }

                        // ★ 追加：データ行を太字にする
                        if (currentRow > dataStartRow)
                        {
                            var dataRange = sheet.Range(dataStartRow, 1, currentRow - 1, header.Count);
                            dataRange.Style.Font.Bold = true;
                        }
                    }

                    // 表と表の間に 1 行空ける
                    currentRow++;
                }

                sheet.Columns().AdjustToContents();
            }

            workbook.SaveAs(filePath);
        }

        // ルート直下のタグ名をシート名にする
        private static string GetSheetKey(string path)
        {
            var parts = path.Split('.');

            // 例:
            // "config"                               → "config"
            // "config.devices.entry.network"         → "devices"
            // "config.shared.botnet.configuration"   → "shared"
            // "config.mgt-config.users.entry"        → "mgt-config"

            if (parts.Length == 1)
                return parts[0];

            return parts[0];
        }

        private static string SanitizeSheetName(string name)
        {
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var c in invalid)
                name = name.Replace(c.ToString(), "");

            if (name.Length > 31)
                name = name[..31];

            return name;
        }

        public static void Export(string filePath, XmlDataNode data)
        {
            _ = new XmlFlattener();
            var rows = XmlFlattener.Flatten(data).ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("XML");

            WriteHeader(sheet, rows);
            WriteBody(sheet, rows);

            workbook.SaveAs(filePath);
        }

        private static void WriteHeader(IXLWorksheet sheet, IEnumerable<FlattenedRow> rows)
        {
            var maxDepth = rows.Max(r => r.Path.Count);

            for (int i = 0; i <= maxDepth; i++)
            {
                sheet.Cell(1, i + 1).Value = $"Level{i + 1}";
                // 背景色（薄いグレー／濃いグレー）
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor =
                    (i % 2 == 0) ? XLColor.Gray : XLColor.DarkGray;
            }
        }

        private static void WriteBody(IXLWorksheet sheet, IEnumerable<FlattenedRow> rows)
        {
            int rowIndex = 2;

            foreach (var row in rows)
            {
                for (int i = 0; i < row.Path.Count; i++)
                {
                    sheet.Cell(rowIndex, i + 1).Value = row.Path[i];
                }

                sheet.Cell(rowIndex, row.Path.Count + 1).Value = row.Value;
                sheet.Cell(rowIndex, row.Path.Count + 1).Style.Font.Bold = true;
                sheet.Cell(rowIndex, row.Path.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                rowIndex++;
            }
        }
    }
}