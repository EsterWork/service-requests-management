using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Domain.Enums;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests;

public class GetRequestsTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRequests_WithCombinedFiltersSortingAndPaging_ReturnsCorrectPagesFromDatabase()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(RequestStatus.InProgress, RequestPriority.High, createdDay: 1),
            TestData.Request(RequestStatus.InProgress, RequestPriority.High, createdDay: 2),
            TestData.Request(RequestStatus.InProgress, RequestPriority.High, createdDay: 3),
            TestData.Request(RequestStatus.InProgress, RequestPriority.Low, createdDay: 4),
            TestData.Request(RequestStatus.New, RequestPriority.High, createdDay: 5),
            TestData.Request(RequestStatus.InProgress, RequestPriority.High, organizationName: "Other Organization Ltd", createdDay: 6));
        // "Test" is a prefix of the default organization only, so the "Other Organization" request is filtered out.
        const string filter = "status=InProgress&priority=High&organizationName=Test&sortBy=createdAt&sortDirection=desc&pageSize=2";

        var firstPage = await GetPageAsync($"{filter}&page=1");
        var secondPage = await GetPageAsync($"{filter}&page=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal([requests[2].Id, requests[1].Id], firstPage.Items.Select(item => item.Id));
        Assert.Equal(2, secondPage.Page);
        Assert.Equal([requests[0].Id], secondPage.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetRequests_WithSearch_MatchesTitleOrOrganizationPrefix()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(title: "Salary slip correction"),
            TestData.Request(title: "Address change", organizationName: "Salary Experts Ltd"),
            TestData.Request(title: "Pension deposit inquiry"));

        var prefixMatch = await GetPageAsync("search=salary&sortBy=title&sortDirection=asc");
        var middleOfTitle = await GetPageAsync("search=slip");

        Assert.Equal([requests[1].Id, requests[0].Id], prefixMatch.Items.Select(item => item.Id));
        Assert.Empty(middleOfTitle.Items);
    }

    [Fact]
    public async Task GetRequests_WithHebrewSearch_MatchesTitleOrOrganizationPrefix()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(title: "תיקון תלוש שכר - יוני 2025", organizationName: "גליל לוגיסטיקה בע\"מ"),
            TestData.Request(title: "שינוי כתובת - מאי 2025", organizationName: "שכר ופנסיה בע\"מ"),
            TestData.Request(title: "בירור הפקדה לפנסיה - מרץ 2025", organizationName: "כרמל מזון בע\"מ"));

        var titleMatch = await GetPageAsync($"search={Uri.EscapeDataString("תיקון")}");
        var organizationMatchOnly = await GetPageAsync($"search={Uri.EscapeDataString("שכר")}");
        var organizationFilter = await GetPageAsync($"organizationName={Uri.EscapeDataString("כרמל")}");

        Assert.Equal([requests[0].Id], titleMatch.Items.Select(item => item.Id));
        // "שכר" also appears in the middle of the first title, but the search is prefix-only.
        Assert.Equal([requests[1].Id], organizationMatchOnly.Items.Select(item => item.Id));
        Assert.Equal([requests[2].Id], organizationFilter.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetRequests_WithDateRange_ReturnsOnlyRequestsCreatedInRange()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(createdDay: 0),
            TestData.Request(createdDay: 10),
            TestData.Request(createdDay: 20));

        var page = await GetPageAsync("createdFrom=2025-01-05T00:00:00Z&createdTo=2025-01-15T00:00:00Z");

        Assert.Equal([requests[1].Id], page.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("page=0", "Page")]
    [InlineData("sortBy=status", "SortBy")]
    [InlineData("sortDirection=up", "SortDirection")]
    [InlineData("status=Closed", "Status")]
    [InlineData("createdFrom=2025-02-01T00:00:00Z&createdTo=2025-01-01T00:00:00Z", "CreatedTo")]
    public async Task GetRequests_WithInvalidParameters_Returns400WithFieldError(string query, string expectedErrorField)
    {
        var response = await Client.GetAsync($"{RequestsUrl}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains(expectedErrorField, problem!.Errors.Keys);
    }

    private async Task<PagedResult<ServiceRequestDto>> GetPageAsync(string query)
    {
        var response = await Client.GetAsync($"{RequestsUrl}?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResult<ServiceRequestDto>>(Json))!;
    }
}
