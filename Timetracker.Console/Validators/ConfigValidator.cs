using FluentValidation;
using Timetracker.Options;
using Timetracker.Services;
using Timetracker.Utils;

namespace Timetracker.Validators;

public class ConfigValidator : AbstractValidator<ConfigOptions>
{
    public ConfigValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Show || x.Reset
                       || !string.IsNullOrEmpty(x.TimetrackerUrl)
                       || !string.IsNullOrEmpty(x.TimetrackerBearerToken)
                       || !string.IsNullOrEmpty(x.Border))
            .WithMessage("Provide at least one option: --url, --token, --border, --show, or --reset.");

        // Credentials are only mandatory on first-time setup. Once configured, any
        // option can be updated on its own without re-entering the others.
        When(x => !x.Show && !x.Reset && !ConfigService.ConfigExists(), () =>
        {
            RuleFor(x => x.TimetrackerBearerToken)
                .NotEmpty().WithMessage("A Bearer token is required for authentication.");

            RuleFor(x => x.TimetrackerUrl)
                .NotEmpty().WithMessage("The Timetracker URL is required. Please provide the base URL.");
        });

        When(x => !string.IsNullOrEmpty(x.TimetrackerUrl), () =>
        {
            RuleFor(x => x.TimetrackerUrl)
                .Must(ValidationUtils.ValidUrl)
                .WithMessage("The provided URL is invalid or does not use HTTPS. Ensure it is in the format 'https://<company>.timehub.7pace.com'.");
        });

        When(x => !string.IsNullOrEmpty(x.Border), () =>
        {
            RuleFor(x => x.Border)
                .Must(b => b.ToLowerInvariant() is "minimal" or "square" or "markdown")
                .WithMessage("--border must be one of: minimal, square, markdown.");
        });
    }
}
