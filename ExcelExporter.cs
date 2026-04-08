using ClosedXML.Excel;

internal class ExcelExporter
{
    private readonly Dictionary<string, List<Dictionary<string, string>>> _tables;

    public ExcelExporter(Dictionary<string, List<Dictionary<string, string>>> tables)
    {
        _tables = tables;
    }

    public void Export(string filePath)
    {
        using var workbook = new XLWorkbook();

        // 1. シート名（ルート直下タグ）ごとにグルーピング
        var groups = _tables.GroupBy(kv => GetSheetKey(kv.Key));

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
                currentRow++;

                if (rows.Count > 0)
                {
                    var header = rows[0].Keys.ToList();

                    // ヘッダ行
                    for (int col = 0; col < header.Count; col++)
                        sheet.Cell(currentRow, col + 1).Value = header[col];

                    currentRow++;

                    // データ行
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
}
