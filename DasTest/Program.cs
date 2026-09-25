using Dapper;
using DasTest;
using DasTest.DTO;
using DasTest.Infrastructure.Utils;
using DasTest.Validators;
using FluentValidation;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.WriteIndented = true;
    opts.SerializerOptions.Encoder =
        System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

builder.Services.AddScoped<IParseUseCase, ParseUseCase>();
builder.Services.AddScoped<IValidator<InputData>, InputDataValidator>();
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres")));


var app = builder.Build();


app.UseSwagger(options =>
{
    options.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api/swagger/v1/swagger.json", "My API V1");
    options.RoutePrefix = "api/swagger";
});

app.MapPost("/", async (
    InputData dto,
    IValidator<InputData> validator,
    IParseUseCase useCase,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(dto, ct);

    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
        .Select(e => new { e.PropertyName, e.ErrorCode, e.ErrorMessage })
        .ToList();

        logger.LogWarning(
            "Validation failed for InputData: {ErrorCount} error(s). Details: {@Errors}",
            errors.Count,
            errors);

        return Results.Json(
            ParseResponse.Error(
                code: ErrorCodes.ValidationError,
                message: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))),
            statusCode: StatusCodes.Status400BadRequest);
    }

    var response = await useCase.ProcessAsync(dto, ct);
    var statusCode = response.IsError == 0
        ? StatusCodes.Status200OK
        : response.ErrorCode switch
        {
            ErrorCodes.UnknownError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

    return Results.Json(response, statusCode: statusCode);
}).WithName("Parse Input Data")
    .Produces<ParseResponse>(StatusCodes.Status200OK)
    .Produces<ParseResponse>(StatusCodes.Status400BadRequest)
    .WithSummary("Парсит HTML-страницу, извлекает элементы, email'ы и расшифровывает текст");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await DatabaseEnsureUtility.EnsureElementsSchemaAsync(db);
}

app.Run();

namespace DasTest.Infrastructure.Utils
{
    public static class DatabaseEnsureUtility
    {
        public static async Task EnsureElementsSchemaAsync(IDbConnection db)
        {
            const string createSql = """
                CREATE TABLE IF NOT EXISTS elements (
                    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    attribute_value TEXT,
                    html_code       TEXT
                );
             """;
            await db.ExecuteAsync(createSql);
        }
    }
}