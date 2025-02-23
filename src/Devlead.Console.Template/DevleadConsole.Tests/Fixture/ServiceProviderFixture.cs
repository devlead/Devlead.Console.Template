
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Devlead.Console.Extensions;

public static partial class ServiceProviderFixture
{
    static partial void InitServiceProvider(IServiceCollection services)
    {
        var logger = new FakeLogger<ConsoleCommand>();

        services
            .AddSingleton(logger)
            .AddSingleton<ILogger<ConsoleCommand>>(
               provider => provider.GetRequiredService<FakeLogger<ConsoleCommand>>()
            );


        services
            .AddCommandApp(new TestConsole());
    }
}