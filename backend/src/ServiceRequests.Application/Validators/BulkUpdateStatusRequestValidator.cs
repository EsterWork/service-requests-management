using FluentValidation;
using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Validators;

public class BulkUpdateStatusRequestValidator : AbstractValidator<BulkUpdateStatusRequest>
{
    public BulkUpdateStatusRequestValidator()
    {
        RuleFor(request => request.NewStatus).NotNull().IsInEnum();
        RuleFor(request => request.ChangedBy).NotEmpty().MaximumLength(100);

        RuleFor(request => request.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(items => items.Count <= BulkUpdateStatusRequest.MaxItems)
            .WithMessage($"'items' must contain at most {BulkUpdateStatusRequest.MaxItems} requests.")
            .Must(items => items.Select(item => item.Id).Distinct().Count() == items.Count)
            .WithMessage("'items' must not contain the same request id more than once.");

        RuleForEach(request => request.Items).ChildRules(item =>
        {
            item.RuleFor(entry => entry.Id).GreaterThan(0);
            item.RuleFor(entry => entry.RowVersion)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(RowVersionFormat.IsValid).WithMessage(RowVersionFormat.ErrorMessage);
        });
    }
}
