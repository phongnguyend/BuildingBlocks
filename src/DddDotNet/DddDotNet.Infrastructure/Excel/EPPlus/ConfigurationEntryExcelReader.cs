﻿using DddDotNet.CrossCuttingConcerns.Excel;
using DddDotNet.CrossCuttingConcerns.Exceptions;
using DddDotNet.Domain.Entities;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Excel.EPPlus;

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
        using var pck = new ExcelPackage(stream);
        var worksheet = pck.Workbook.Worksheets.First();

        string result = worksheet.VerifyHeader(1, GetCorrectHeaders());
        if (!string.IsNullOrEmpty(result))
        {
            throw new ValidationException(result);
        }

        var rows = new List<ConfigurationEntry>();

        for (var i = 2; i <= worksheet.Dimension.End.Row; i++)
        {
            var row = new ConfigurationEntry
            {
                Key = worksheet.GetCellValue("A", i),
                Value = worksheet.GetCellValue("B", i),
            };

            rows.Add(row);
        }

        return Task.FromResult(rows);
    }
}
