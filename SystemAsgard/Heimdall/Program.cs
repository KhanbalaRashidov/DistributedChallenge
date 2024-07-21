using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Heimdall.Services;
using SecretsAgent;
using Steeltoe.Discovery.Client;
using Steeltoe.Common.Http.Discovery;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDiscoveryClient();

builder.Services.AddHttpClient("HomeMessengerApi", client =>
{
    client.BaseAddress = new Uri("http://web-backend-service");
})
.AddServiceDiscovery()
.AddRoundRobinLoadBalancer();

builder.Services.AddHttpClient("HomeGatewayApi", client =>
{
    client.BaseAddress = new Uri("http://home-gateway-service");
})
.AddServiceDiscovery()
.AddRoundRobinLoadBalancer();

builder.Services.AddHttpClient("HalAuditApi", client =>
{
    client.BaseAddress = new Uri("http://hal-audit-service");
})
.AddServiceDiscovery()
.AddRoundRobinLoadBalancer();

builder.Services.AddHttpClient("MiddleEarthGatewayApi", client =>
{
    client.BaseAddress = new Uri("http://reporting-gateway-service");
})
.AddServiceDiscovery()
.AddRoundRobinLoadBalancer();

var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
});
ILogger<SecretStoreService> logger = loggerFactory.CreateLogger<SecretStoreService>();
IConfiguration configuration = builder.Configuration;
var secretStoreService = new SecretStoreService(logger, configuration);

builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString: secretStoreService.GetSecretAsync("RedisConnectionString")
                    .GetAwaiter().GetResult(),
        name: "Redis",
        tags: ["Docker-Compose", "Redis"])
    .AddRabbitMQ(
        rabbitConnectionString: secretStoreService.GetSecretAsync("RabbitAmqpConnectionString")
                    .GetAwaiter().GetResult(),
        name: "RabbitMQ",
        tags: ["Docker-Compose", "RabbitMQ"])
    .AddNpgSql(connectionString: secretStoreService.GetSecretAsync("GamersWorldDbConnStr")
                    .GetAwaiter().GetResult(),
        name: "Report Db",
        tags: ["Docker-Compose", "PostgreSQL", "Database"]
    )
    .AddConsul(setup =>
        {
            setup.HostName = "localhost";
            setup.Port = 8500;
            setup.RequireHttps = false;
        },
        name: "Consul",
        tags: ["Docker-Compose", "Consul", "Service-Discovery", "hashicorp"]
        )
    .AddCheck("GamersWorld Messenger",
        instance: new HealthChecker(builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), "HomeMessengerApi"),
        tags: ["SystemHOME", "REST", "BackendApi"]
    )
    .AddCheck(
        name: "Audit Api",
        instance: new HealthChecker(builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), "HalAuditApi"),
        tags: ["SystemHAL", "REST", "AuditApi"]
    )
    .AddCheck(
        name: "Kahin Reporting Gateway",
        instance: new HealthChecker(builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), "MiddleEarthGatewayApi"),
        tags: ["SystemMIDDLE_EARTH", "REST"]
    )
    .AddCheck(
        name: "GamersWorld Gateway",
        instance: new HealthChecker(builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), "HomeGatewayApi"),
        tags: ["SystemHOME", "REST"]
    );

builder.Services.AddHealthChecksUI(setupSettings =>
{
    setupSettings.SetHeaderText("Inventory Health Check Gate");
    setupSettings.AddHealthCheckEndpoint("Basic Health Check", "/health");
    setupSettings.SetEvaluationTimeInSeconds(10);
    setupSettings.SetApiMaxActiveRequests(2);
}).AddInMemoryStorage();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(config => config.UIPath = "/health-ui");

app.Run();
