using DddDotNet.Infrastructure.Excel.ExcelDataReader;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Excel.ExcelDataReader;

public class ConfigurationEntryExcelTests
{
    [Fact]
    public async Task Read_ReturnData()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var reader = new ConfigurationEntryExcelReader();

        using var fileStream = File.OpenRead("Excel/ConfigurationEntries.xlsx");
        var entries = await reader.ReadAsync(fileStream);

        Assert.Equal(8, entries.Count);
        Assert.Equal("Key1", entries[0].Key);
        Assert.Equal("Value 1", entries[0].Value);
        Assert.Equal("Key8", entries[7].Key);
        Assert.Equal("Value 8", entries[7].Value);
    }
}
