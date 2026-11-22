using Microsoft.AspNetCore.Components;
using Model;
using View.Services;

namespace View.Components.Shared
{
    /// <summary>
    /// מחלקת הבסיס של כל הדפים המוגנים באתר
    /// בדיקת ההרשאה כתובה כאן פעם אחת, וכל דף מוגן יורש אותה במקום להעתיק
    ///
    /// איך כותבים דף מוגן, שני דברים
    ///     @inherits ProtectedPage
    ///     protected override string AllowedRole
    ///     {
    ///         get { return Roles.Parent; }
    ///     }
    ///
    /// abstract
    ///     אי אפשר ליצור דף מוגן סתם, רק דף מסוים שיורש מהמחלקה
    ///     וכל דף שיורש חייב להגדיר איזה תפקיד מורשה להיכנס אליו
    ///
    /// Token
    ///     ההגנה כאן היא בתצוגה בלבד, והיא קובעת רק מה המשתמש רואה
    ///     להגנה אמיתית על השרת צריך אסימון שהתצוגה שולחת עם כל בקשה
    /// </summary>
    public abstract class ProtectedPage : ComponentBase
    {
        // Inject
        //     מביא את השירות שנרשם בקובץ ההפעלה
        //     בדף רגיל אותו דבר נכתב כך
        //         @inject UserSession Session
        // protected
        //     כדי שגם דפים שיורשים יוכלו להשתמש בשירותים
        [Inject] protected UserSession Session { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;

        // התפקיד שמורשה להיכנס לדף, כל דף שיורש חייב להגדיר אותו
        protected abstract string AllowedRole { get; }

        // האם מותר להציג את התוכן, כשהתפקיד של המשתמש זהה לתפקיד שהדף מרשה
        // הדף בודק אותה לפני שהוא מציג את התוכן
        //     @if (IsAllowed)
        protected bool IsAllowed
        {
            get { return Session.Role == AllowedRole; }
        }

        // OnInitialized
        //     רצה אוטומטית כשהדף נפתח, לפני שהוא מוצג
        //     אורח עובר לדף ההתחברות, ותפקיד לא מתאים עובר לדף אין הרשאה
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
