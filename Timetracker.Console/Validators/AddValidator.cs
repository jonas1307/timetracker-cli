using FluentValidation;
using Timetracker.Options;
using Timetracker.Utils;

namespace Timetracker.Validators;

public class AddValidator : AbstractValidator<AddOptions>
{
    private readonly IEnumerable<string> _activities;

    public AddValidator(IEnumerable<string> activities)
    {
        _activities = activities;

        RuleFor(x => x.ActivityDate)
            .Matches(@"^\d{4}/\d{2}/\d{2}$").WithMessage("Please provide a date in the format YYYY/MM/DD (e.g., 2025/12/31).")
            .Must(ValidationUtils.ValidDate).WithMessage("The provided date is invalid. Please check the date and try again.");

        RuleFor(x => x.WorkItemId)
            .GreaterThan(0).WithMessage("Work Item ID must be greater than 0.");

        RuleFor(x => x.ActivityLength)
            .GreaterThan(0).WithMessage("Activity length must be greater than 0.");

        RuleFor(x => x.ActivityType)
            .Must((x, url) => ValidationUtils.ValidType(_activities, url)).WithMessage("Activity type is invalid. Use the 'activity-type' command to list allowed values.");

        RuleFor(x => x.ActivityComment)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.ActivityComment)).WithMessage("Activity comments must be at least 3 characters long.");

        RuleFor(x => x.ActivityStartHour)
            .Matches("^([01]?[0-9]|2[0-3]):([0-5][0-9])$").When(x => !string.IsNullOrEmpty(x.ActivityStartHour))
            .WithMessage("Please provide a valid time in the format HH:MM (e.g., 9:00, 09:00, or 21:00).");
    }
}

