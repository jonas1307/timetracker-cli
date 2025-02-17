using FluentValidation;
using Timetracker.Options;
using Timetracker.Utils;

namespace Timetracker.Validators;

public class ConfigValidator : AbstractValidator<ConfigOptions>
{
    public ConfigValidator()
    {
        RuleFor(x => x.TimetrackerBearerToken)
            .NotEmpty().WithMessage("A Bearer token is required for authentication.");

        RuleFor(x => x.TimetrackerUrl)
            .NotEmpty().WithMessage("The Timetracker URL is required. Please provide the base URL.")
            .Must(ValidationUtils.ValidUrl).WithMessage("The provided URL is invalid. Ensure it is in the format 'https://<company>.timehub.7pace.com'.");
    }
}
