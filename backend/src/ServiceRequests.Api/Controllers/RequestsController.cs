using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Services;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController(IRequestService requestService, IRequestSummaryService summaryService) : ControllerBase
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
}
