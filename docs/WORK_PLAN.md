# תוכנית עבודה

פירוק דרישות המבדק ל-Features ול-Tasks: מה נדרש בכל משימה, התוצר, התלויות, הערכת מאמץ, והסעיף במבדק שהיא מממשת. התוכנית כוללת גם משימות שאינן כתיבת קוד: תכנון בסיס הנתונים, ביצועים, בדיקות, תיעוד והיערכות להרצה.

סדר השלבים הוגדר לפני תחילת העבודה ([התוכנית המקורית](#התוכנית-המקורית)). פירוק המשימות המפורט והערכת המורכבות נכתבו בסוף העבודה, על בסיס מה שבוצע. בסוף המסמך יש טבלה של השינויים שנעשו במהלך העבודה לעומת הכיוון הראשוני.

## הערכת מאמץ

הערכת המאמץ היא יחסית, בשלוש רמות מורכבות:

| רמה | הגדרה | דוגמה |
|---|---|---|
| **נמוך** | שינוי פשוט ומוכר, בלי החלטות תכנון ובלי סיכון מיוחד | Aggregations עם `GROUP BY`, הקמת מבנה הפרויקט |
| **בינוני** | כמה חלקים שצריך לחבר, או החלטה אחת שצריך לנמק | Cache עם Invalidation, דיאלוג עדכון עם טיפול ב-409 |
| **גבוה** | מורכב, עם סיכון לטעות שקשה לזהות או עם החלטות תכנון משמעותיות | Optimistic Concurrency, Bulk עם Partial Success, ניתוח ביצועים |

## סיכום

| Feature | משימות | נמוך | בינוני | גבוה |
|---|---|---|---|---|
| F0: הכנה ותכנון | 3 | 2 | 1 | 0 |
| F1: מודל נתונים ובסיס נתונים | 4 | 1 | 2 | 1 |
| F2: API שליפה | 3 | 0 | 2 | 1 |
| F3: Aggregations ו-Cache | 2 | 1 | 1 | 0 |
| F4: עדכון סטטוס, Concurrency ו-Audit | 3 | 0 | 2 | 1 |
| F5: Bulk Update | 2 | 0 | 1 | 1 |
| F6: בדיקות אוטומטיות | 5 | 2 | 2 | 1 |
| F7: Angular | 6 | 2 | 4 | 0 |
| F8: ביצועים | 3 | 0 | 2 | 1 |
| F9: תיעוד והיערכות להגשה | 5 | 1 | 4 | 0 |
| **סה"כ** | **36** | **9** | **21** | **6** |

## User Stories וקריטריוני קבלה

כל קריטריון ניתן לבדיקה, ולצידו הבדיקה האוטומטית או הידנית שמוכיחה אותו.

**US1: איתור פניות (F2, F7).** מי שמטפל בפניות צריך למצוא במהירות פניות לפי סטטוס, עדיפות, ארגון, מטפל, תאריך או טקסט, מתוך 100,000 פניות.

| קריטריון קבלה | נבדק ב- |
|---|---|
| סינון משולב, מיון ודפדוף מתבצעים בשרת; ה-Client מקבל עמוד אחד בלבד | `GetRequests_WithCombinedFiltersSortingAndPaging_...` |
| חיפוש מוצא פניות שהכותרת או שם הארגון שלהן מתחילים בטקסט, גם בעברית | `GetRequests_WithSearch_...`, `GetRequests_WithHebrewSearch_...` |
| פרמטר לא תקין (למשל `pageSize=101`) מחזיר 400 עם שם השדה | `GetRequests_WithInvalidParameters_...` |
| הקלדה מהירה שולחת בקשה אחת; בקשה ישנה מבוטלת כשמגיעה חדשה | `request-filters.component.spec.ts`, `request-list.component.spec.ts` |
| מצבי Loading, Empty ו-Error מוצגים בבירור | בדפדפן; `request-list.component.spec.ts` |

**US2: עדכון סטטוס בלי לדרוס עבודה של אחרים (F4, F7).** כשכמה אנשים עובדים על אותן פניות, עדכון לא יכול למחוק בשקט עדכון של מישהו אחר.

| קריטריון קבלה | נבדק ב- |
|---|---|
| רק מעברים מותרים מתקבלים; מעבר אסור מחזיר 400 ולא נרשם ב-Audit | `UpdateStatus_ForbiddenTransition_...`, Unit tests |
| משני עדכונים בו-זמניים לאותה גרסה, אחד מצליח והשני מקבל 409; נרשמת רשומת Audit אחת | `UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_...` |
| ב-409 המשתמש רואה הודעה ברורה, והנתונים מתרעננים | `status-update.component.spec.ts`; בדפדפן בשני טאבים |
| כל שינוי נרשם בהיסטוריה, ואפשר לצפות בה מהחדש לישן | `UpdateStatus_ValidTransition_...`; בדפדפן |

**US3: עדכון של כמה פניות יחד (F5).** צריך לעדכן עד 100 פניות בבקשה אחת, ולדעת בדיוק מה הצליח.

| קריטריון קבלה | נבדק ב- |
|---|---|
| כל פריט מעובד בנפרד; פריט שנכשל לא מבטל את האחרים | `BulkUpdateStatus_MixedItems_...` |
| לכל פריט מוחזרת תוצאה (Succeeded / NotFound / InvalidTransition / Conflict), עם סיכום | `BulkUpdateStatus_MixedItems_...` |
| יותר מ-100 פריטים או פריט לא תקין מחזירים 400 | `BulkUpdateStatus_InvalidRequest_...` |

**US4: תמונת מצב (F3, F7).** צריך לראות כמה פניות יש בכל סטטוס ובכל עדיפות, בלי לחכות.

| קריטריון קבלה | נבדק ב- |
|---|---|
| הכמויות מחושבות ב-DB ותואמות את הנתונים | השוואה ל-SQL ישיר |
| התשובה נשמרת ב-Cache ל-60 שניות, ומתעדכנת מיד אחרי שינוי סטטוס | `Summary_AfterStatusUpdate_...`; מדידת miss מול hit |

## Definition of Done

משימה נחשבת גמורה כש:
1. הקוד מתקמפל בלי אזהרות.
2. כל הבדיקות עוברות, ונוספו בדיקות לקריטריוני הקבלה הרלוונטיים.
3. ההתנהגות נבדקה בפועל: מול ה-API, ב-DB או בדפדפן.
4. התיעוד הרלוונטי עודכן (README, DECISIONS או PERFORMANCE).
5. השינוי נשמר ב-commit עם הודעה שמתארת מה ולמה.

## סיכונים

| סיכון | השפעה | דרך התמודדות | מה קרה בפועל |
|---|---|---|---|
| אין Docker בסביבת הפיתוח | אין Testcontainers ואין docker-compose שנבדק | בדיקות מול Database ייעודי עם הגנה מפני מחיקת נתונים אמיתיים; הוראות הרצה מפורטות | נבחרה הדרך החלופית, וההוראות נבדקו על clone נקי |
| חיפוש טקסט חופשי לא משתמש באינדקס | סריקה מלאה בכל חיפוש | חיפוש prefix עם אינדקס | נמדד ותועד; נמצאה ותוקנה בעיית תוכנית שמורה (T8.3) |
| בדיקת Concurrency שלא באמת בו-זמנית | בדיקה שעוברת בלי להוכיח את ההגנה ב-DB | שחרור שתי הבקשות יחד, ובדיקה שה-409 נוצר ב-DB | 10 הרצות, בכולן ה-409 נוצר ב-`WHERE RowVersion` |
| ביצועי SQL Server לא יציבים במחשב הפיתוח | מספרי מדידה מטעים | Logical reads כמדד עיקרי, Median של 7 הרצות, מדידה על נתונים נקיים | תועד ב-PERFORMANCE.md; Timeout ייעודי לפקודת ה-Seed בלבד |
| הבודק לא מצליח להריץ את המערכת | פגיעה בהערכה | README עם הוראות צעד אחר צעד, טבלת פתרון תקלות, התחלה מהירה | נבדק על clone נקי |

## משימות

### F0: הכנה ותכנון

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T0.1 | בדיקת סביבה ובחירת כלים | לבדוק אילו כלים מותקנים (.NET, Node, Angular CLI, SQL Server, Docker) ולבחור לפיהם | החלטה על SQL Server, Angular וגישת הבדיקות | | נמוך | טכנולוגיות |
| T0.2 | מבנה הפתרון ו-Git | Solution עם 4 פרויקטים ו-2 פרויקטי בדיקות, הפניות בין השכבות, `.gitignore`, ריפו | Solution שמתקמפל, commit ראשון | T0.1 | נמוך | §10 |
| T0.3 | תוכנית עבודה | פירוק האפיון למשימות, תלויות והערכות | המסמך הזה | | בינוני | §9 |

### F1: מודל נתונים ובסיס נתונים

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T1.1 | מודל Domain וכללי מעבר | ישות `ServiceRequest`, ישות Audit, enums, כללי מעבר במקום אחד | `Domain/` | T0.2 | בינוני | המשימה, §2, §3 |
| T1.2 | תכנון בסיס הנתונים | טבלאות, `rowversion`, Foreign Key ל-Audit, תאריכים ב-UTC, ותכנון אינדקסים לפי תרחישי הסינון והמיון | רשימת אינדקסים ונימוק | T1.1 | גבוה | §1, §6 |
| T1.3 | DbContext ו-Migration | Configurations, אינדקסים, Migration ראשון | `Infrastructure/Persistence/` | T1.2 | נמוך | §6 |
| T1.4 | Seed של 100,000 רשומות | נתונים מגוונים, הכנסה מהירה (`SqlBulkCopy`), ניתן להרצה חוזרת | פקודת `seed` | T1.3 | בינוני | המשימה (100K, יצירה חוזרת) |

### F2: API שליפה

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T2.1 | חוזה ה-API ו-Validation | פרמטרי Pagination, סינון, חיפוש ומיון; whitelist למיון; הגבלת Page Size; שגיאות כ-`ProblemDetails` | `RequestQueryParameters`, Validator, Validation filter | T1.1 | בינוני | §1, §10 |
| T2.2 | שאילתה בצד השרת | `IQueryable` עד הסוף, Projection ל-DTO, `CountAsync` + `Skip/Take`, `CancellationToken` | `GET /api/requests` | T1.3, T2.1 | גבוה | §1 |
| T2.3 | טיפול בשגיאות ו-Logging | Global Exception Handler, Logging בנקודות משמעותיות | `GlobalExceptionHandler` | T0.2 | בינוני | §10 |

### F3: Aggregations ו-Cache

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T3.1 | Aggregations | כמויות לפי Status ולפי Priority, `GROUP BY` ב-DB | `GET /api/requests/summary` | T1.3 | נמוך | §1 |
| T3.2 | Cache | בחירת הנתון, Expiration, Invalidation, תכנון לכמה מופעי שרת | `RequestSummaryService` | T3.1, T4.1 (Invalidation) | בינוני | §7 |

### F4: עדכון סטטוס, Concurrency ו-Audit

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T4.1 | עדכון סטטוס | 404, 400 לסטטוס או מעבר לא חוקי, 200 עם DTO | `PATCH /api/requests/{id}/status` | T1.1, T2.1 | בינוני | §2 |
| T4.2 | Optimistic Concurrency | `rowVersion` מהלקוח, 409 ב-Conflict, הגנה מ-Lost Update ב-DB | בדיקה מוקדמת ו-`WHERE RowVersion` | T4.1 | גבוה | §2 |
| T4.3 | Audit והיסטוריה | רשומת Audit באותה טרנזקציה; Endpoint להיסטוריה | `GET /api/requests/{id}/history` | T4.1 | בינוני | §3 |

### F5: Bulk Update

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T5.1 | הגדרת ההתנהגות | בחירה בין All-or-Nothing ל-Partial Success, חוזה התשובה, קוד HTTP, נימוק | החלטה מתועדת | T4.2 | בינוני | §4 |
| T5.2 | מימוש | עד 100 פריטים, טעינה בשאילתה אחת, תוצאה לכל פריט, שימוש באותה לוגיקה כמו העדכון הבודד | `PATCH /api/requests/status/bulk` | T5.1, T4.3 | גבוה | §4 |

### F6: בדיקות אוטומטיות

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T6.1 | תשתית בדיקות | `WebApplicationFactory` מול SQL Server אמיתי, Database ייעודי, ניקוי בין בדיקות | `ApiFactory`, `IntegrationTestBase` | T2.2 | בינוני | §8 |
| T6.2 | שליפה ו-Validation | סינון, מיון ודפדוף נכונים; 400 לפרמטרים לא תקינים | `GetRequestsTests` | T6.1 | נמוך | §8 |
| T6.3 | Concurrency, Bulk ו-Cache | שני עדכונים בו-זמניים (200 ו-409, Audit אחד); מעבר אסור בלי Audit; Bulk מעורב; Invalidation | `UpdateStatusTests`, `BulkUpdateStatusTests`, `SummaryCacheTests` | T6.1, T5.2, T3.2 | גבוה | §8 |
| T6.4 | Unit | כללי מעבר הסטטוס | `StatusTransitionValidatorTests` | T1.1 | נמוך | §8 |
| T6.5 | בדיקות Frontend | debounce, ביטול בקשות ב-`switchMap`, טיפול ב-409, שחזור מצב מה-URL | 4 קובצי `*.spec.ts` (Karma ו-Jasmine) | T7.3, T7.4, T7.6 | בינוני | §5, §8 |

### F7: Angular

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T7.1 | הקמה ושכבת API | פרויקט, Material, models, `RequestsApiService` יחיד, proxy ל-API | `core/` | T2.2 | נמוך | §5 |
| T7.2 | רשימה | טבלה, מיון ודפדוף בצד השרת, מצבי Loading / Empty / Error | `RequestListComponent` | T7.1 | בינוני | §5 |
| T7.3 | סינון וחיפוש | Debounce, ביטול בקשות קודמות (`switchMap`) | `RequestFiltersComponent` | T7.2 | בינוני | §5 |
| T7.4 | עדכון סטטוס | דיאלוג, טיפול ב-409 עם הודעה ורענון | `StatusUpdateComponent` | T7.2, T4.2 | בינוני | §5 |
| T7.5 | Summary והיסטוריה | הצגת Aggregations ורענונם אחרי עדכון; דיאלוג היסטוריה | `RequestSummaryComponent`, `RequestHistoryDialogComponent` | T7.2, T3.1, T4.3 | נמוך | §5 |
| T7.6 | מצב התצוגה ב-URL | שמירת סינון, מיון ועמוד בכתובת ושחזורם ברענון, עם התעלמות מערכים לא תקינים | `request-query-url.ts` | T7.3 | בינוני | §5 |

### F8: ביצועים

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T8.1 | שיטת מדידה | ה-SQL המדויק של EF, השוואה עם ובלי אינדקסים, Logical reads, זמן ו-Execution Plan | סקריפט מדידה ניתן להרצה חוזרת | T1.4, T2.2, T3.1 | בינוני | §6 |
| T8.2 | מדידה וניתוח | לפחות שתי פעולות על 100K רשומות; זיהוי Bottlenecks והצעות שיפור, עם מדידה | `PERFORMANCE.md` | T8.1 | גבוה | §6 |
| T8.3 | תיקון מה שהמדידה מצאה | מימוש שיפור שמצדיק את עצמו, ומדידה חוזרת | תיקון ומספרים מעודכנים | T8.2 | בינוני | §6 |

### F9: תיעוד והיערכות להגשה

| מזהה | משימה | מה נדרש | תוצר | תלויות | מורכבות | סעיף במבדק |
|---|---|---|---|---|---|---|
| T9.1 | README והוראות הרצה | גרסאות, הרצה צעד אחר צעד, מבנה, Endpoints עם דוגמאות, Concurrency, Bulk, Cache. **בדיקת ההוראות על clone נקי** | `README.md` | כל ה-Features | בינוני | §11, אופן ההגשה |
| T9.2 | החלטות טכנולוגיות | החלטות, חלופות, מגבלה ושיפור | `DECISIONS.md` | כל ה-Features | בינוני | §11 |
| T9.3 | פירוט שימוש ב-AI | סעיף ב-README | סעיף השימוש בכלי AI | T9.1 | נמוך | §11, שימוש בכלי AI |
| T9.4 | מעבר סופי והגשה | מעבר על כל סעיף במבדק ועל טבלת הניקוד, ריפו ב-GitHub | רשימת בדיקה, קישור לריפו | הכול | בינוני | אופן ההגשה |
| T9.5 | CI | הרצת כל הבדיקות על כל push: Backend מול SQL Server בקונטיינר, Frontend ב-Chrome Headless | `.github/workflows/ci.yml`, סימן סטטוס ב-README | F6, T9.4 | בינוני | §8, אופן ההגשה |

## סדר ביצוע ותלויות

```mermaid
flowchart LR
    F0[F0 הכנה] --> F1[F1 מודל ו-DB]
    F1 --> F2[F2 API שליפה]
    F2 --> F3[F3 Summary ו-Cache]
    F2 --> F4[F4 עדכון ו-Concurrency]
    F4 --> F3
    F4 --> F5[F5 Bulk]
    F3 --> F6[F6 בדיקות]
    F5 --> F6
    F2 --> F7[F7 Angular]
    F4 --> F7
    F6 --> F8[F8 ביצועים]
    F7 --> F9[F9 תיעוד והגשה]
    F8 --> F9
```

1. **F0 → F1:** הסביבה, המבנה ומודל הנתונים הם הבסיס לכל השאר.
2. **F2:** השליפה היא הדרישה המרכזית, והיא מגדירה את ה-DTO וה-Validation שכל השאר משתמש בהם.
3. **F3, F4 ו-F5:** F4 לפני F5, כי ה-Bulk משתמש באותה לוגיקת עדכון. ה-Invalidation של F3 תלוי בעדכון של F4.
4. **F6:** הבדיקות אחרי שה-API יציב. בדיקת Unit לכללי המעבר נכתבה כבר עם F1.
5. **F7:** ה-Angular יכול להתחיל אחרי F2, במקביל ל-F3 עד F6. עדכון הסטטוס וההיסטוריה ב-UI תלויים ב-F4.
6. **F8:** מדידות רק אחרי שה-Seed, השליפה וה-Summary סופיים, ועל נתונים נקיים.
7. **F9:** התיעוד בסוף, כשההחלטות והמספרים סופיים. ההוראות נבדקות על clone נקי.

## התוכנית המקורית

לפני תחילת העבודה הוגדר סדר של 13 שלבים. כל שלב הסתיים בבדיקה (Build, בדיקות והרצה בפועל) וב-commit, כך שאפשר לעקוב אחרי הביצוע בהיסטוריית ה-Git.

| שלב | תוכן | Features | commit שסגר את השלב |
|---|---|---|---|
| 1 | בדיקת סביבה, יצירת Solution ומבנה פרויקטים, Git | F0 | [`5c5e6ae`](https://github.com/EsterWork/service-requests-management/commit/5c5e6ae) |
| 2 | Domain, DbContext, Configurations, אינדקסים, Migration | F1 | [`4dad29b`](https://github.com/EsterWork/service-requests-management/commit/4dad29b) |
| 3 | Seeding של 100K רשומות (ניתן להרצה חוזרת) | F1 | [`ed74231`](https://github.com/EsterWork/service-requests-management/commit/ed74231) |
| 4 | API שליפה: Pagination, סינון, חיפוש, מיון, Validation, ProblemDetails | F2 | [`72d24ad`](https://github.com/EsterWork/service-requests-management/commit/72d24ad) |
| 5 | Summary, Cache ו-Invalidation | F3 | [`56e9583`](https://github.com/EsterWork/service-requests-management/commit/56e9583) |
| 6 | עדכון סטטוס, Concurrency, Audit והיסטוריה | F4 | [`bb12bf1`](https://github.com/EsterWork/service-requests-management/commit/bb12bf1) |
| 7 | Bulk Update | F5 | [`15d9bda`](https://github.com/EsterWork/service-requests-management/commit/15d9bda) |
| 8 | בדיקות אוטומטיות | F6 | [`37acbb2`](https://github.com/EsterWork/service-requests-management/commit/37acbb2) |
| 9 | Angular: רשימה, סינון, מיון, דפדוף, Debounce, מצבים | F7 | [`2d5bcc3`](https://github.com/EsterWork/service-requests-management/commit/2d5bcc3) |
| 10 | Angular: עדכון סטטוס, 409, Summary, היסטוריה | F7 | [`40a7560`](https://github.com/EsterWork/service-requests-management/commit/40a7560) |
| 11 | מדידות ביצועים ו-PERFORMANCE.md | F8 | [`e7337f5`](https://github.com/EsterWork/service-requests-management/commit/e7337f5), [`b95ded2`](https://github.com/EsterWork/service-requests-management/commit/b95ded2) |
| 12 | README, WORK_PLAN, DECISIONS | F9 | [`38a1907`](https://github.com/EsterWork/service-requests-management/commit/38a1907), [`b89b715`](https://github.com/EsterWork/service-requests-management/commit/b89b715) |
| 13 | מעבר סופי מול המבדק ורשימת בדיקה | F9 | [`20b960a`](https://github.com/EsterWork/service-requests-management/commit/20b960a), [`8247d6e`](https://github.com/EsterWork/service-requests-management/commit/8247d6e), [`2c971e7`](https://github.com/EsterWork/service-requests-management/commit/2c971e7) |

אחרי 13 השלבים נוספו שיפורים שמתוארים בטבלת השינויים למטה (camelCase בשגיאות, מצב ב-URL, בדיקות Frontend ו-CI).

## שינויים במהלך העבודה

| שינוי | סיבה |
|---|---|
| חיפוש prefix ולא Full-Text | Full-Text מתעדכן באופן אסינכרוני (בדיקות לא יציבות) ולא נתמך ב-Docker image הרשמי ([DECISIONS.md](DECISIONS.md)) |
| סינון ארגון ומטפל כ-prefix ולא כהתאמה מדויקת | התגלה בבניית ה-UI: משתמש לא מקליד שם ארגון מלא |
| נתוני ה-Seed בעברית | הממשק בעברית, והמסך עם נתונים באנגלית לא נראה אחיד. נוספה בדיקת חיפוש בעברית |
| `OPTION (RECOMPILE)` לחיפוש (T8.3) | המדידות חשפו תוכנית שמורה שסורקת את כל הטבלה בחיפוש נדיר |
| בלי docker-compose | אין Docker בסביבת הפיתוח, וקובץ שלא נבדק עלול לא לעבוד אצל הבודק. במקומו, הוראות הרצה שנבדקו על clone נקי |
| בדיקות מול Database ייעודי ולא Testcontainers | אין Docker בסביבת הפיתוח |
| שמות שדות ב-camelCase בשגיאות Validation | חוסר אחידות שהתגלה במעבר הסופי: שאר ה-JSON היה camelCase והשגיאות PascalCase |
| debounce לשדות טקסט בלבד | התגלה במעבר הסופי: debounce על כל הטופס עיכב גם בחירה ב-select, שהיא פעולה אחת מכוונת |
| מצב התצוגה ב-URL ובדיקות Frontend (T6.5, T7.6) | נוספו במעבר הסופי כדי לחזק את ה-Angular ואת הבדיקות |
| CI ב-GitHub Actions (T9.5) | נוסף אחרי ההעלאה ל-GitHub: הבודק רואה שהבדיקות עוברות על מכונה נקייה, ובשרתי GitHub יש Docker לבדיקות מול SQL Server |
