using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Services;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController(
    IRequestService requestService,
    IRequestSummaryService summaryService,
    IRequestStatusService statusService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ServiceRequestDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceRequestDto>>> GetRequests(
        [FromQuery] RequestQueryParameters parameters, CancellationToken cancellationToken) =>
        Ok(await requestService.GetRequestsAsync(parameters, cancellationToken));

    [HttpGet("summary")]
    [ProducesResponseType<RequestSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestSummaryDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await summaryService.GetSummaryAsync(cancellationToken));

    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<RequestStatusHistoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory(int id, CancellationToken cancellationToken)
    {
        var history = await requestService.GetHistoryAsync(id, cancellationToken);
        return history is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Request {id} was not found.")
            : Ok(history);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(
        int id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await statusService.UpdateStatusAsync(id, request, cancellationToken);

        switch (result.Outcome)
        {
            case StatusUpdateOutcome.Succeeded:
                return Ok(result.Request);
            case StatusUpdateOutcome.NotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: result.Error);
            case StatusUpdateOutcome.Conflict:
                return Problem(statusCode: StatusCodes.Status409Conflict, detail: result.Error);
            case StatusUpdateOutcome.InvalidTransition:
                ModelState.AddModelError(nameof(request.NewStatus), result.Error!);
                return ValidationProblem(ModelState);
            default:
                throw new UnreachableException($"Unhandled outcome {result.Outcome}.");
        }
    }
}
