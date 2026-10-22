# רשימת בדיקה מול המבדק

לכל דרישה במבדק: איפה היא מומשה (קובץ, Endpoint או מסמך) ואיך היא נבדקה. נתיבי הקבצים יחסיים לשורש הריפו.

**מקרא:** ✅ מומש ונבדק · ⏳ פתוח, לביצוע לפני ההגשה

---

## המשימה ומבנה הפנייה

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ `Id`, `Title`, `OrganizationName` | [ServiceRequest.cs](../backend/src/ServiceRequests.Domain/Entities/ServiceRequest.cs) | Migration, Integration tests |
| ✅ `Status`: New, InProgress, Waiting, Completed | [RequestStatus.cs](../backend/src/ServiceRequests.Domain/Enums/RequestStatus.cs) | Unit tests |
| ✅ `Priority`: Low, Medium, High | [RequestPriority.cs](../backend/src/ServiceRequests.Domain/Enums/RequestPriority.cs) | Integration tests |
| ✅ `AssignedTo`, `CreatedAt`, `UpdatedAt` (UTC) | [ServiceRequest.cs](../backend/src/ServiceRequests.Domain/Entities/ServiceRequest.cs), [UtcDateTimeConverter.cs](../backend/src/ServiceRequests.Infrastructure/Persistence/UtcDateTimeConverter.cs) | `UpdateStatus_ValidTransition_...` |
| ✅ `RowVersion` לזיהוי עדכון מתחרה | `rowversion` ב-[ServiceRequestConfiguration.cs](../backend/src/ServiceRequests.Infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs) | בדיקת Concurrency |
| ✅ לפחות 100,000 רשומות | [DatabaseSeeder.cs](../backend/src/ServiceRequests.Infrastructure/Seeding/DatabaseSeeder.cs), [ServiceRequestDataGenerator.cs](../backend/src/ServiceRequests.Infrastructure/Seeding/ServiceRequestDataGenerator.cs) | 100,000 רשומות ב-DB |
| ✅ בחירת בסיס נתונים | SQL Server; נימוק ב-[DECISIONS.md §1](DECISIONS.md#1-בסיס-נתונים-sql-server) | |
| ✅ יצירה חוזרת של נתוני הבדיקה | `dotnet run --project backend/src/ServiceRequests.Api -- seed` ([README שלב 3](../README.md#שלב-3-יצירת-בסיס-הנתונים-ו-100000-פניות-seed)) | הורץ פעמים רבות, כולל מ-clone נקי |

## 1. שליפת נתונים

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ REST API ב-.NET | `GET /api/requests`, [RequestsController.cs](../backend/src/ServiceRequests.Api/Controllers/RequestsController.cs) | |
| ✅ אין העברה של כל הרשומות ל-Client | [RequestService.cs](../backend/src/ServiceRequests.Application/Services/RequestService.cs): `IQueryable` עד `ToListAsync` | לוג ה-SQL: `OFFSET/FETCH` |
| ✅ Pagination בצד השרת | `page`, `pageSize`, `PagedResult<T>` | `GetRequests_WithCombinedFiltersSortingAndPaging_...` |
| ✅ סינון משולב: Status, Priority, OrganizationName, AssignedTo, טווח תאריכים | `RequestService.ApplyFilters` | `..._WithCombinedFilters...`, `..._WithDateRange_...` |
| ✅ חיפוש ב-Title וב-OrganizationName | `search` (prefix) | `..._WithSearch_...`, `..._WithHebrewSearch_...` |
| ✅ מיון לפי לפחות 3 שדות, Asc/Desc | `sortBy`: createdAt, updatedAt, priority, title; `sortDirection` | `..._WithCombinedFilters...` ובדיקת דפדפן |
| ✅ לפחות שני Aggregations | `GET /api/requests/summary`: לפי Status ולפי Priority, [RequestSummaryService.cs](../backend/src/ServiceRequests.Application/Services/RequestSummaryService.cs) | השוואה ל-SQL ישיר, `SummaryCacheTests` |
| ✅ הגבלת Page Size ו-Validation לפרמטרים | [RequestQueryParametersValidator.cs](../backend/src/ServiceRequests.Application/Validators/RequestQueryParametersValidator.cs) (מקסימום 100) | `GetRequests_WithInvalidParameters_Returns400WithFieldError` (6 מקרים) |
| ✅ `CancellationToken` בפעולות I/O | מה-Controller ועד כל `...Async` של EF | ביטול בקשה באמצע מחזיר 499 |
| ✅ DTOs ולא ישויות DB | [Dtos/](../backend/src/ServiceRequests.Application/Dtos/), [ServiceRequestMapping.cs](../backend/src/ServiceRequests.Application/Mapping/ServiceRequestMapping.cs) | |
| ✅ תיעוד: איך נמנעת טעינה לזיכרון ואיך האינדקסים תומכים | [PERFORMANCE.md](PERFORMANCE.md#איך-נמנעת-טעינת-כל-הנתונים-לזיכרון), [README](../README.md#בסיס-הנתונים-והאינדקסים) | |

## 2. עדכון סטטוס ותחרותיות

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ פנייה שאינה קיימת: קוד HTTP מתאים | 404 ב-`PATCH /api/requests/{id}/status` | `UpdateStatus_UnknownRequest_Returns404` |
| ✅ סטטוס לא חוקי או מעבר אסור: Validation ברור | 400; [StatusTransitionValidator.cs](../backend/src/ServiceRequests.Domain/StatusTransitionValidator.cs), [UpdateStatusRequestValidator.cs](../backend/src/ServiceRequests.Application/Validators/UpdateStatusRequestValidator.cs) | `UpdateStatus_ForbiddenTransition_...`, Unit tests |
| ✅ Optimistic Concurrency: רק עדכון אחד מצליח, השני מקבל Conflict | 409; [RequestStatusService.cs](../backend/src/ServiceRequests.Application/Services/RequestStatusService.cs) (`WHERE RowVersion = @original`) | `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_...` (בו-זמני, 10 הרצות) |
| ✅ `UpdatedAt` ו-`RowVersion` מתעדכנים | `ChangeStatus` ו-`rowversion` של SQL Server | `UpdateStatus_ValidTransition_...` |
| ✅ הימנעות מ-Lost Update | האכיפה ב-DB, לא רק ב-UI | בבדיקה הבו-זמנית ה-409 נוצר ב-DB |

## 3. היסטוריית שינויים

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ Audit: RequestId, PreviousStatus, NewStatus, ChangedAt, ChangedBy | [RequestStatusHistory.cs](../backend/src/ServiceRequests.Domain/Entities/RequestStatusHistory.cs), נשמר באותה טרנזקציה | רשומת Audit אחת בעדכון בו-זמני; אין Audit במעבר אסור |
| ✅ Endpoint להיסטוריה | `GET /api/requests/{id}/history` (מהחדש לישן, 404) | `UpdateStatus_ValidTransition_...`, בדיקת דפדפן |

## 4. פעולת Bulk

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ עדכון מספר פניות, עד 100 | `PATCH /api/requests/status/bulk`, [BulkUpdateStatusRequestValidator.cs](../backend/src/ServiceRequests.Application/Validators/BulkUpdateStatusRequestValidator.cs) | 101 פריטים מחזירים 400, 100 מתקבלים |
| ✅ התנהגות מוגדרת כשחלק לא קיימות או בהתנגשות | Partial Success: תוצאה לכל פריט וסיכום | `BulkUpdateStatus_MixedItems_ProcessesEachItemIndependently` |
| ✅ נימוק הבחירה ותשובה ברורה | [README](../README.md#bulk-update), [DECISIONS.md §3](DECISIONS.md#3-bulk-partial-success-ו-200-גם-כשחלק-נכשל) | |

## 5. Angular

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ חיפוש, סינון, מיון ודפדוף בצד השרת | [request-list.component.ts](../frontend/src/app/features/requests/request-list/request-list.component.ts) | בדפדפן: כל שינוי שולח בקשה עם הפרמטרים |
| ✅ Debounce לחיפוש | 300ms לשדות טקסט ב-[request-filters.component.ts](../frontend/src/app/features/requests/request-filters/request-filters.component.ts) | בדפדפן: 5 הקשות, בקשה אחת; [request-filters.component.spec.ts](../frontend/src/app/features/requests/request-filters/request-filters.component.spec.ts) |
| ✅ ביטול או החלפה של בקשות קודמות | `switchMap` ב-`RequestListComponent` | בדפדפן: `ERR_ABORTED` לבקשה הקודמת; [request-list.component.spec.ts](../frontend/src/app/features/requests/request-list/request-list.component.spec.ts) |
| ✅ Loading, Empty ו-Error | `RequestListComponent`, `RequestSummaryComponent`, `RequestHistoryDialogComponent` | בדפדפן; Error ו"נסה שוב" גם ב-`request-list.component.spec.ts` |
| ✅ עדכון סטטוס וטיפול ב-409 | [status-update.component.ts](../frontend/src/app/features/requests/status-update/status-update.component.ts) | בדפדפן: עדכון מקביל, הודעה ורענון; [status-update.component.spec.ts](../frontend/src/app/features/requests/status-update/status-update.component.spec.ts) |
| ✅ הצגת Aggregations | [request-summary.component.ts](../frontend/src/app/features/requests/request-summary/request-summary.component.ts) | מתעדכן אחרי עדכון |
| ✅ צפייה בהיסטוריה | [request-history-dialog.component.ts](../frontend/src/app/features/requests/request-history-dialog/request-history-dialog.component.ts) | בדפדפן |
| ✅ הפרדה בין Components ל-Services | [requests-api.service.ts](../frontend/src/app/core/services/requests-api.service.ts) הוא היחיד שמשתמש ב-`HttpClient` | |
| ✅ (מעבר לנדרש) מצב התצוגה ב-URL | [request-query-url.ts](../frontend/src/app/features/requests/request-query-url.ts) | בדפדפן: רענון מחזיר את אותה תצוגה; [request-query-url.spec.ts](../frontend/src/app/features/requests/request-query-url.spec.ts) |

## 6. ביצועים

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ לפחות שתי פעולות API על 100K רשומות | רשימה מסוננת, חיפוש, Summary ועמוד עמוק, ב-[PERFORMANCE.md](PERFORMANCE.md) | [Measure-Queries.ps1](performance/Measure-Queries.ps1) |
| ✅ Execution Plan או מדד מקביל | Actual plans ב-[plans.txt](performance/plans.txt); Logical reads וזמנים | |
| ✅ פירוט האינדקסים והסיבה | [PERFORMANCE.md](PERFORMANCE.md#האינדקסים-ולמה) | לפני ואחרי אינדקסים |
| ✅ Bottleneck אפשרי והצעת שיפור | תוכנית שמורה בחיפוש (**תוקן**, `OPTION (RECOMPILE)`); אינדקס מורכב ו-Keyset (מוצעים, נמדדו) | מדידה חוזרת אחרי התיקון |
| ✅ אין טעינה מלאה לפני Pagination או Filtering | `IQueryable`, `COUNT` ו-`GROUP BY` ב-DB | |

## 7. Cache

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ Cache לנתון מסכם | `IMemoryCache` על ה-Summary, [RequestSummaryService.cs](../backend/src/ServiceRequests.Application/Services/RequestSummaryService.cs) | Miss 157ms, Hit כ-10ms |
| ✅ Expiration | Absolute, 60 שניות | נבדק: miss אחרי 62 שניות |
| ✅ Invalidation אחרי עדכון | מחיקת המפתח אחרי עדכון בודד או Bulk | `Summary_AfterStatusUpdate_ReturnsFreshCountsInsteadOfCachedOnes` |
| ✅ נימוק לבחירה | [README](../README.md#cache-ו-invalidation), [DECISIONS.md §5](DECISIONS.md#5-cache-רק-ה-summary-ב-imemorycache) | |
| ✅ התאמה לכמה מופעי שרת | Redis / `IDistributedCache`, או Pub/Sub ל-Invalidation ([README](../README.md#cache-ו-invalidation)) | |

## 8. בדיקות אוטומטיות

| דרישה | איפה | נבדק |
|---|---|---|
| ✅ לפחות 4 בדיקות משמעותיות, לפחות אחת Integration | 49 בדיקות: Backend 19 Unit ו-17 Integration, Frontend 13 | `dotnet test backend/ServiceRequests.sln`, `cd frontend; npm test` |
| ✅ תרחיש תקין של שליפה, סינון או עדכון | `GetRequests_WithCombinedFilters...`, `UpdateStatus_ValidTransition_...` | |
| ✅ Validation או קלט לא תקין | `GetRequests_WithInvalidParameters_...`, `UpdateStatus_ForbiddenTransition_...` | |
| ✅ Concurrency Conflict | `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_...` | |
| ✅ Integration עם API ובסיס נתונים | [ApiFactory.cs](../backend/tests/ServiceRequests.IntegrationTests/Infrastructure/ApiFactory.cs): `WebApplicationFactory` ו-SQL Server אמיתי | |

## 9. תכנון עבודה וחלוקה למשימות

| דרישה | איפה |
|---|---|
| ✅ חלוקה ל-Features ו-Tasks | [WORK_PLAN.md](WORK_PLAN.md): 10 Features, 35 משימות, ו-4 User Stories עם קריטריוני קבלה |
| ✅ מה נדרש ומה התוצר לכל משימה | עמודות "מה נדרש" ו"תוצר" |
| ✅ סדר ביצוע ותלויות | עמודת "תלויות", דיאגרמה וסעיף "סדר ביצוע" |
| ✅ הערכת מאמץ | עמודת "שעות", 77 שעות בסך הכול |
| ✅ קשר לדרישות המבדק | עמודת "סעיף במבדק" |
| ✅ משימות שאינן קוד | תכנון DB (T1.2), בדיקות (F6), ביצועים (F8), תיעוד והיערכות להרצה (F9) |

## 10. איכות הקוד

| דרישה | איפה |
|---|---|
| ✅ מבנה פרויקט והפרדת אחריות | 4 פרויקטים: Api, Application, Domain, Infrastructure ([README](../README.md#מבנה-הפתרון)) |
| ✅ קוד קריא, Naming עקבי, בלי שכפול | לוגיקת עדכון אחת לבודד ול-Bulk (`ApplyStatusChangeAsync`); whitelist אחד למיון; Build בלי אזהרות |
| ✅ DTOs ו-Mapping | `Dtos/`, `ServiceRequestMapping` (Expression אחד, גם ל-SQL וגם לזיכרון) |
| ✅ Dependency Injection ו-Lifetimes | `DbContext` ו-Services הם Scoped; `IMemoryCache` ו-`TimeProvider` הם Singleton |
| ✅ עבודה אסינכרונית | `async` לכל I/O, כולל `CancellationToken` |
| ✅ Global Error Handling / ProblemDetails | [GlobalExceptionHandler.cs](../backend/src/ServiceRequests.Api/ErrorHandling/GlobalExceptionHandler.cs) |
| ✅ Validation עקבי | FluentValidation דרך [ValidationFilter.cs](../backend/src/ServiceRequests.Api/Filters/ValidationFilter.cs), פורמט שגיאה אחד |
| ✅ Logging בלי מידע רגיש | עדכון, Conflict, Bulk, Cache, Seeding; רק מזהים, סטטוסים וכמויות |
| ✅ הימנעות מ-Over-Engineering | בלי MediatR, CQRS או Repository גנרי ([DECISIONS.md §6](DECISIONS.md#6-ארכיטקטורה-ארבעה-פרויקטים-בלי-שכבות-מיותרות)) |

## 11. תיעוד הפתרון

| דרישה | איפה |
|---|---|
| ✅ הוראות להבנה ולהפעלה | [README: הרצה צעד אחר צעד](../README.md#הרצה-צעד-אחר-צעד) ופתרון תקלות; נבדק על clone נקי |
| ✅ טכנולוגיות וגרסאות | [README: טכנולוגיות וגרסאות](../README.md#טכנולוגיות-וגרסאות) |
| ✅ מבנה הפתרון | [README: מבנה הפתרון](../README.md#מבנה-הפתרון) |
| ✅ בחירת בסיס הנתונים והאינדקסים | [README](../README.md#בסיס-הנתונים-והאינדקסים), [DECISIONS.md §1](DECISIONS.md), [PERFORMANCE.md](PERFORMANCE.md) |
| ✅ הסבר על Concurrency ועל Bulk | [README: Concurrency](../README.md#concurrency), [README: Bulk Update](../README.md#bulk-update) |
| ✅ הסבר על Cache ו-Invalidation | [README: Cache ו-Invalidation](../README.md#cache-ו-invalidation) |
| ✅ שתי החלטות טכנולוגיות וחלופות | [DECISIONS.md](DECISIONS.md): 9 החלטות |
| ✅ מגבלה ידועה ושיפור אפשרי | [DECISIONS.md](DECISIONS.md#מגבלות-ידועות): 6 מגבלות ו-6 שיפורים |
| ✅ פירוט שימוש בכלי AI | [README: שימוש בכלי AI](../README.md#שימוש-בכלי-ai) |

## אופן ההגשה

| דרישה | איפה |
|---|---|
| ⏳ ריפו נגיש לבודקים (לא ZIP) | ריפו ב-GitHub |
| ✅ אפשר לבחון את המערכת, הקוד, התכנון, המימוש וההחלטות | README עם הוראות שנבדקו, ותיקיית `docs/` |

---

## מיפוי לטבלת הניקוד

| תחום | משקל | איפה לבדוק |
|---|---|---|
| Backend: .NET / .NET Core | 17 | מבנה ב-4 שכבות, DI ו-Lifetimes, async ו-`CancellationToken`, `GlobalExceptionHandler`, כללי מעבר ב-Domain, Logging (§10 למעלה) |
| REST API | 13 | 5 Endpoints עם קודי HTTP מדויקים, Pagination, Filtering, Validation ו-Bulk ([README: API](../README.md#api-רשימת-endpoints)) |
| Angular | 15 | Components נפרדים, `RequestsApiService` יחיד, RxJS (debounce, distinct, switchMap), Server-side, מצבי UI, מצב ב-URL, 13 בדיקות Frontend (§5) |
| בסיס נתונים וביצועים | 13 | מודל, אינדקסים, Aggregations ב-DB, מדידות לפני ואחרי, ותיקון שנמצא במדידה ([PERFORMANCE.md](PERFORMANCE.md)) |
| Concurrency + Audit | 11 | `rowversion` שנאכף ב-DB, 409, Audit באותה טרנזקציה, בדיקה בו-זמנית (§2, §3) |
| תכנון עבודה וחלוקה למשימות | 8 | [WORK_PLAN.md](WORK_PLAN.md) |
| איכות קוד וחשיבה מערכתית | 9 | [DECISIONS.md](DECISIONS.md), §10 למעלה |
| Cache | 4 | בחירה, Expiration, Invalidation ו-Scale-out (§7) |
| בדיקות אוטומטיות | 6 | 49 בדיקות, כולל Integration מול DB אמיתי, Concurrency בו-זמני ובדיקות Frontend (§8) |
| תיעוד ואיכות ההגשה | 4 | README, `docs/`, ההחלטות, סעיף AI |

## דגשים להערכה

| דגש | איך הוא מטופל |
|---|---|
| סינון, מיון או דפדוף של כל הנתונים ב-Client | הכול בצד השרת; ה-Client מקבל עמוד אחד בלבד |
| Concurrency שמטופל רק ב-UI | נאכף ב-DB (`WHERE RowVersion`) |
| Cache בלי אסטרטגיית Invalidation | Invalidation אחרי כל עדכון, ו-Expiration כגבול עליון |
| חלוקת משימות שטחית | 35 משימות עם תלויות, תוצרים והערכות, כולל משימות שאינן קוד; User Stories עם קריטריוני קבלה, Definition of Done וניהול סיכונים |
| Over-Engineering | בלי שכבות שלא נדרשו; כל תוספת נומקה במדידה או בדרישה |
