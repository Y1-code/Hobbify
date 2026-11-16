using Microsoft.AspNetCore.Components;
using Model;
using View.Services;

namespace View.Components.Shared
{
    /// <summary>
    /// נוסף בשלב 8. מחלקת הבסיס של כל הדפים המוגנים באתר.
    /// בדיקת ההרשאה כתובה כאן פעם אחת, וכל דף מוגן יורש אותה במקום להעתיק אותה.
    ///
    /// דף מוגן כותב בראשו: @inherits ProtectedPage
    /// ודורס את התכונה AllowedRole, כדי לקבוע איזה תפקיד מורשה להיכנס אליו.
    ///
    /// abstract: אי אפשר ליצור "דף מוגן" סתם, רק דף מסוים שיורש מהמחלקה.
    /// ההגנה כאן היא בתצוגה בלבד: היא קובעת מה המשתמש רואה, ולא מגינה על ה-API.
    /// </summary>
    public abstract class ProtectedPage : ComponentBase
    {
        // השירותים שהבדיקה צריכה. [Inject] בקובץ C# הוא כמו @inject בדף Razor.
        // הם protected, ולכן גם הדפים שיורשים יכולים להשתמש בהם (למשל Session.FullName).
        [Inject] protected UserSession Session { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        // התפקיד שמורשה להיכנס לדף. תכונה מופשטת: כל דף שיורש חייב לדרוס אותה.
        protected abstract string AllowedRole { get; }

        // האם מותר להציג את התוכן: התפקיד של המשתמש המחובר זהה לתפקיד שהדף מרשה.
        // הדף בודק אותה בתנאי @if (IsAllowed) לפני שהוא מציג את התוכן.
        protected bool IsAllowed
        {
            get { return Session.Role == AllowedRole; }
        }

        // רצה כשהדף נפתח, לפני שהוא מוצג:
        // אין משתמש מחובר - עוברים לדף ההתחברות.
        // יש משתמש מחובר, אבל התפקיד שלו לא מתאים - עוברים לדף "אין הרשאה".
        protected override void OnInitialized()
        {
            if (!Session.IsLoggedIn)
            {
                Navigation.NavigateTo("login");
            }
            else if (!IsAllowed)
            {
                Navigation.NavigateTo("no-access");
            }
        }
    }
}
