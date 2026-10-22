using FluentValidation;
using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Validators;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(request => request.NewStatus).NotNull().IsInEnum();
        RuleFor(request => request.RowVersion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(RowVersionFormat.IsValid).WithMessage(RowVersionFormat.ErrorMessage);
        RuleFor(request => request.ChangedBy).NotEmpty().MaximumLength(100);
    }
}
