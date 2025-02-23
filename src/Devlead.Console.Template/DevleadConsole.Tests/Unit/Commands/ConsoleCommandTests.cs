namespace DevleadConsole.Tests;

public class ConsoleCommandTests
{
    [TestCase("--help")]
    [TestCase("console")]
    [TestCase("console", "mandatoryValue")]
    [TestCase("console", "mandatoryValue", "optionalValue")]
    [TestCase("console", "mandatoryValue", "--command-option-flag")]
    [TestCase("console", "mandatoryValue", "--command-option-value", "commandOptionValue")]
    public async Task RunAsync(params string[] args)
    {
        // Given
            var (commandApp, testConsole, fakeLog) = ServiceProviderFixture
                                                .GetRequiredService<ICommandApp, TestConsole, FakeLogger<ConsoleCommand>>();

            // When
            var result = await commandApp.RunAsync(args);

            // Then
            await Verify(
                    new
                    {
                        ExitCode = result,
                        ConsoleOutput = testConsole.Output,
                        LogOutput = fakeLog.Collector.GetSnapshot().Select(log => new { log.Level, log.Message }).ToArray()
                    }
                )
                .DontIgnoreEmptyCollections()
                .AddExtraSettings(setting => setting.DefaultValueHandling = Argon.DefaultValueHandling.Include)
                .IgnoreStackTrace();
    }
}
