using FluentValidation;
using Timetracker.Options;
using Timetracker.Utils;

namespace Timetracker.Validators;

public class UpdateValidator : AbstractValidator<UpdateOptions>
{
    public UpdateValidator(IEnumerable<string> activityNames)
    {
        RuleFor(x => x)
            .Must(x => x.ActivityDate != null || x.WorkItemId.HasValue || x.ActivityLength.HasValue
                    || x.ActivityType != null || x.ActivityComment != null || x.ActivityStartHour != null)
            .WithMessage("At least one field must be provided to update.");

        When(x => x.ActivityDate != null, () =>
        {
            RuleFor(x => x.ActivityDate)
                .Must(ValidationUtils.ValidActivityDate)
                .WithMessage("Invalid date. Use YYYY/MM/DD, 'today' or 'yesterday'.");
        });

        When(x => x.WorkItemId.HasValue, () =>
        {
            RuleFor(x => x.WorkItemId!.Value)
                .GreaterThan(0)
                .WithMessage("Work Item ID must be greater than 0.");
        });

        When(x => x.ActivityLength.HasValue, () =>
        {
            RuleFor(x => x.ActivityLength!.Value)
                .GreaterThan(0)
                .WithMessage("Activity length must be greater than 0.");
        });

        When(x => x.ActivityType != null, () =>
        {
            RuleFor(x => x.ActivityType)
                .Must(t => ValidationUtils.ValidType(activityNames, t))
                .WithMessage("Activity type is invalid. Use the 'activities' command to list allowed values.");
        });

        When(x => !string.IsNullOrEmpty(x.ActivityComment), () =>
        {
            RuleFor(x => x.ActivityComment)
                .MinimumLength(3)
                .WithMessage("Activity comments must be at least 3 characters long.");
        });

        When(x => x.ActivityStartHour != null, () =>
        {
            RuleFor(x => x.ActivityStartHour)
                .Matches(@"^([01]?[0-9]|2[0-3]):([0-5][0-9])$")
                .WithMessage("Please provide a valid time in the format HH:MM (e.g., 9:00, 09:00, or 21:00).");
        });
    }
}
