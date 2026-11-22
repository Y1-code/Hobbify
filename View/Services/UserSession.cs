using Model;

namespace View.Services
{
    /// <summary>
    /// השירות שזוכר מי המשתמש המחובר, לאורך כל הגלישה באתר
    ///
    /// Stateless
    ///     השרת עונה לכל בקשה ושוכח, ולא זוכר אף גולש בין בקשה לבקשה
    ///     לכן מי שמחובר עכשיו נשמר כאן, בצד התצוגה, ולא בשרת
    ///
    /// AddScoped
    ///     השירות רשום בקובץ ההפעלה, עצם אחד לכל לשונית בדפדפן
    ///     כל הדפים באותה לשונית מקבלים את אותו עצם, ולכן כולם רואים את אותו משתמש
    /// </summary>
    public class UserSession
    {
        // פרטי המשתמש המחובר, כל דף יכול לקרוא אותם
        // private set
        //     משנים אותם רק דרך הפעולות של הכניסה והיציאה
        // אין כאן סיסמה, כי אחרי ההתחברות אין בה צורך
        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public string Role { get; private set; } = string.Empty;

        // תכונה מחושבת: מזהה של משתמש אמיתי תמיד גדול מ-0
        public bool IsLoggedIn
        {
            get { return UserId > 0; }
        }

        // הכתובת של הדף הראשי של המשתמש, לפי התפקיד שלו
        // דף ההתחברות משתמש בה כדי להעביר כל משתמש לדף הראשי שלו
        public string HomePage
        {
            get
            {
                if (Role == Roles.SuperAdmin)
                {
                    return "admin";
                }
                if (Role == Roles.Manager)
                {
                    return "manager";
                }
                if (Role == Roles.Parent)
                {
                    return "parent";
                }
                return ""; // אורח: דף הבית
            }
        }

        // אירוע שמודיע שהמשתמש המחובר השתנה. הסרגל העליון נרשם אליו כדי להתעדכן
        public event Action? OnChange;

        // נקראת מדף ההתחברות, אחרי שהשרת אישר את האימייל והסיסמה
        public void Login(User user)
        {
            UserId = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            RoleId = user.RoleId;
            Role = user.Role;
            NotifyChanged();
        }

        // נקראת מכפתור ההתנתקות. מחזירה את השירות למצב של אורח
        public void Logout()
        {
            UserId = 0;
            FullName = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            Role = string.Empty;
            NotifyChanged();
        }

        // מפעילה את האירוע, רק אם מישהו נרשם אליו
        private void NotifyChanged()
        {
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }
    }
}
