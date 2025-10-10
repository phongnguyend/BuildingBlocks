using DddDotNet.Domain.Entities;
using DddDotNet.Infrastructure.Csv;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Csv;

public class ConfigurationEntryCsvTests
{
    [Fact]
    public async Task Read_ReturnData()
    {
        var reader = new ConfigurationEntryCsvReader();

        using var fileStream = File.OpenRead("Csv/ConfigurationEntries.csv");
        var entries = (await reader.ReadAsync(fileStream)).ToList();

        Assert.Equal(8, entries.Count);
        Assert.Equal("Key1", entries[0].Key);
        Assert.Equal("Value 1", entries[0].Value);
        Assert.Equal("Key8", entries[7].Key);
        Assert.Equal("Value 8", entries[7].Value);
    }

    [Fact]
    public async Task Write_Ok()
    {
        var writer = new ConfigurationEntryCsvWriter();

        using (var fileStream = File.OpenWrite("Csv/ConfigurationEntries1.csv"))
        {
            await writer.WriteAsync(new List<ConfigurationEntry> {
                new ConfigurationEntry
                {
                    Key = "Key1",
                    Value = "Value 1",
                },
                new ConfigurationEntry
                {
                    Key = "Key2",
                    Value = "Value 2",
                },
                new ConfigurationEntry
                {
                    Key = "Key5",
                    Value = "Value 5",
                },
            }, fileStream);
        }

        var reader = new ConfigurationEntryCsvReader();

        using var fileStream2 = File.OpenRead("Csv/ConfigurationEntries1.csv");
        var entries = (await reader.ReadAsync(fileStream2)).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal("Key1", entries[0].Key);
        Assert.Equal("Value 1", entries[0].Value);
        Assert.Equal("Key5", entries[2].Key);
        Assert.Equal("Value 5", entries[2].Value);
    }
}
