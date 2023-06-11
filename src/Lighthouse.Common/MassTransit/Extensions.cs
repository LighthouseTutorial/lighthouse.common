using System.Reflection;
using GreenPipes;
using GreenPipes.Configurators;
using Lighthouse.Common.Settings;
using MassTransit;
using MassTransit.Definition;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMQ(
        this IServiceCollection services,
        Action<IRetryConfigurator>? configureRetries = null)
    {
        services.AddMassTransit(options =>
        {
            options.AddConsumers(Assembly.GetEntryAssembly());
            options.UsingLighthouseRabbitMq(configureRetries);
        });

        services.AddMassTransitHostedService();

        return services;
    }

    public static void UsingLighthouseRabbitMq(
        this IServiceCollectionBusConfigurator options,
        Action<IRetryConfigurator>? configureRetries = null)
    {
        options.UsingRabbitMq((context, configurator) =>
        {
            var configuration = context.GetService<IConfiguration>();
            var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
            configurator.Host(rabbitMQSettings!.Host);
            configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings!.ServiceName, false));

            if (configureRetries is null)
            {
                // Default retry policy if one isn't passed in
                configureRetries = (retryConfigurator) => retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
            }

            configurator.UseMessageRetry(configureRetries);

        });
    }
}