public partial class Program
{

    static partial void AddServices(IServiceCollection services)
    {
    }

    static partial void ConfigureApp(AppServiceConfig appServiceConfig)
    {
        appServiceConfig.SetApplicationName("DevleadConsole");

        appServiceConfig
            .AddCommand<ConsoleCommand>("console")
            .WithDescription("Example console command.")
            .WithExample(new[] { "console" });
    }
}