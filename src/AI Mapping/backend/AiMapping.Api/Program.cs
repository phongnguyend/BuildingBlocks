using AiMapping.Api.Models;
using AiMapping.Api.Options;
using AiMapping.Api.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection(ImportOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = builder.Configuration.GetValue<long?>("Import:MaxFileSizeBytes") ?? 10 * 1024 * 1024;
});

builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<PredefinedHeaderCatalog>();
builder.Services.AddSingleton<CsvTabularFileParser>();
builder.Services.AddSingleton<XlsxTabularFileParser>();
builder.Services.AddSingleton<TabularFileParser>();
builder.Services.AddSingleton<HeaderRowDetector>();
builder.Services.AddSingleton<FallbackHeaderMappingService>();
builder.Services.AddHttpClient<OpenAiHeaderMappingService>();
builder.Services.AddScoped<IHeaderMappingSuggester, HeaderMappingSuggester>();
builder.Services.AddScoped<ImportWorkflow>();

var app = builder.Build();

app.UseCors("frontend");

app.MapGet("/api/import/headers", (PredefinedHeaderCatalog catalog) =>
{
    return Results.Ok(catalog.GetHeaders());
});

app.MapPost("/api/import/analyze", async (HttpRequest request, ImportWorkflow workflow, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Upload must use multipart/form-data." });
    }

    try
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

        if (file is null)
        {
            return Results.BadRequest(new { message = "No file was uploaded." });
        }

        var response = await workflow.AnalyzeAsync(file, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/import/complete", async (CompleteImportRequest request, ImportWorkflow workflow, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await workflow.CompleteAsync(request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.Run();
