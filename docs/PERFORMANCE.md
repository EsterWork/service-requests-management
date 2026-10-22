# ביצועים: מדידות על 100,000 רשומות

כל המספרים במסמך נמדדו בפועל על ה-Seed המלא (100,000 פניות), באמצעות הסקריפט [performance/Measure-Queries.ps1](performance/Measure-Queries.ps1). תוכניות הביצוע המלאות (Actual Execution Plans) נמצאות ב-[performance/plans.txt](performance/plans.txt).

## שיטת המדידה

| נושא | פירוט |
|---|---|
| סביבה | SQL Server 2019 Express, מחשב פיתוח (Windows 11, 11.8GB RAM) |
| נתונים | 100,000 פניות מה-Seed (`dotnet run -- seed`), נתונים נקיים לפני המדידה |
| השאילתות | ה-SQL המדויק ש-EF Core מייצר, כפי שנלכד מלוג ה-API, מורץ דרך `sp_executesql` עם פרמטרים, בדיוק כמו ש-EF שולח אותו |
| "לפני אינדקסים" | עותק זמני של הטבלה (`ServiceRequests_NoIndexes`) עם אותן שורות ו-Primary Key בלבד. הטבלה האמיתית לא שונתה, והעותק נמחק בסוף |
| מדדים | `SET STATISTICS IO, TIME ON`: **Logical reads** (מספר דפי 8KB שנקראו) וזמן ריצה (Median של 7 הרצות, אחרי הרצת חימום) |
| תוכנית ביצוע | `SET STATISTICS PROFILE ON`: התוכנית שרצה בפועל, כולל מספר השורות שעברו בכל אופרטור |

**איך לקרוא את המספרים:** המדד העיקרי הוא **Logical reads**. הוא לא תלוי במחשב ומראה כמה עבודה השאילתה עושה. זמני הריצה נמדדו כשהנתונים כבר בזיכרון (Warm cache), ולכן ההבדלים בזמן קטנים יותר מההבדלים בקריאות. במחשב הפיתוח נמדד גם מחסור בזיכרון פיזי, ולכן הזמנים המוחלטים עשויים להיות שונים בסביבה אחרת.

## איך נמנעת טעינת כל הנתונים לזיכרון

- השאילתה נבנית כ-`IQueryable` עד הסוף ([RequestService.cs](../backend/src/ServiceRequests.Application/Services/RequestService.cs)). סינון, חיפוש, מיון ודפדוף מתורגמים ל-`WHERE`, `ORDER BY` ו-`OFFSET/FETCH` ורצים ב-DB.
- `CountAsync` רץ כ-`SELECT COUNT(*)` ב-DB, ולא סופר שורות בזיכרון.
- Projection ישירות ל-DTO: ה-`SELECT` קורא רק את העמודות של ה-DTO, ו-`AsNoTracking` חוסך את מעקב השינויים של EF.
- ה-Summary הוא `GROUP BY` ב-DB, שמחזיר 4 ו-3 שורות בלבד.
- `pageSize` מוגבל ל-100 ב-Validation, כך שאי אפשר לבקש את כל הטבלה.

התוצאה: לכל בקשה ה-API מקבל מה-DB לכל היותר שורה אחת של COUNT ועוד עמוד אחד של שורות.

## האינדקסים ולמה

| אינדקס | התרחיש שהוא משרת |
|---|---|
| `(Status, CreatedAt)` | סינון לפי סטטוס עם מיון ברירת המחדל (החדשות קודם); גם סריקה צרה ל-`GROUP BY Status` |
| `(Priority, CreatedAt)` | סינון לפי עדיפות עם אותו מיון; גם `GROUP BY Priority` |
| `CreatedAt` | הרשימה בלי סינון (ברירת המחדל), וטווח תאריכים |
| `OrganizationName` | סינון לפי ארגון וחיפוש prefix בשם הארגון |
| `Title` | חיפוש prefix בכותרת |
| `AssignedTo` | סינון לפי מטפל |
| `RequestStatusHistory (RequestId, ChangedAt)` | היסטוריה של פנייה אחת, ממוינת לפי זמן |

**למה זה עובד:** אינדקס הוא עותק ממוין של עמודות נבחרות. כשהשאילתה מסננת לפי העמודה הראשונה באינדקס, SQL Server קופץ ישר לטווח המתאים (**Index Seek**) במקום לעבור על כל הטבלה (**Scan**). כשהעמודה השנייה היא `CreatedAt`, השורות כבר ממוינות לפי תאריך, ולכן לעמוד הראשון מספיק לקרוא 20 שורות מסוף הטווח, בלי למיין כלום. כל אינדקס כולל גם את ה-`Id` (המפתח הראשי), ולכן המיון `CreatedAt DESC, Id DESC` נתמך במלואו.

**חיפוש טקסט:** `LIKE '%term%'` לא יכול להשתמש באינדקס, כי אינדקס ממוין לפי תחילת הטקסט. לכן החיפוש הוא **prefix** (`LIKE 'term%'`), שכן משתמש באינדקס. המגבלה: מילה מאמצע הכותרת לא נמצאת. פתרון מלא הוא Full-Text Index, וההחלטה מתועדת ב-[DECISIONS.md](DECISIONS.md).

