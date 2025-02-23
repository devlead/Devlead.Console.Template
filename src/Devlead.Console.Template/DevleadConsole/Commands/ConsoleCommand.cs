using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace DevleadConsole.Commands;

public class ConsoleCommand(ILogger<ConsoleCommand> logger)
    : AsyncCommand<ConsoleSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConsoleSettings settings)
    {
        logger.LogInformation("Mandatory: {Mandatory}", settings.Mandatory);
        logger.LogInformation("Optional: {Optional}", settings.Optional);
        logger.LogInformation("CommandOptionFlag: {CommandOptionFlag}", settings.CommandOptionFlag);
        logger.LogInformation("CommandOptionValue: {CommandOptionValue}", settings.CommandOptionValue);
        return await Task.FromResult(0);
    }
}