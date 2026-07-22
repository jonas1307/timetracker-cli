using FluentValidation;
using Timetracker.Options;
using Timetracker.Utils;

namespace Timetracker.Validators;

public class ConfigValidator : AbstractValidator<ConfigOptions>
{
    public ConfigValidator()
    {
        // A border-only invocation (config --border ...) does not set credentials.
        bool BorderOnly(ConfigOptions x) =>
            !string.IsNullOrEmpty(x.Border) &&
            string.IsNullOrEmpty(x.TimetrackerUrl) &&
            string.IsNullOrEmpty(x.TimetrackerBearerToken);

        When(x => !x.Show && !x.Reset && !BorderOnly(x), () =>
        {
            RuleFor(x => x.TimetrackerBearerToken)
                .NotEmpty().WithMessage("A Bearer token is required for authentication.");

            RuleFor(x => x.TimetrackerUrl)
                .NotEmpty().WithMessage("The Timetracker URL is required. Please provide the base URL.")
                .Must(ValidationUtils.ValidUrl).WithMessage("The provided URL is invalid or does not use HTTPS. Ensure it is in the format 'https://<company>.timehub.7pace.com'.");
        });

        When(x => !string.IsNullOrEmpty(x.Border), () =>
        {
            RuleFor(x => x.Border)
                .Must(b => b.ToLowerInvariant() is "minimal" or "square" or "markdown")
                .WithMessage("--border must be one of: minimal, square, markdown.");
        });
    }
}
