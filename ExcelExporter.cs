using ClosedXML.Excel;

namespace C2E4XML
{
    internal class ExcelExporter(XmlDataNode data)
    {
        XmlDataNode Data { get; set; } = data;

        public void Export(string filePath)
        {
            var flattener = new XmlFlattener();
            var rows = flattener.Flatten(Data).ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("XML");

            WriteHeader(sheet, rows);
            WriteBody(sheet, rows);

            workbook.SaveAs(filePath);
        }

        private static void WriteHeader(IXLWorksheet sheet, IEnumerable<FlattenedRow> rows)
        {
            var maxDepth = rows.Max(r => r.Path.Count);

            for (int i = 0; i < maxDepth; i++)
            {
                sheet.Cell(1, i + 1).Value = $"Level{i + 1}";
            }

            sheet.Cell(1, maxDepth + 1).Value = "Value";
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
                rowIndex++;
            }
        }
    }
}
