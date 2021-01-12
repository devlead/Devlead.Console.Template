using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Devlead.Console.Commands;
using Spectre.Console.Cli;
using Spectre.Cli.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => {
                    opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
    );

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.ValidateExamples();

        config.AddCommand<ConsoleCommand>("console")
                .WithDescription("Example console command.")
                .WithExample(new[] { "console" });
    });

return await app.RunAsync(args);