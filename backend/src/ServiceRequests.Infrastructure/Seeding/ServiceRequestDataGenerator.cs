using System.Globalization;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Infrastructure.Seeding;

/// <summary>
/// Generates realistic, varied service requests. A fixed random seed keeps runs reproducible.
/// The data is in Hebrew to match the Hebrew user interface.
/// </summary>
public class ServiceRequestDataGenerator(DateTime utcNow)
{
    private const int RandomSeed = 42;
    private static readonly TimeSpan CreationRange = TimeSpan.FromDays(730);
    private static readonly TimeSpan MaxHandlingTime = TimeSpan.FromDays(30);
    private static readonly CultureInfo HebrewCulture = CultureInfo.GetCultureInfo("he-IL");

    private static readonly string[] OrganizationPrefixes =
    [
        "גליל", "כרמל", "נגב", "ירדן", "ערבה", "חרמון", "תבור", "שרון", "כנרת", "איילון",
        "גולן", "שפלה", "אילת", "מודיעין", "אורן", "אלון", "דקל", "ארז", "תמר", "רימון",
        "שקד", "זית", "גפן", "ברוש", "אשל"
    ];

    private static readonly string[] OrganizationTypes =
    [
        "לוגיסטיקה", "טכנולוגיות", "תעשיות", "מערכות", "מזון",
        "בנייה", "ייעוץ", "קמעונאות", "ביטוח", "תקשורת"
    ];

    private static readonly string[] FirstNames = ["דנה", "יוסי", "נועה", "אבי", "מאיה", "איתן", "תמר", "עומר"];
    private static readonly string[] LastNames = ["כהן", "לוי", "מזרחי", "פרץ", "ביטון", "פרידמן"];

    private static readonly string[] TitleTemplates =
    [
        "בקשה לאישור מעסיק", "תיקון תלוש שכר", "בירור הפקדה לפנסיה",
        "עדכון פרטי עובד", "חישוב פיצויי פיטורים", "חידוש היתר העסקה",
        "הגשת דוח שנתי", "הבהרה בנושא ניכוי מס", "בקשה להסדר תשלומים",
        "שינוי כתובת", "דיווח על הפקדה חסרה", "הודעה על סיום העסקה",
        "בירור יתרת ימי מחלה", "הרשמה לתוכנית הכשרה", "עדכון פרטי חשבון בנק",
        "השגה על חשבונית", "בקשה להרשאות גישה", "בדיקת תשלום שעות נוספות",
        "רישום חופשת לידה", "החזר תשלום כפול"
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
        OrganizationPrefixes.SelectMany(prefix => OrganizationTypes.Select(type => $"{prefix} {type} בע\"מ")).ToArray();

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
            Title = string.Create(HebrewCulture, $"{Pick(TitleTemplates)} - {createdAt:MMMM yyyy}"),
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
