﻿using DddDotNet.CrossCuttingConcerns.Pdf;
using DddDotNet.Domain.Entities;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.PdfConverters.DinkToPdf;

public class ConfigurationEntryPdfWriter : IPdfWriter<ConfigurationEntry>
{
    private readonly IConverter _converter;

    public ConfigurationEntryPdfWriter(IConverter converter)
    {
        _converter = converter;
    }

    public Task WriteAsync(ConfigurationEntry data, Stream stream)
    {
        var html = "";

        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings =
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings() { Top = 10, Bottom = 15, Left = 10, Right = 10 },
            },
            Objects =
            {
                new ObjectSettings()
                {
                    PagesCount = true,
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8", Background = true },
                    HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812 },
                },
            },
        };

        byte[] pdf = _converter.Convert(doc);

        return Task.CompletedTask;
    }
}
