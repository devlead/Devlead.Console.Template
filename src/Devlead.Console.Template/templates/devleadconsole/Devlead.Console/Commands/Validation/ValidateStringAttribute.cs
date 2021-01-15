using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlead.Console.Commands.Validation
{
    public class ValidateStringAttribute : ParameterValidationAttribute
    {
        public const int MinimumLength = 3;

#nullable disable
        public ValidateStringAttribute() : base(errorMessage: null)
        {
        }
#nullable enable

        public override ValidationResult Validate(ICommandParameterInfo parameterInfo, object? value)
            => (value as string) switch {
                { Length: >= MinimumLength }
                    => ValidationResult.Success(),

                { Length: < MinimumLength }
                    => ValidationResult.Error($"{parameterInfo?.PropertyName} ({value}) needs to be at least {MinimumLength} characters long."),

                _ => ValidationResult.Error($"Invalid {parameterInfo?.PropertyName} ({value}) specified.")
            };
    }
}