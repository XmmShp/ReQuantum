using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NOF.Infrastructure.Core;

namespace ReQuantum.MAUI.Persistence;

public class MigratorAppBuilder : NOFAppBuilder<IHost>
{
    public required HostApplicationBuilder HostApplicationBuilder { get; init; }

    public static MigratorAppBuilder Create(string[] args)
    {
        return new MigratorAppBuilder
        {
            HostApplicationBuilder = new HostApplicationBuilder(args)
        };
    }
    protected override Task<IHost> BuildApplicationAsync() => Task.FromResult(HostApplicationBuilder.Build());

    public override void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        => HostApplicationBuilder.ConfigureContainer(factory, configure);

    public override IDictionary<object, object> Properties => ((IHostApplicationBuilder)HostApplicationBuilder).Properties;
    public override IConfigurationManager Configuration => HostApplicationBuilder.Configuration;
    public override IHostEnvironment Environment => HostApplicationBuilder.Environment;
    public override ILoggingBuilder Logging => HostApplicationBuilder.Logging;
    public override IMetricsBuilder Metrics => HostApplicationBuilder.Metrics;
    public override IServiceCollection Services => HostApplicationBuilder.Services;
}
