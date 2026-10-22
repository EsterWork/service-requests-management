# מערכת לניהול פניות: מבדק Full Stack

[![CI](https://github.com/EsterWork/service-requests-management/actions/workflows/ci.yml/badge.svg)](https://github.com/EsterWork/service-requests-management/actions/workflows/ci.yml)

מערכת לניהול פניות המתקבלות מארגונים ומעסיקים: איתור, סינון, מיון ודפדוף בצד השרת על 100,000 פניות, עדכון סטטוס עם Optimistic Concurrency, היסטוריית שינויים, עדכון Bulk ונתונים מסכמים עם Cache.

| מסמך | תוכן |
|---|---|
| [docs/WORK_PLAN.md](docs/WORK_PLAN.md) | פירוק האפיון ל-Features ו-Tasks, תלויות, סדר ביצוע והערכת מאמץ |
| [docs/PERFORMANCE.md](docs/PERFORMANCE.md) | מדידות על 100K רשומות: SQL, Execution Plans, לפני ואחרי אינדקסים, Bottlenecks |
| [docs/DECISIONS.md](docs/DECISIONS.md) | החלטות טכנולוגיות, חלופות שנשקלו, מגבלות ושיפורים אפשריים |
| [docs/CHECKLIST.md](docs/CHECKLIST.md) | רשימת בדיקה: איפה מומשה כל דרישה במבדק, ומיפוי לטבלת הניקוד |

## התחלה מהירה

דרוש: .NET 8 SDK, Node.js 18.19+/20.11+/22+, ו-SQL Server מקומי בשם `.\SQLEXPRESS` (למופע אחר ראה [שלב 2](#שלב-2-הגדרת-חיבור-ל-sql-server-רק-אם-צריך)).

```powershell
dotnet run --project backend/src/ServiceRequests.Api -- seed   # יוצר את ה-DB ו-100,000 פניות
dotnet run --project backend/src/ServiceRequests.Api           # טרמינל 1: API על http://localhost:5080/swagger
cd frontend; npm ci; npm start                                 # טרמינל 2: ממשק על http://localhost:4200
```

בדיקות: `dotnet test backend/ServiceRequests.sln` (Backend), `cd frontend; npm test` (Frontend). הפירוט המלא ב-[הרצה צעד אחר צעד](#הרצה-צעד-אחר-צעד).

---

## תוכן עניינים

1. [טכנולוגיות וגרסאות](#טכנולוגיות-וגרסאות)
2. [דרישות מקדימות](#דרישות-מקדימות)
3. [הרצה צעד אחר צעד](#הרצה-צעד-אחר-צעד)
4. [פתרון תקלות](#פתרון-תקלות)
5. [מבנה הפתרון](#מבנה-הפתרון)
6. [API: רשימת Endpoints](#api-רשימת-endpoints)
7. [בסיס הנתונים והאינדקסים](#בסיס-הנתונים-והאינדקסים)
8. [Concurrency](#concurrency)
9. [Bulk Update](#bulk-update)
10. [Cache ו-Invalidation](#cache-ו-invalidation)
11. [טיפול בשגיאות, Validation ו-Logging](#טיפול-בשגיאות-validation-ו-logging)
12. [Frontend](#frontend)
13. [בדיקות אוטומטיות](#בדיקות-אוטומטיות)
14. [שימוש בכלי AI](#שימוש-בכלי-ai)

---

## טכנולוגיות וגרסאות

| שכבה | טכנולוגיה | גרסה |
|---|---|---|
| Backend | .NET, ASP.NET Core Web API (Controllers) | .NET 8 (LTS) |
| ORM | Entity Framework Core (SQL Server provider) | 8.0.32 |
| Validation | FluentValidation | 12.1.1 |
| Database | SQL Server | נבדק על SQL Server 2019 Express |
| Frontend | Angular (Standalone Components), RxJS, Angular Material | 18.2.14, RxJS 7.8.2, Material 18.2.14 |
| Tests | xUnit, `WebApplicationFactory` (Microsoft.AspNetCore.Mvc.Testing) | xUnit 2.5.3, Mvc.Testing 8.0.32 |
| API Docs | Swagger (Swashbuckle) | 6.6.2 |

---

## דרישות מקדימות

| כלי | גרסה נדרשת | בדיקה |
|---|---|---|
| .NET SDK | כל SDK של .NET 8 | `dotnet --list-sdks` (שורה שמתחילה ב-`8.0.`) |
| Node.js | 18.19.1+, 20.11.1+ או 22+ | `node -v` |
| npm | מגיע עם Node.js | `npm -v` |
| SQL Server | SQL Server עם הרשאה ליצור Database (המערכת נבדקה על SQL Server 2019 Express) | ראה למטה |
| Git | כל גרסה | `git --version` |
| Google Chrome | נדרש רק לבדיקות ה-Frontend (`npm test`) | |

**אין צורך** להתקין את Angular CLI גלובלית: הוא מותקן מקומית עם `npm ci`.

**SQL Server:** ברירת המחדל בקוד היא מופע בשם `SQLEXPRESS` במחשב המקומי, עם Windows Authentication:

```
Server=.\SQLEXPRESS;Database=ServiceRequests;Trusted_Connection=True;TrustServerCertificate=True
```

אם ה-SQL Server אצלך שונה, ראה [שלב 2](#שלב-2-הגדרת-חיבור-ל-sql-server-רק-אם-צריך).

---

## הרצה צעד אחר צעד

הפקודות מיועדות ל-PowerShell ב-Windows, ומורצות **מתיקיית השורש של הריפו**, אלא אם כתוב אחרת. צריך **שני חלונות טרמינל**: אחד ל-API ואחד ל-Angular.

### שלב 1: הורדת הקוד

```powershell
git clone <repository-url> ServiceRequests
cd ServiceRequests
```

### שלב 2: הגדרת חיבור ל-SQL Server (רק אם צריך)

אם יש לך מופע `.\SQLEXPRESS` עם Windows Authentication, דלג לשלב 3.

אחרת, הגדר את מחרוזת החיבור במשתנה סביבה **באותו חלון טרמינל** שבו תריץ את ה-API וה-Seed:

```powershell
# דוגמה: מופע ברירת מחדל (MSSQLSERVER) עם Windows Authentication
$env:ConnectionStrings__ServiceRequestsDb = "Server=.;Database=ServiceRequests;Trusted_Connection=True;TrustServerCertificate=True"

# דוגמה: SQL Authentication
$env:ConnectionStrings__ServiceRequestsDb = "Server=localhost,1433;Database=ServiceRequests;User Id=sa;Password=<password>;TrustServerCertificate=True"
```

לחלופין, אפשר לשנות את `ConnectionStrings:ServiceRequestsDb` בקובץ [appsettings.json](backend/src/ServiceRequests.Api/appsettings.json).

### שלב 3: יצירת בסיס הנתונים ו-100,000 פניות (Seed)

```powershell
dotnet run --project backend/src/ServiceRequests.Api -- seed
```

הפקודה:
1. יוצרת את ה-Database אם הוא לא קיים, ומריצה את כל ה-Migrations (טבלאות ואינדקסים).
2. מוחקת את כל הפניות וההיסטוריה הקיימות.
3. מכניסה 100,000 פניות חדשות ב-`SqlBulkCopy`, במנות של 10,000.

בסיום תופיע שורה כמו `Seeding completed: 100000 requests in 17.4s`, והפקודה מסתיימת (היא לא מפעילה את השרת). אפשר להריץ אותה שוב בכל פעם שרוצים לחזור לנתונים נקיים.

**משך:** במחשב הפיתוח רוב ההרצות לקחו 17 עד 26 שניות, אבל חלקן לקחו כמה דקות (עד כ-5). ההרצה הראשונה עשויה להיות איטית יותר. אם אין שגיאה בחלון, הפקודה עדיין עובדת.

הנתונים דטרמיניסטיים: כל הרצה יוצרת את אותן פניות (Id 1 עד 100,000), עם 250 ארגונים, 48 מטפלים ותאריכים פרוסים על פני השנתיים האחרונות. הנתונים בעברית, כמו ממשק המשתמש.

### שלב 4: הפעלת ה-API (טרמינל 1)

```powershell
dotnet run --project backend/src/ServiceRequests.Api
```

חכה לשורה:

```
Now listening on: http://localhost:5080
```

**השאר את החלון הזה פתוח.** בדיקה מהירה: פתח בדפדפן את **http://localhost:5080/swagger** (תיעוד אינטראקטיבי של כל ה-Endpoints).

### שלב 5: הפעלת ה-Frontend (טרמינל 2)

```powershell
cd frontend
npm ci
npm start
```

- `npm ci` מתקין את התלויות לפי `package-lock.json`. צריך להריץ אותו רק בפעם הראשונה.
- אם Angular CLI שואל אם לשתף נתוני שימוש (analytics), אפשר לענות `N`.

חכה לשורה:

```
Local:   http://localhost:4200/
```

**פתח בדפדפן את http://localhost:4200.**

שרת הפיתוח של Angular מעביר כל קריאה ל-`/api` אל `http://localhost:5080` ([proxy.conf.json](frontend/proxy.conf.json)), ולכן ה-API משלב 4 חייב לרוץ.

**מה לנסות במסך:**
- **חיפוש:** הקלד `גליל` בשדה החיפוש. הבקשה נשלחת רק אחרי שמפסיקים להקליד.
- **סינון ומיון:** בחר סטטוס או עדיפות, הקלד תחילת שם ארגון או מטפל, בחר טווח תאריכים, ולחץ על כותרת עמודה (כותרת, עדיפות, נוצרה, עודכנה) כדי למיין.
- **דפדוף:** בתחתית הטבלה, כולל שינוי מספר הפניות בעמוד.
- **שמירת התצוגה:** אחרי סינון, מיון ומעבר עמוד, הכתובת בדפדפן מתעדכנת. רענון (F5) מציג את אותה תצוגה.
- **עדכון סטטוס:** כפתור העיפרון בשורה. מוצגים רק המעברים המותרים.
- **היסטוריה:** כפתור השעון בשורה.
- **409 Conflict:** פתח את המערכת ב**שני טאבים**. עדכן פנייה בטאב הראשון, ואז, בלי לרענן, נסה לעדכן את אותה פנייה בטאב השני. תוצג הודעה "הפנייה עודכנה על ידי משתמש אחר", והנתונים ירועננו.
- **מצב Error:** עצור את ה-API (Ctrl+C בטרמינל 1) ושנה סינון. אחרי שתפעיל אותו שוב, לחץ "נסה שוב".

### שלב 6: הרצת הבדיקות האוטומטיות

```powershell
dotnet test backend/ServiceRequests.sln
```

- בדיקות ה-Integration רצות מול SQL Server אמיתי, ב-Database נפרד בשם **`ServiceRequests_Tests`**, שנוצר אוטומטית. ה-Database של שלב 3 לא נפגע.
- אם ה-SQL Server אינו `.\SQLEXPRESS`, הגדר לפני ההרצה:
  ```powershell
  $env:SERVICE_REQUESTS_TEST_DB = "Server=.;Database=ServiceRequests_Tests;Trusted_Connection=True;TrustServerCertificate=True"
  ```
  שם ה-Database **חייב** להסתיים ב-`_Tests`. הבדיקות מוחקות נתונים, ולכן הן מסרבות לרוץ מול Database אחר.
- אם ה-API משלב 4 עדיין רץ, ייתכן שה-Build ייכשל כי הקבצים נעולים. עצור אותו (Ctrl+C) לפני הבדיקות.

תוצאה צפויה: `Passed!` בשני פרויקטי הבדיקות (19 Unit ו-17 Integration).

**בדיקות ה-Frontend** (Karma ו-Jasmine, רצות ב-Chrome Headless, ולכן דורשות Google Chrome מותקן):

```powershell
cd frontend
npm test
```

תוצאה צפויה: `TOTAL: 13 SUCCESS`. הבדיקות לא צריכות API או DB, כי בקשות ה-HTTP מדומות (`HttpTestingController`).

### שלב 7 (אופציונלי): מדידות ביצועים

```powershell
cd docs/performance
./Measure-Queries.ps1 -Server '.\SQLEXPRESS' -Database 'ServiceRequests'
```

פירוט ב-[docs/PERFORMANCE.md](docs/PERFORMANCE.md).

---

## פתרון תקלות

| תסמין | סיבה ופתרון |
|---|---|
| `A network-related or instance-specific error occurred` | ה-SQL Server לא נמצא. בדוק שהשירות רץ (`Get-Service MSSQL*`), והגדר את מחרוזת החיבור לפי [שלב 2](#שלב-2-הגדרת-חיבור-ל-sql-server-רק-אם-צריך) |
| `Cannot open database "ServiceRequests"` | עוד לא הורץ ה-Seed. הרץ את [שלב 3](#שלב-3-יצירת-בסיס-הנתונים-ו-100000-פניות-seed) |
| `CREATE DATABASE permission denied` | המשתמש אינו מורשה ליצור Database. השתמש במשתמש עם הרשאה, או צור Database ריק בשם `ServiceRequests` והרץ שוב |
| `Failed to bind to address http://localhost:5080` | הפורט תפוס, בדרך כלל על ידי API שכבר רץ. סגור אותו, או הרץ עם `--urls http://localhost:5090` ועדכן את `target` ב-[proxy.conf.json](frontend/proxy.conf.json) |
| ה-Frontend מציג "אירעה שגיאה בטעינת הפניות" | ה-API לא רץ על 5080. בדוק את טרמינל 1 |
| `dotnet test` נכשל ב-`The file is locked by: "ServiceRequests.Api"` | ה-API רץ. עצור אותו ונסה שוב |
| `The current .NET SDK does not support targeting .NET 8.0` / `A compatible .NET SDK was not found` | התקן .NET 8 SDK |

---

## מבנה הפתרון

```
/
├─ backend/
│  ├─ ServiceRequests.sln
│  ├─ src/
│  │  ├─ ServiceRequests.Api/             Controllers, Validation filter, Global exception handler, Program.cs
│  │  ├─ ServiceRequests.Application/     Services, DTOs, Validators (FluentValidation), Mapping, Interfaces
│  │  ├─ ServiceRequests.Domain/          Entities, Enums, StatusTransitionValidator (כללי מעבר הסטטוס)
│  │  └─ ServiceRequests.Infrastructure/  DbContext, EF Configurations + אינדקסים, Migrations, Seeding, Interceptor
│  └─ tests/
│     ├─ ServiceRequests.UnitTests/         כללי מעבר, ChangeStatus, RecompileQueryInterceptor
│     └─ ServiceRequests.IntegrationTests/  API + SQL Server אמיתי (WebApplicationFactory)
├─ frontend/                                Angular 18
│  └─ src/app/
│     ├─ core/       models, RequestsApiService (המקום היחיד שמדבר עם ה-API)
│     ├─ features/requests/
│     │    request-list/            טבלה, מיון, דפדוף, מצבי Loading/Empty/Error
│     │    request-filters/         חיפוש וסינון (debounce)
│     │    request-summary/         Aggregations
│     │    status-update/           דיאלוג עדכון סטטוס וטיפול ב-409
│     │    request-history-dialog/  היסטוריית שינויים
│     └─ shared/     תוויות בעברית, טקסטים לדפדוף
└─ docs/          WORK_PLAN, PERFORMANCE, DECISIONS, CHECKLIST, performance/ (סקריפט מדידה ותוכניות ביצוע)
```

**כיוון התלויות:** `Api → Application + Infrastructure`, `Infrastructure → Application → Domain`. ה-Domain לא תלוי באף שכבה.

**DI ו-Lifetimes:** `DbContext` ו-Services הם Scoped (מופע לכל בקשה), `IMemoryCache` ו-`TimeProvider` הם Singleton.

---

## API: רשימת Endpoints

כל ה-Endpoints תחת `http://localhost:5080/api/requests`. Enums מוחזרים כמחרוזות (`"InProgress"`), תאריכים ב-UTC (ISO 8601 עם `Z`), ו-`rowVersion` ב-Base64.

| Method | Path | תיאור | קודי תשובה |
|---|---|---|---|
| GET | `/api/requests` | רשימה עם Pagination, סינון, חיפוש ומיון | 200, 400 |
| GET | `/api/requests/summary` | Aggregations: כמויות לפי Status ולפי Priority (Cache 60s) | 200 |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס, מהחדש לישן | 200, 404 |
| PATCH | `/api/requests/{id}/status` | עדכון סטטוס של פנייה | 200, 400, 404, 409 |
| PATCH | `/api/requests/status/bulk` | עדכון סטטוס של עד 100 פניות (Partial Success) | 200, 400 |

### GET /api/requests

| פרמטר | תיאור | ברירת מחדל |
|---|---|---|
| `page` | מספר עמוד, 1 ומעלה | 1 |
| `pageSize` | 1 עד 100 | 20 |
| `status` | `New`, `InProgress`, `Waiting`, `Completed` | |
| `priority` | `Low`, `Medium`, `High` | |
| `organizationName` | תחילת שם הארגון (prefix) | |
| `assignedTo` | תחילת שם המטפל (prefix) | |
| `createdFrom`, `createdTo` | טווח תאריכי יצירה, כולל הקצוות (ISO 8601, מומלץ עם אזור זמן) | |
| `search` | תחילת הכותרת **או** תחילת שם הארגון (prefix) | |
| `sortBy` | `createdAt`, `updatedAt`, `priority`, `title` | `createdAt` |
| `sortDirection` | `asc`, `desc` | `desc` |

כל הדוגמאות בסעיף הזה הן בקשות ותשובות אמיתיות, שנלכדו מה-API על נתוני ה-Seed.

```http
GET /api/requests?status=InProgress&priority=High&sortBy=createdAt&sortDirection=desc&page=1&pageSize=2
```

```json
{
  "items": [
    {
      "id": 65049,
      "title": "בירור הפקדה לפנסיה - אוקטובר 2026",
      "organizationName": "שרון מזון בע\"מ",
      "status": "InProgress",
      "priority": "High",
      "assignedTo": "יוסי לוי",
      "createdAt": "2026-10-22T11:14:46.9755716Z",
      "updatedAt": "2026-10-22T11:43:11.265267Z",
      "rowVersion": "AAAAAAAMNvA="
    },
    {
      "id": 71406,
      "title": "השגה על חשבונית - אוקטובר 2026",
      "organizationName": "תמר תקשורת בע\"מ",
      "status": "InProgress",
      "priority": "High",
      "assignedTo": "תמר פרץ",
      "createdAt": "2026-10-22T09:56:25.6600919Z",
      "updatedAt": "2026-10-22T10:46:00.918247Z",
      "rowVersion": "AAAAAAAMT94="
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 5039,
  "totalPages": 2520
}
```

פרמטר לא תקין מחזיר 400 עם `ValidationProblemDetails`:

```http
GET /api/requests?pageSize=101&sortBy=status
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "sortBy": ["'sortBy' must be one of: createdAt, updatedAt, priority, title."],
    "pageSize": ["'Page Size' must be between 1 and 100. You entered 101."]
  },
  "traceId": "00-47a2f1a4638fe773bcb8d808985a8c3d-6d0105bc0cdc93d8-00"
}
```

שמות השדות בשגיאות הם camelCase, כמו בשאר ה-JSON. בשדות מקוננים נשמר הנתיב, למשל `items[0].rowVersion` ב-Bulk.

### GET /api/requests/summary

```json
{
  "byStatus": [
    { "status": "New", "count": 14953 },
    { "status": "InProgress", "count": 25168 },
    { "status": "Waiting", "count": 15065 },
    { "status": "Completed", "count": 44814 }
  ],
  "byPriority": [
    { "priority": "Low", "count": 40230 },
    { "priority": "Medium", "count": 39881 },
    { "priority": "High", "count": 19889 }
  ]
}
```

### PATCH /api/requests/{id}/status

```http
PATCH /api/requests/65049/status
Content-Type: application/json

{ "newStatus": "Waiting", "rowVersion": "AAAAAAAMNvA=", "changedBy": "דנה כהן" }
```

תשובה 200, עם `status`, `updatedAt` ו-`rowVersion` חדשים:

```json
{
  "id": 65049,
  "title": "בירור הפקדה לפנסיה - אוקטובר 2026",
  "organizationName": "שרון מזון בע\"מ",
  "status": "Waiting",
  "priority": "High",
  "assignedTo": "יוסי לוי",
  "createdAt": "2026-10-22T11:14:46.9755716Z",
  "updatedAt": "2026-10-22T13:01:32.7436786Z",
  "rowVersion": "AAAAAAAMwAQ="
}
```

שליחה נוספת עם ה-`rowVersion` **הישן** (`AAAAAAAMNvA=`), אחרי שהפנייה כבר עודכנה, מחזירה 409:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Request 65049 was modified by another user. Reload it and try again.",
  "traceId": "00-5243a64e9936183ccd78de3b69bcf961-e49f15f748cfa0a8-00"
}
```

מעבר אסור (פנייה במצב `Completed` ל-`InProgress`) מחזיר 400:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "newStatus": ["Transition from Completed to InProgress is not allowed."] },
  "traceId": "00-861663a21204f4d4d19fc443cdbcc372-3121a30876d0b28f-00"
}
```

| מצב | תשובה |
|---|---|
| הצלחה | 200 עם הפנייה המעודכנת |
| שדה חסר / `rowVersion` לא תקין / סטטוס לא קיים | 400 `ValidationProblemDetails` |
| מעבר אסור | 400, השגיאה תחת `errors.newStatus` |
| הפנייה לא קיימת | 404 `ProblemDetails` |
| הפנייה שונתה מאז שהלקוח קרא אותה | 409 `ProblemDetails` |

**מעברי הסטטוס המותרים** (מוגדרים במקום אחד: [StatusTransitionValidator.cs](backend/src/ServiceRequests.Domain/StatusTransitionValidator.cs)):

```
New ──► InProgress ──► Completed (סופי)
           ▲  │
           │  ▼
          Waiting
```

מעבר לאותו סטטוס אינו חוקי.

### GET /api/requests/{id}/history

אחרי העדכון שלמעלה ועדכון נוסף בחזרה ל-`InProgress`. ה-409 לא נרשם בהיסטוריה:

```json
[
  { "id": 25, "requestId": 65049, "previousStatus": "Waiting", "newStatus": "InProgress", "changedAt": "2026-10-22T13:01:32.7760185Z", "changedBy": "יוסי לוי" },
  { "id": 24, "requestId": 65049, "previousStatus": "InProgress", "newStatus": "Waiting", "changedAt": "2026-10-22T13:01:32.7436786Z", "changedBy": "דנה כהן" }
]
```

### PATCH /api/requests/status/bulk

ארבעה פריטים: פנייה תקינה, פנייה שלא קיימת, פנייה עם `rowVersion` ישן, ופנייה במצב `Completed`:

```http
PATCH /api/requests/status/bulk
Content-Type: application/json

{
  "newStatus": "Waiting",
  "changedBy": "דנה כהן",
  "items": [
    { "id": 76359, "rowVersion": "AAAAAAAMY0s=" },
    { "id": 999999, "rowVersion": "AAAAAAAAAAE=" },
    { "id": 44747, "rowVersion": "AAAAAAAAAAE=" },
    { "id": 85954, "rowVersion": "AAAAAAAMiOs=" }
  ]
}
```

```json
{
  "results": [
    { "id": 76359, "outcome": "Succeeded", "rowVersion": "AAAAAAAMwAY=", "error": null },
    { "id": 999999, "outcome": "NotFound", "rowVersion": null, "error": "Request 999999 was not found." },
    { "id": 44747, "outcome": "Conflict", "rowVersion": null, "error": "Request 44747 was modified by another user. Reload it and try again." },
    { "id": 85954, "outcome": "InvalidTransition", "rowVersion": null, "error": "Transition from Completed to Waiting is not allowed." }
  ],
  "summary": { "total": 4, "succeeded": 1, "notFound": 1, "invalidTransition": 1, "conflict": 1 }
}
```

400 מוחזר רק כשהבקשה עצמה לא תקינה: 0 או יותר מ-100 פריטים, מזהה כפול, `rowVersion` לא תקין, או חוסר ב-`newStatus` או `changedBy`.

---

## בסיס הנתונים והאינדקסים

**SQL Server.** הנתונים טבלאיים עם מבנה קבוע, הדרישות כוללות Aggregations, טרנזקציה שמעדכנת שתי טבלאות יחד, ו-Optimistic Concurrency. ל-SQL Server יש לכל אלה תמיכה מובנית (`rowversion`, טרנזקציות, `GROUP BY`, אינדקסים מורכבים), ו-EF Core תומך בו באופן מלא. ההשוואה ל-MongoDB ב-[DECISIONS.md](docs/DECISIONS.md).

**טבלאות:** `ServiceRequests` (הפניות) ו-`RequestStatusHistory` (ה-Audit, עם Foreign Key ל-`RequestId`). `Status` ו-`Priority` נשמרים כ-`int`, ו-`RowVersion` מסוג `rowversion`.

**אינדקסים** (מוגדרים ב-[ServiceRequestConfiguration.cs](backend/src/ServiceRequests.Infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs)):

| אינדקס | משרת |
|---|---|
| `(Status, CreatedAt)` | סינון לפי סטטוס עם מיון לפי תאריך; `GROUP BY Status` |
| `(Priority, CreatedAt)` | סינון לפי עדיפות עם מיון לפי תאריך; `GROUP BY Priority` |
| `CreatedAt` | הרשימה בלי סינון (ברירת המחדל) וטווח תאריכים |
| `OrganizationName`, `Title` | חיפוש prefix וסינון לפי ארגון |
| `AssignedTo` | סינון לפי מטפל |
| `RequestStatusHistory (RequestId, ChangedAt)` | היסטוריה של פנייה |

**איך נמנעת טעינת כל הנתונים לזיכרון:** השאילתה נשארת `IQueryable` עד הסוף, כך שסינון, חיפוש, מיון ודפדוף רצים ב-SQL (`WHERE`, `ORDER BY`, `OFFSET/FETCH`). `COUNT` ו-`GROUP BY` רצים ב-DB, ה-Projection קורא רק את עמודות ה-DTO, ו-`pageSize` מוגבל ל-100.

**חיפוש:** prefix (`LIKE 'term%'`) על הכותרת ועל שם הארגון. הוא משתמש באינדקסים, בניגוד ל-`LIKE '%term%'`, אבל מילה מאמצע הכותרת לא נמצאת. לשאילתת החיפוש מתווסף `OPTION (RECOMPILE)`, כי התוכנית הטובה תלויה בכמה פניות מתאימות. המדידות והנימוק ב-[PERFORMANCE.md](docs/PERFORMANCE.md).

---

## Concurrency

**הבעיה (Lost Update):** שני משתמשים קוראים את אותה פנייה ומעדכנים אותה. בלי הגנה, העדכון השני דורס את הראשון, בלי שהמשתמש השני ראה אותו.

**הפתרון: Optimistic Concurrency עם `rowversion`.**
1. לכל פנייה יש עמודת `RowVersion` שה-SQL Server משנה אוטומטית בכל עדכון. הלקוח מקבל אותה עם הפנייה (Base64).
2. בעדכון, הלקוח שולח את ה-`rowVersion` שראה.
3. בדיקה מוקדמת: אם הוא שונה מהערך הנוכחי, מוחזר 409 מיד.
4. **ההגנה האמיתית היא ב-DB:** EF שולח
   ```sql
   UPDATE [ServiceRequests] SET [Status] = @p0, [UpdatedAt] = @p1
   OUTPUT INSERTED.[RowVersion]
   WHERE [Id] = @id AND [RowVersion] = @originalRowVersion;
   ```
   אם בקשה אחרת עדכנה את השורה בין הקריאה לכתיבה, ה-UPDATE מעדכן 0 שורות, EF זורק `DbUpdateConcurrencyException`, והתשובה היא 409.
5. העדכון ורשומת ה-Audit נשמרים באותו `SaveChanges`, כלומר באותה טרנזקציה. ב-409 גם רשומת ה-Audit לא נשמרת.

**ב-UI:** ב-409 הדיאלוג נסגר, מוצגת הודעה "הפנייה עודכנה על ידי משתמש אחר", והטבלה וה-Summary נטענים מחדש, כדי שהמשתמש יחליט מחדש על סמך המצב העדכני.

**נבדק:** בדיקת Integration שולחת שני עדכונים **בו-זמניים** עם אותו `rowVersion` ומוודאת שאחד מקבל 200, השני 409, ונוצרה רשומת Audit אחת בלבד. בהרצות חוזרות ה-409 נוצר ב-DB (ה-`WHERE RowVersion`), כלומר שתי הבקשות באמת רצו במקביל.

---

## Bulk Update

**ההתנהגות: Partial Success.** כל פריט מעובד ונשמר **בנפרד**, והתשובה מפרטת לכל פריט אחת מהתוצאות: `Succeeded`, `NotFound`, `InvalidTransition` או `Conflict`, עם סיכום.

**למה Partial Success ולא All-or-Nothing:** בעבודה של כמה משתמשים במקביל, התנגשות בפנייה אחת לא אמורה לחסום עדכון של 99 אחרות. הלקוח מקבל תמונה מלאה, ויכול לנסות שוב רק את הפריטים שנכשלו, עם ה-`rowVersion` העדכני שלהם.

**למה 200 גם כשחלק מהפריטים נכשלו:** הבקשה עצמה תקינה ועובדה במלואה. התוצאה של כל פריט היא התוכן של התשובה, לא שגיאה של הבקשה. קוד שגיאה (4xx) היה רומז שכלום לא קרה, בזמן שחלק מהפניות כן עודכנו. `207 Multi-Status` שייך ל-WebDAV ולא מקובל ב-REST API רגיל. 400 מוחזר רק כשהבקשה עצמה לא תקינה.

**מימוש:**
- **טעינה בשאילתה אחת** לכל הפריטים (`WHERE Id IN (...)`), לא 100 שאילתות.
- **כל פריט נשמר ב-`SaveChanges` משלו**, כלומר בטרנזקציה משלו, עם אותה בדיקת `rowVersion` ואותה רשומת Audit כמו בעדכון בודד. עם `SaveChanges` אחד לכל הפריטים, התנגשות אחת הייתה מבטלת את כולם.
- ה-Cache של ה-Summary נמחק פעם אחת בסוף, אם לפחות פריט אחד הצליח.

---

## Cache ו-Invalidation

**מה נשמר ב-Cache ולמה:** רק ה-Summary (`GET /api/requests/summary`).
- הוא סופר את **כל** 100,000 הפניות בכל קריאה. אינדקס לא מקצר את זה, כי צריך לעבור על כל השורות. נמדד: 764 קריאות דפים ו-~30ms לכל `GROUP BY`.
- התוצאה זהה לכל המשתמשים, ולכן מספיק מפתח אחד.
- מספרים מסכמים בני כמה שניות מקובלים במסך כזה, בניגוד לרשימה, שבה המשתמש צריך לראות את ה-`rowVersion` העדכני.

**Expiration:** Absolute Expiration של 60 שניות.

**Invalidation:** כל עדכון סטטוס מוצלח, בודד או Bulk, מוחק את המפתח **אחרי** שהשינוי נשמר ב-DB. הקריאה הבאה מחשבת מחדש. נבדק בבדיקת Integration (`SummaryCacheTests`).

**נמדד ברמת ה-API:** קריאה ראשונה (Cache miss) 157ms, הקריאות הבאות (Cache hit) כ-10ms.

**מגבלות ידועות:**
- **חלון מירוץ קטן:** אם Summary מחושב בדיוק בזמן שעדכון נשמר, ייתכן שהערך הישן ייכנס ל-Cache אחרי ה-Invalidation, ויישאר עד 60 שניות לכל היותר. זמן התפוגה חוסם את זה.
- כמה בקשות שמגיעות יחד כשה-Cache ריק יחשבו את ה-Summary כל אחת בנפרד. בהיקף הזה זה סביר.

**בסביבה עם כמה מופעי שרת:** `IMemoryCache` נמצא בזיכרון של כל מופע בנפרד, ולכן עדכון במופע A ימחק את ה-Cache רק ב-A, ומופע B יציג מספרים ישנים עד 60 שניות. ההתאמה הנדרשת:
- **Cache משותף:** `IDistributedCache` עם Redis. כל המופעים קוראים וכותבים לאותו מפתח, ו-Invalidation במופע אחד משפיע על כולם. השינוי בקוד מוגבל ל-[RequestSummaryService.cs](backend/src/ServiceRequests.Application/Services/RequestSummaryService.cs), המקום היחיד שמחזיק את המפתח, התפוגה וה-Invalidation.
- **או, אם רוצים להשאיר Cache מקומי** (מהיר יותר): Redis Pub/Sub. המופע שעדכן מפרסם הודעת "summary changed", וכל המופעים מוחקים את המפתח המקומי שלהם.

---

## טיפול בשגיאות, Validation ו-Logging

- **Validation:** FluentValidation לכל קלט (פרמטרי השליפה, עדכון בודד, Bulk), דרך Action Filter אחד ([ValidationFilter.cs](backend/src/ServiceRequests.Api/Filters/ValidationFilter.cs)). כל תשובות ה-400 (שגיאות Model Binding כמו `status=Closed`, שגיאות FluentValidation ומעבר אסור) נבנות במקום אחד ([ValidationProblemResponse.cs](backend/src/ServiceRequests.Api/ErrorHandling/ValidationProblemResponse.cs)), כ-`ValidationProblemDetails` עם שמות שדות ב-camelCase. הודעות JSON לא תקינות לא חושפות שמות טיפוסים פנימיים.
- **Global Exception Handler** ([GlobalExceptionHandler.cs](backend/src/ServiceRequests.Api/ErrorHandling/GlobalExceptionHandler.cs), `IExceptionHandler`): שגיאה לא צפויה מוחזרת כ-500 `ProblemDetails` בלי פרטים פנימיים. בקשה שהלקוח ביטל (למשל חיפוש ש-`switchMap` החליף) מוחזרת כ-499 ונרשמת כ-Information, לא כשגיאה. ב-.NET 8, ה-Middleware של ASP.NET עדיין רושם שורת `fail` אחת כש-SqlClient מדווח על ביטול כ-`SqlException`; אין דרך לכבות את זה ב-.NET 8.
- **Logging מובנה** (`ILogger`): עדכון סטטוס (מזהה ומעבר), Conflict (ואיפה נתפס), סיכום Bulk, Invalidation של ה-Cache, Seeding (כמות וזמן). לא נרשמים שמות משתמשים או תוכן הפניות.

---

## Frontend

- **Angular 18, Standalone Components**, Angular Material מינימלי, ממשק בעברית מימין לשמאל.
- **הפרדת אחריות:** `RequestsApiService` הוא המקום היחיד שמשתמש ב-`HttpClient`. ה-Components מקבלים ממנו Observables.
- **הכול בצד השרת:** כל שינוי בסינון, במיון או בעמוד שולח בקשה חדשה לשרת. ה-Client לא מסנן ולא ממיין.
- **שרשרת ה-RxJS של החיפוש:**
  ```
  שינוי בשדה ─► debounce (300ms לטקסט, 0 לבחירה) ─► distinctUntilChanged ─► query ─► switchMap(api.getRequests)
  ```
  - **debounce:** שדות טקסט (חיפוש, ארגון, מטפל) נשלחים רק אחרי שהמשתמש הפסיק להקליד ל-300ms (נבדק: 5 הקשות, בקשה אחת). בחירה בסטטוס, בעדיפות או בתאריך נשלחת מיד.
  - `distinctUntilChanged`: לא שולח שוב ערך זהה לקודם.
  - `switchMap`: מבטל את הבקשה הקודמת כשמגיעה חדשה, כך שתשובה ישנה ואיטית לא דורסת תוצאות חדשות.
- **מצב התצוגה ב-URL:** הסינון, המיון והעמוד נשמרים בכתובת (למשל `/?search=גליל&status=Waiting&page=2&sortBy=title&sortDirection=asc`), כך שרענון הדף או שליחת קישור מציגים את אותה תצוגה. ערכים לא תקינים ב-URL מתעלמים מהם ומשתמשים בברירת המחדל ([request-query-url.ts](frontend/src/app/features/requests/request-query-url.ts)).
- **מצבים:** Loading (spinner בטעינה הראשונה, פס התקדמות בטעינות הבאות), Empty ("לא נמצאו פניות"), Error (הודעה וכפתור "נסה שוב"), גם ב-Summary ובהיסטוריה.
- **עדכון סטטוס:** הדיאלוג מציג רק את המעברים המותרים. השרת נשאר הסמכות, ודוחה כל מעבר אחר ב-400. ה-409 מטופל כפי שתואר ב-[Concurrency](#concurrency).
- **ללא ממשק Bulk:** המבחן דורש את ה-Bulk ב-API בלבד.

---

## בדיקות אוטומטיות

```powershell
dotnet test backend/ServiceRequests.sln   # 19 Unit, 17 Integration
cd frontend; npm test                     # 13 Frontend
```

**CI:** כל push ל-`main` מריץ את כל הבדיקות ב-GitHub Actions ([ci.yml](.github/workflows/ci.yml)): ה-Backend מול SQL Server 2022 בקונטיינר על Linux, וה-Frontend ב-Chrome Headless, כולל Build של Production. התוצאה מוצגת בסימן שבראש הקובץ.

### Backend

| בדיקה | סוג | מה היא מוכיחה |
|---|---|---|
| `GetRequests_WithCombinedFiltersSortingAndPaging_ReturnsCorrectPagesFromDatabase` | Integration | סינון משולב, מיון ודפדוף נכונים מול DB אמיתי (`totalCount`, `totalPages`, עמוד 1 ו-2) |
| `GetRequests_WithSearch_MatchesTitleOrOrganizationPrefix`, `GetRequests_WithHebrewSearch_...` | Integration | חיפוש prefix בכותרת ובארגון, כולל בעברית; מילה מאמצע הכותרת לא נמצאת |
| `GetRequests_WithDateRange_...` | Integration | סינון לפי טווח תאריכים |
| `GetRequests_WithInvalidParameters_Returns400WithFieldError` (6 מקרים) | Integration | `pageSize` מעל 100, `page=0`, `sortBy` ו-`sortDirection` לא חוקיים, סטטוס לא קיים, טווח תאריכים הפוך |
| `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsOneConflictsAndOneAuditRow` | Integration | שני עדכונים בו-זמניים: 200 אחד, 409 אחד, רשומת Audit אחת |
| `UpdateStatus_ForbiddenTransition_Returns400AndCreatesNoAuditRow` | Integration | מעבר אסור מחזיר 400 בלי Audit |
| `UpdateStatus_ValidTransition_...`, `UpdateStatus_UnknownRequest_Returns404` | Integration | עדכון תקין (סטטוס, `rowVersion`, `UpdatedAt`, היסטוריה); 404 |
| `BulkUpdateStatus_MixedItems_ProcessesEachItemIndependently` | Integration | Bulk עם הצלחות, NotFound, Conflict ו-InvalidTransition: כל פריט עובד בנפרד |
| `BulkUpdateStatus_InvalidRequest_Returns400WithErrorPerField` | Integration | יותר מ-100 פריטים, ו-`rowVersion` לא תקין בפריט (`items[0].rowVersion`) |
| `Summary_AfterStatusUpdate_ReturnsFreshCountsInsteadOfCachedOnes` | Integration | Invalidation של ה-Cache |
| `StatusTransitionValidatorTests`, `ServiceRequestTests` | Unit | כללי המעבר ו-`ChangeStatus` (כולל אותו סטטוס ו-Completed סופי) |
| `RecompileQueryInterceptorTests` | Unit | `OPTION (RECOMPILE)` נוסף רק לשאילתות החיפוש |

בדיקות ה-Integration מריצות את ה-API האמיתי בזיכרון (`WebApplicationFactory`) מול SQL Server אמיתי, ב-Database ייעודי לבדיקות. כל בדיקה מתחילה מ-Database ריק ומכניסה רק את הנתונים שהיא צריכה.

### Frontend

| קובץ | מה הוא מוכיח |
|---|---|
| [request-filters.component.spec.ts](frontend/src/app/features/requests/request-filters/request-filters.component.spec.ts) | 4 הקשות מהירות שולחות חיפוש **אחד** רק אחרי 300ms; ערך זהה לא נשלח פעמיים; בחירת סטטוס נשלחת מיד |
| [request-list.component.spec.ts](frontend/src/app/features/requests/request-list/request-list.component.spec.ts) | `switchMap` מבטל את הבקשה הקודמת ומציג רק את התוצאה האחרונה; סינון, מיון ועמוד משוחזרים מה-URL ונשלחים לשרת; מצב Error ו"נסה שוב" |
| [status-update.component.spec.ts](frontend/src/app/features/requests/status-update/status-update.component.spec.ts) | רק מעברים מותרים מוצגים; ה-`rowVersion` שהמשתמש ראה נשלח; 409 סוגר את הדיאלוג כ-`conflict` לרענון; 400 משאיר אותו פתוח עם הודעה |
| [request-query-url.spec.ts](frontend/src/app/features/requests/request-query-url.spec.ts) | המצב שנכתב ל-URL משוחזר בדיוק; ערכים לא תקינים חוזרים לברירת המחדל |

---

## שימוש בכלי AI

**כלים:**
- Claude Code (מודל Claude Opus 5.5) כתוסף ב-VS Code: כתיבת הקוד, הרצת בדיקות ומדידות.
- Claude (צ'אט): ייעוץ בקבלת החלטות ובבדיקת התוצרים בכל שלב.

**אופן העבודה:**
- הכנתי, בעזרת Claude (צ'אט), קובץ הנחיות מפורט (מבנה הפתרון, עקרונות, סדר שלבים) על בסיס דרישות המבדק.
- העבודה נעשתה ב-13 שלבים. בסוף כל שלב קיבלתי סיכום של מה נבנה ואיך נבדק, ואישרתי או ביקשתי שינויים לפני המעבר לשלב הבא.
- כל שלב נשמר ב-commit נפרד, כך שאפשר לעקוב אחרי התהליך בהיסטוריית ה-Git.
- סדר 13 השלבים הוגדר מראש, וה-commit שסגר כל שלב מתועדים ב-WORK_PLAN.md, בסעיף "התוכנית המקורית".
- אחרי השלמת 13 השלבים ביקשתי הערכה לפי טבלת הניקוד, ובעקבותיה סבב שיפורים: בדיקות Frontend, שמירת מצב הסינון ב-URL, אחידות בהודעות השגיאה, חיזוק תוכנית העבודה והרצת הבדיקות אוטומטית ב-GitHub Actions.

**באילו חלקים נעזרתי ב-AI:**
- כתיבת קוד ה-Backend, ה-Frontend והבדיקות האוטומטיות.
- הרצת מדידות הביצועים וניתוח תוכניות הביצוע.
- ניסוח מסמכי התיעוד.

**החלטות שקיבלתי במהלך העבודה:**
- בחירת SQL Server 2019 Express ו-Angular 18 לפי מה שמותקן בסביבה, בלי שדרוגים מיותרים.
- חיפוש prefix במקום Full-Text: פשוט, יציב לבדיקות, והמגבלה מתועדת.
- דרשתי שבדיקת ה-Concurrency תשלח שני עדכונים בו-זמניים באמת, ותוודא שנוצרה רשומת Audit אחת בלבד.
- ביקשתי בדיקת Integration ל-Bulk עם תוצאות מעורבות.
- שינוי נתוני ה-Seed לעברית, כדי שיתאימו לממשק.
- מימוש OPTION (RECOMPILE) לחיפוש בלבד, והשארת האינדקס המורכב וה-Keyset כהצעות מתועדות.
- ויתור על docker-compose שלא ניתן לבדוק, ובמקומו הוראות הרצה שנבדקו על עותק נקי.
- הסרת טענות לא מבוססות מהתיעוד: רק מספרים שנמדדו בפועל.

**מה בדקתי בעצמי:**
- הרצתי את המערכת ובדקתי במסך חיפוש, סינון, מיון, דפדוף, עדכון סטטוס והיסטוריה.
- בדקתי את תרחיש ה-409 בשני טאבים.
- עברתי על הקוד ועל מסמכי התיעוד.
