using System.Globalization;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Infrastructure.Seeding;

/// <summary>
/// Generates realistic, varied service requests. A fixed random seed keeps runs reproducible.
/// </summary>
public class ServiceRequestDataGenerator(DateTime utcNow)
{
    private const int RandomSeed = 42;
    private static readonly TimeSpan CreationRange = TimeSpan.FromDays(730);
    private static readonly TimeSpan MaxHandlingTime = TimeSpan.FromDays(30);

    private static readonly string[] OrganizationPrefixes =
    [
        "Northwind", "Galil", "Carmel", "Negev", "Yarden", "Arava", "Hermon", "Tavor", "Sharon", "Kinneret",
        "Ayalon", "Golan", "Shfela", "Eilat", "Modiin", "Orion", "Atlas", "Delta", "Vertex", "Summit",
        "Harbor", "Cedar", "Maple", "Silverline", "Bluewave"
    ];

    private static readonly string[] OrganizationTypes =
    [
        "Logistics", "Technologies", "Industries", "Medical Center", "Foods",
        "Construction", "Consulting", "Retail", "Insurance", "Education Group"
    ];

    private static readonly string[] FirstNames = ["Dana", "Yossi", "Noa", "Avi", "Maya", "Eitan", "Tamar", "Omer"];
    private static readonly string[] LastNames = ["Cohen", "Levi", "Mizrahi", "Peretz", "Biton", "Friedman"];

    private static readonly string[] TitleTemplates =
    [
        "Employer certificate request", "Salary slip correction", "Pension deposit inquiry",
        "Employee details update", "Severance pay calculation", "Work permit renewal",
        "Annual report submission", "Tax deduction clarification", "Payment arrangement request",
        "Address change", "Missing deposit report", "Contract termination notice",
        "Sick leave balance inquiry", "Training program enrollment", "Bank details update",
        "Invoice dispute", "Access permissions request", "Overtime payment review",
        "Maternity leave registration", "Duplicate payment refund"
    ];

    private static readonly (RequestStatus Value, int Weight)[] StatusWeights =
    [
        (RequestStatus.New, 15), (RequestStatus.InProgress, 25), (RequestStatus.Waiting, 15), (RequestStatus.Completed, 45)
    ];

    private static readonly (RequestPriority Value, int Weight)[] PriorityWeights =
    [
        (RequestPriority.Low, 40), (RequestPriority.Medium, 40), (RequestPriority.High, 20)
    ];

    private static readonly string[] Organizations =
        OrganizationPrefixes.SelectMany(prefix => OrganizationTypes.Select(type => $"{prefix} {type} Ltd")).ToArray();

    private static readonly string[] Handlers =
        FirstNames.SelectMany(first => LastNames.Select(last => $"{first} {last}")).ToArray();

    private readonly Random _random = new(RandomSeed);

    public IEnumerable<ServiceRequest> Generate(int count)
    {
        for (var id = 1; id <= count; id++)
        {
            yield return CreateRequest(id);
        }
    }

    private ServiceRequest CreateRequest(int id)
    {
        var createdAt = utcNow - RandomDuration(CreationRange);
        var status = PickWeighted(StatusWeights);
        var isUnassigned = status == RequestStatus.New && _random.Next(2) == 0;

        return new ServiceRequest
        {
            Id = id,
            Title = string.Create(CultureInfo.InvariantCulture, $"{Pick(TitleTemplates)} - {createdAt:MMMM yyyy}"),
            OrganizationName = Pick(Organizations),
            Status = status,
            Priority = PickWeighted(PriorityWeights),
            AssignedTo = isUnassigned ? null : Pick(Handlers),
            CreatedAt = createdAt,
            UpdatedAt = status == RequestStatus.New ? createdAt : CreateUpdatedAt(createdAt)
        };
    }

    private DateTime CreateUpdatedAt(DateTime createdAt)
    {
        var maxElapsed = utcNow - createdAt;
        return createdAt + RandomDuration(maxElapsed < MaxHandlingTime ? maxElapsed : MaxHandlingTime);
    }

    private TimeSpan RandomDuration(TimeSpan max) => TimeSpan.FromSeconds(_random.NextDouble() * max.TotalSeconds);

    private T Pick<T>(T[] items) => items[_random.Next(items.Length)];

    private T PickWeighted<T>((T Value, int Weight)[] options)
    {
        var roll = _random.Next(options.Sum(option => option.Weight));
        foreach (var (value, weight) in options)
        {
            if (roll < weight)
            {
                return value;
            }

            roll -= weight;
        }

        throw new InvalidOperationException("Weighted options must have positive weights.");
    }
}
