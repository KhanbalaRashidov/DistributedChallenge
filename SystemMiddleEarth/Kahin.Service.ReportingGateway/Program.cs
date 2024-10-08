using JudgeMiddleware;
using Kahin.Common.Constants;
using Kahin.Common.Entities;
using Kahin.Common.Enums;
using Kahin.Common.Requests;
using Kahin.Common.Responses;
using Kahin.Common.Validation;
using Kahin.MQ;
using Kahin.Service.ReportingGateway;
using Serilog;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.Services.AddDependencies(logger);
builder.Services.AddServiceDiscovery(o => o.UseConsul());

var systemName = "KahinReportingService";
var environmentName = builder.Environment.EnvironmentName;

Log.Logger = new LoggerConfiguration()
    .UseElasticsearch(systemName, environmentName)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();
app.AddJudgeMiddleware(new Options
{
    DurationThreshold = TimeSpan.FromSeconds(2),
    ExcludedPaths =
    [
        "/swagger"
    ]
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapPost("/getReport", async (GetReportRequest request, ILogger<Program> logger) =>
{
    var response = new GetReportResponse();
    try
    {
        if (request.DocumentId == null)
        {
            response.Exception = "DocumentId is null";
            response.StatusCode = StatusCode.Fail;

            return Results.Json(response);
        }

        var documentId = ReferenceDocumentId.Parse(request.DocumentId);

        var reportContent = await File.ReadAllBytesAsync("SampleReport.dat"); // Just for testing

        logger.LogWarning("Referenced '{DocumentId}' length is  {Length} bytes.", request.DocumentId, reportContent.Length);

        response = new GetReportResponse
        {
            DocumentId = documentId.ToString(),
            Document = reportContent,
            StatusCode = StatusCode.ReportReady,
        };
    }
    catch (Exception excp)
    {
        logger.LogError(excp, "Error on getReport post call.");
        response = new GetReportResponse
        {
            Exception = excp.Message,
            StatusCode = StatusCode.Fail
        };
    }

    return Results.Json(response);
})
.WithName("GetReport")
.WithOpenApi();

app.MapPost("/", async (
    CreateReportRequest request
    , ILogger<Program> logger
    , ValidatorClient validatorClient
    , IRedisService redisService) =>
{
    logger.LogInformation("{TraceId}, {Title}, {Expression}", request.TraceId, request.Title, request.Expression);
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);

    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults
            .GroupBy(e => e.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage ?? string.Empty).ToArray()
            );

        return Results.ValidationProblem(errors);
    }

    var expressionState = await validatorClient.ValidateExpression(request);
    if (!expressionState)
    {
        logger.LogError("'{Expression}' is not valid!", request.Expression);

        var auditPayload = new RedisPayload
        {
            TraceId = request.TraceId,
            EmployeeId = request.EmployeeId,
            EventType = EventType.AuditFail
        };

        await redisService.AddReportPayloadAsync(Names.EventStream, auditPayload, TimeSpan.FromMinutes(TimeCop.OneMilisecond * 10));

        return Results.Json(new CreateReportResponse
        {
            Status = StatusCode.InvalidExpression,
            DocumentId = string.Empty,
            Explanation = "Invalid Report Expression"
        });
    }

    if (!Guid.TryParse(request.TraceId, out var traceId))
    {
        logger.LogWarning("TraceId must be a valid GUID.");

        return Results.BadRequest(new { error = "TraceId must be a valid GUID." });
    }

    Random rnd = new();
    var refDocId = new ReferenceDocumentId
    {
        Head = rnd.Next(1000, 1100),
        Source = rnd.Next(1, 10),
        Stamp = Guid.NewGuid(),
    };

    logger.LogInformation("Created Referenced Document Id: {RefDocumentId}", refDocId.ToString());

    var payload = new RedisPayload
    {
        TraceId = request.TraceId,
        ReportTitle = request.Title,
        DocumentId = refDocId,
        EmployeeId = request.EmployeeId,
        Expression = request.Expression,
        EventType = EventType.ReportRequested,
        ReportExpireTime = request.ExpireTime
    };

    await redisService.AddReportPayloadAsync(Names.EventStream, payload, TimeSpan.FromMinutes(TimeCop.SixtyMinutes));

    var response = new CreateReportResponse
    {
        Status = StatusCode.Success,
        DocumentId = refDocId.ToString(),
        Explanation = "Report request has been recorded successfully."
    };

    return Results.Json(response);
})
.WithName("CreateReportRequest")
.WithOpenApi();

app.Run();