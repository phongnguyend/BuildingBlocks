using DddDotNet.CrossCuttingConcerns.Excel;
using DddDotNet.CrossCuttingConcerns.Exceptions;
using DddDotNet.Domain.Entities;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Excel.OpenXml;

public class ConfigurationEntryExcelReader : IExcelReader<List<ConfigurationEntry>>
{
    private static Dictionary<string, string> GetCorrectHeaders()
    {
        return new Dictionary<string, string>
        {
            { "A", "Key" },
            { "B", "Value" },
        };
    }

    public Task<List<ConfigurationEntry>> ReadAsync(Stream stream)
    {
        var rows = new List<ConfigurationEntry>();

        using SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
        SharedStringTable sharedStringTable = spreadsheetDocument.WorkbookPart?.SharedStringTablePart?.SharedStringTable;

        WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;

        foreach (Sheet sheet in workbookPart.Workbook.Sheets)
        {
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

            int i = 1;

            foreach (var r in sheetData.Elements<Row>())
            {
                var cells = r.Elements<Cell>().ToList();

                if (i == 1)
                {
                    var header = new ConfigurationEntry
                    {
                        Key = GetText(cells[0], spreadsheetDocument),
                        Value = GetText(cells[1], spreadsheetDocument),
                    };

                    if (header.Key != "Key")
                    {
                        throw new ValidationException($"Wrong Template! The expected value of cell [A1] is: Key but the actual value is: {header.Key}");
                    }

                    if (header.Value != "Value")
                    {
                        throw new ValidationException($"Wrong Template! The expected value of cell [B1] is: Key but the actual value is: {header.Value}");
                    }

                    i++;
                    continue;
                }

                var row = new ConfigurationEntry
                {
                    Key = GetText(cells[0], spreadsheetDocument),
                    Value = GetText(cells[1], spreadsheetDocument),
                };

                rows.Add(row);

                i++;
            }
        }

        return Task.FromResult(rows);
    }

    public static string GetText(Cell cell, SpreadsheetDocument document)
    {
        if (cell == null)
        {
            return null;
        }

        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            SharedStringTable sharedStringTable = document.WorkbookPart.SharedStringTablePart.SharedStringTable;
            return sharedStringTable.ElementAt(int.Parse(cell.CellValue?.Text)).InnerText;
        }
        else
        {
            return cell.CellValue?.Text;
        }
    }
}
