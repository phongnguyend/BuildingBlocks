using DddDotNet.CrossCuttingConcerns.Excel;
using DddDotNet.Domain.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Excel.OpenXml;

public class ConfigurationEntryExcelWriter : IExcelWriter<List<ConfigurationEntry>>
{
    public Task WriteAsync(List<ConfigurationEntry> data, Stream stream)
    {
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            // Create workbook and worksheet
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            {
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };

            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            // Add header row
            Row headerRow = new Row();
            headerRow.Append(
                CreateTextCell("Key"),
                CreateTextCell("Value")
            );

            sheetData.Append(headerRow);

            // Add data rows
            foreach (var record in data)
            {
                Row row = new Row();
                row.Append(
                    CreateTextCell(record.Key),
                    CreateTextCell(record.Value)
                );

                sheetData.Append(row);
            }

            workbookPart.Workbook.Save();
        }

        return Task.CompletedTask;
    }

    private static Cell CreateTextCell(string text)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(text)
        };
    }
}