## פעולה 1: רשימה מסוננת (סטטוס + עדיפות)

`GET /api/requests?status=InProgress&priority=High&sortBy=createdAt&sortDirection=desc&page=1&pageSize=20`

```sql
SELECT COUNT(*)
FROM [ServiceRequests] AS [s]
WHERE [s].[Status] = @status AND [s].[Priority] = @priority

SELECT [s].[Id], [s].[Title], [s].[OrganizationName], [s].[Status], [s].[Priority],
       [s].[AssignedTo], [s].[CreatedAt], [s].[UpdatedAt], [s].[RowVersion]
FROM [ServiceRequests] AS [s]
WHERE [s].[Status] = @status AND [s].[Priority] = @priority
ORDER BY [s].[CreatedAt] DESC, [s].[Id] DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
```

| שאילתה | בלי אינדקסים | עם אינדקסים |
|---|---|---|
| COUNT (5,040 תוצאות) | 2,044 reads, 17ms | 350 reads, 19ms |
| עמוד 1 (20 שורות) | 2,044 reads, 24ms | **165 reads, 0ms** |

**תוכנית הביצוע, עמוד 1:**

```
בלי אינדקסים:
  5040 rows   Clustered Index Scan (WHERE Status = @status AND Priority = @priority)   ← עובר על כל 100,000 השורות
    20 rows   Sort (CreatedAt DESC, Id DESC)                                          ← ממיין את כל 5,040 התוצאות
עם אינדקסים:
    50 rows   Index Seek IX_ServiceRequests_Priority_CreatedAt ORDERED BACKWARD       ← קורא מהתאריך החדש אחורה
    20 rows   Clustered Index Seek (Key Lookup, WHERE Status = @status) × 50          ← עוצר אחרי 20 התאמות
```

עם האינדקס, SQL Server קורא **50 שורות** במקום 100,000, ולא ממיין כלום.

**ה-COUNT מעניין יותר:** עם האינדקסים הוא קורא פי 6 פחות דפים, אבל לא מהיר יותר (19ms מול 17ms). התוכנית מסבירה למה: אין אינדקס אחד על שתי העמודות, ולכן SQL Server קורא את שני האינדקסים (19,889 שורות של `Priority` ו-25,170 של `Status`) ומצליב אותם ב-Hash Match. פחות I/O, אבל יותר עבודת CPU. ראה את השיפור המוצע בהמשך.

## פעולה 2: חיפוש prefix

`GET /api/requests?search=גליל`

```sql
SELECT COUNT(*)
FROM [ServiceRequests] AS [s]
WHERE [s].[Title] LIKE @search ESCAPE N'\' OR [s].[OrganizationName] LIKE @search ESCAPE N'\'
-- @search = N'גליל%'  (EF מוסיף את ה-% ומבצע escape לתווים מיוחדים שהמשתמש הקליד)
```

| שאילתה | בלי אינדקסים | עם אינדקסים |
|---|---|---|
| COUNT (4,015 תוצאות) | 2,044 reads, 57ms | **50 reads, 7ms** |
| עמוד 1 | 2,044 reads, 92ms | **1,673 reads, 2ms** |

**תוכנית הביצוע, COUNT עם אינדקסים:** שני Index Seek, על `IX_ServiceRequests_Title` (0 שורות) ועל `IX_ServiceRequests_OrganizationName` (4,015 שורות), מאוחדים ב-Concatenation. ה-`OR` לא מונע שימוש באינדקסים.

**תוכנית הביצוע, עמוד 1 עם אינדקסים:** SQL Server בחר לקרוא את `IX_ServiceRequests_CreatedAt` מהסוף ולבדוק כל שורה, עד שנמצאו 20 התאמות (543 שורות נבדקו). כש-4% מהשורות מתאימות זה מהיר מאוד (2ms). בהמשך מתואר הסיכון שבבחירה הזו.

## פעולה 3: Summary (Aggregations)

`GET /api/requests/summary`

```sql
SELECT [s].[Status], COUNT(*) AS [Count] FROM [ServiceRequests] AS [s] GROUP BY [s].[Status]
SELECT [s].[Priority], COUNT(*) AS [Count] FROM [ServiceRequests] AS [s] GROUP BY [s].[Priority]
```

| שאילתה | בלי אינדקסים | עם אינדקסים |
|---|---|---|
| GROUP BY Status | 2,044 reads, 25ms | 764 reads, 36ms |
| GROUP BY Priority | 2,044 reads, 28ms | 763 reads, 42ms |

**תוכנית הביצוע:** בלי אינדקסים: Clustered Index Scan של כל הטבלה ו-Hash Match Aggregate. עם אינדקסים: Index Scan של האינדקס הצר `(Status, CreatedAt)`, שקטן פי 2.7 מהטבלה, ו-Stream Aggregate, כי השורות כבר ממוינות לפי `Status`.

ה-Summary סופר את **כל** 100,000 השורות, ולכן גם עם אינדקס הוא סורק את כולן. האינדקס חוסך 63% מהקריאות, אבל הזמן לא השתפר, כי ספירת 100,000 שורות שכבר בזיכרון היא עבודת CPU. כאן הפתרון הנכון הוא **Cache**, לא אינדקס.

