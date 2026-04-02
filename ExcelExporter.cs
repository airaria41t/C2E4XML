using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ClosedXML.Excel;

namespace C2E4XML
{
    internal class ExcelExporter(XmlDataNode data)
    {
        XmlDataNode Data { get; set; } = data;

        public void ExportItemsToExcel(string excelPath, IEnumerable<dynamic> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Items");

            // ヘッダー行
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Value";

            int row = 2;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.Id;
                ws.Cell(row, 2).Value = item.Name;
                ws.Cell(row, 3).Value = item.Value;
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(excelPath);
        }
    }
}
