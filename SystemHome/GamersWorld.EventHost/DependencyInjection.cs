using GamersWorld.EventBusiness;
using GamersWorld.EventHost.Factory;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using GamersWorld.Domain.Constants;
using SecretsAgent;
using GamersWorld.Application.Contracts.Events;

namespace GamersWorld.EventHost;

public static class DependencyInjection
{
    public static IServiceCollection AddEventDrivers(this IServiceCollection services)
    {
        services.AddTransient<IEventDriver<ReportRequestedEvent>, NewReportRequest>();
        services.AddTransient<IEventDriver<ReportReadyEvent>, ReportDocumentAvailable>();
        services.AddTransient<IEventDriver<ReportIsHereEvent>, UsePreparedReport>();
        services.AddTransient<IEventDriver<DeleteReportRequestEvent>, DeleteReport>();
        services.AddTransient<IEventDriver<InvalidExpressionEvent>, InvalidExpression>();
        services.AddTransient<IEventDriver<ArchiveReportRequestEvent>, ArchiveReport>();
        services.AddSingleton<EventHandlerFactory>();

        return services;
    }

    public static IServiceCollection AddRabbitMq(this IServiceCollection services)
    {
        var secretStoreService = services.BuildServiceProvider().GetRequiredService<ISecretStoreService>();
        services.AddSingleton<IConnectionFactory>(c => new ConnectionFactory()
        {
            HostName = secretStoreService.GetSecretAsync(SecretName.RabbitMQHostName).GetAwaiter().GetResult(),
            UserName = secretStoreService.GetSecretAsync(SecretName.RabbitMQUsername).GetAwaiter().GetResult(),
            Password = secretStoreService.GetSecretAsync(SecretName.RabbitMQPassword).GetAwaiter().GetResult(),
            Port = Convert.ToInt32(secretStoreService.GetSecretAsync(SecretName.RabbitMQPort).GetAwaiter().GetResult())
        });
        services.AddSingleton<EventConsumer>();

        return services;
    }
}