**השפעת ה-Cache, נמדד ברמת ה-API (HTTP מקצה לקצה):**

| קריאה | זמן |
|---|---|
| ראשונה (Cache miss: שתי שאילתות GROUP BY) | 157ms |
| 5 הקריאות הבאות (Cache hit) | 13, 10, 10, 9, 11ms |

לשם השוואה, רשימה מסוננת (פעולה 1) לקחה ברמת ה-API Median של 51ms (בין 40 ל-63ms, 7 קריאות).

## Bottlenecks ושיפורים: נמדדו על אותם נתונים

### 1. COUNT עם שני מסננים: אינדקס מורכב

אין אינדקס אחד על `Status` ו-`Priority` יחד, ולכן SQL Server מצליב שני אינדקסים. ניסוי: עותק של הטבלה עם אינדקס `(Status, Priority, CreatedAt)`:

| | אינדקסים קיימים | עם האינדקס המורכב |
|---|---|---|
| COUNT | 350 reads, 19ms | **21 reads, 0ms** |
| עמוד 1 | 165 reads, 0ms | **66 reads, 0ms** |

עם האינדקס המורכב יש Index Seek אחד שמחזיר בדיוק את 5,040 השורות המתאימות. **המחיר:** עוד אינדקס שצריך לעדכן בכל כתיבה, והוא עוזר רק לשילוב הזה של מסננים. מומלץ אם זה שילוב נפוץ אצל המשתמשים.

### 2. חיפוש עם prefix נדיר: תוכנית שמורה (Parameter Sniffing)

SQL Server שומר את תוכנית הביצוע לפי **הערך הראשון** שהשאילתה רצה איתו. אם הערך הראשון היה prefix נפוץ (`גליל`), נשמרת התוכנית "סרוק לפי תאריך ועצור אחרי 20 התאמות". עם prefix שכמעט אין לו התאמות, אותה תוכנית סורקת את כל הטבלה:

| עמוד 1 עם `search=zzz` (0 תוצאות) | Logical reads | זמן |
|---|---|---|
| התוכנית השמורה מ-`גליל` | **306,956** | **382ms** |
| `OPTION (RECOMPILE)` (תוכנית לפי הערך הנוכחי) | 10 | 5ms |

בתוכנית השמורה נבדקו 100,000 שורות, עם 100,000 Key Lookups, כדי למצוא 0 תוצאות. עם `RECOMPILE` יש שני Index Seek שמוצאים מיד שאין התאמות.

**הצעה:** להוסיף `OPTION (RECOMPILE)` לשאילתת החיפוש בלבד. ב-EF Core 8 עושים את זה עם `TagWith` ו-`DbCommandInterceptor` קטן. המחיר: קומפילציה בכל חיפוש, כמה מילישניות. חלופה: Full-Text Index.

### 3. עמודים עמוקים: Keyset Pagination

`OFFSET 99980` מחייב את SQL Server לעבור על 99,980 שורות ולזרוק אותן. בשני המקרים, עם ובלי אינדקסים, התוכנית היא סריקה ומיון של כל 100,000 השורות:

| העמוד האחרון (5,000) | Logical reads | זמן |
|---|---|---|
| `OFFSET 99980` עם אינדקסים | 3,564 | 213ms |
| `OFFSET 99980` בלי אינדקסים | 2,044 | 213ms |
| Keyset: `WHERE (CreatedAt, Id) < (ערכי השורה האחרונה בעמוד הקודם)` | **66** | **0ms** |

עם Keyset יש Index Seek על `IX_ServiceRequests_CreatedAt` שקורא בדיוק 20 שורות, בלי קשר לעומק העמוד. **המחיר:** אי אפשר לקפוץ ישירות לעמוד N, רק "הבא" ו"הקודם". ב-UI הנוכחי יש קפיצה לעמוד האחרון, ולכן נשארנו עם OFFSET. בהיקף גדול יותר כדאי לעבור ל-Keyset, או להגביל את עומק הדפדוף.

### 4. Summary בהיקף גדול יותר

ה-Summary סורק את כל הטבלה, והזמן שלו יגדל ליניארית עם כמות הנתונים. היום ה-Cache חוסך את רוב הקריאות. בהיקף של מיליוני רשומות אפשר להוסיף Indexed View עם `COUNT_BIG` לכל סטטוס, ש-SQL Server מעדכן בכל כתיבה, וכך הספירה הופכת לקריאה של כמה שורות.

## איך להריץ את המדידות

```powershell
dotnet run --project backend/src/ServiceRequests.Api -- seed
cd docs/performance
./Measure-Queries.ps1 -Server '.\SQLEXPRESS' -Database 'ServiceRequests' -Runs 7
```

הסקריפט יוצר שני עותקים זמניים של הטבלה (בלי אינדקסים, ועם האינדקס המורכב), מדפיס את הטבלאות, כותב את התוכניות ל-`plans.txt`, ומוחק את העותקים. הטבלה האמיתית לא משתנה.
