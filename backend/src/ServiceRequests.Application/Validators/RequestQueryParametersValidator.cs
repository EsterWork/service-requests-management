using FluentValidation;
using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Validators;

public class RequestQueryParametersValidator : AbstractValidator<RequestQueryParameters>
{
    private const int MaxTextLength = 200;

    public RequestQueryParametersValidator()
    {
        RuleFor(parameters => parameters.Page).GreaterThanOrEqualTo(1);

        RuleFor(parameters => parameters.PageSize).InclusiveBetween(1, RequestQueryParameters.MaxPageSize);

        RuleFor(parameters => parameters.Status).IsInEnum();
        RuleFor(parameters => parameters.Priority).IsInEnum();

        RuleFor(parameters => parameters.OrganizationName).MaximumLength(MaxTextLength);
        RuleFor(parameters => parameters.AssignedTo).MaximumLength(MaxTextLength);
        RuleFor(parameters => parameters.Search).MaximumLength(MaxTextLength);

        RuleFor(parameters => parameters.CreatedTo)
            .GreaterThanOrEqualTo(parameters => parameters.CreatedFrom)
            .When(parameters => parameters.CreatedFrom.HasValue && parameters.CreatedTo.HasValue)
            .WithMessage("'createdTo' must be later than or equal to 'createdFrom'.");

        RuleFor(parameters => parameters.SortBy)
            .Must(sortBy => RequestSortFields.All.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"'sortBy' must be one of: {string.Join(", ", RequestSortFields.All)}.");

        RuleFor(parameters => parameters.SortDirection)
            .Must(direction => SortDirections.All.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"'sortDirection' must be one of: {string.Join(", ", SortDirections.All)}.");
    }
}
