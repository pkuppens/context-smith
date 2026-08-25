using ContextSmith.Api;
using ContextSmith.Application;

const string AngularDevClient = "AngularDevClient";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddContextSmithApi(builder.Configuration);

builder.Services.AddCors(options => options.AddPolicy(AngularDevClient, policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(AngularDevClient);

app.MapGet("/api/prompts", () => Results.Ok(PromptCatalog.All));

app.MapPost("/api/documents", async (HttpRequest request, DocumentProcessingService service, CancellationToken cancellationToken) =>
{
    if (request.HasFormContentType)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is not null)
        {
            await using var stream = file.OpenReadStream();
            var summary = await service.PrepareFromUploadAsync(file.FileName, stream, cancellationToken);
            return Results.Ok(summary);
        }

        var urlValue = form["url"].ToString();
        if (!string.IsNullOrWhiteSpace(urlValue) && Uri.TryCreate(urlValue, UriKind.Absolute, out var formUrl))
        {
            var summary = await service.PrepareFromUrlAsync(formUrl, cancellationToken);
            return Results.Ok(summary);
        }
    }
    else if (request.HasJsonContentType())
    {
        var body = await request.ReadFromJsonAsync<UrlDocumentRequest>(cancellationToken);
        if (body is not null && Uri.TryCreate(body.Url, UriKind.Absolute, out var jsonUrl))
        {
            var summary = await service.PrepareFromUrlAsync(jsonUrl, cancellationToken);
            return Results.Ok(summary);
        }
    }

    return Results.BadRequest("Provide a multipart 'file' field, a multipart 'url' field, or a JSON { \"url\": \"...\" } body.");
})
.DisableAntiforgery();

app.MapPost("/api/chat", async (ChatRequest request, DocumentProcessingService service, CancellationToken cancellationToken) =>
{
    try
    {
        var (answer, sources) = await service.ChatAsync(request.DocumentId, request.Message, cancellationToken);
        var response = new ChatResponse(
            answer,
            sources.Select(chunk => new ChatSource(chunk.HeadingPath, chunk.Text)).ToList());
        return Results.Ok(response);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
});

app.Run();
