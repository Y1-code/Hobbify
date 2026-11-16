using Model;

namespace View.Services
{
    /// <summary>
    /// נוסף בשלב 6. השירות שזוכר מי המשתמש המחובר, לאורך כל הגלישה באתר.
    ///
    /// למה הוא נמצא בתצוגה ולא ב-API: "מי מחובר עכשיו" הוא מידע של גולש מסוים בלשונית מסוימת.
    /// ה-API חסר מצב (Stateless): הוא עונה לכל בקשה ושוכח, ולא זוכר אף גולש בין בקשה לבקשה.
    ///
    /// השירות רשום ב-Program.cs עם AddScoped: עצם אחד לכל לשונית בדפדפן.
    /// כל הדפים באותה לשונית מקבלים את אותו עצם, ולכן כולם רואים את אותו משתמש.
    /// </summary>
    public class UserSession
    {
        // פרטי המשתמש המחובר. קוראים אותם מכל דף, אבל משנים אותם רק דרך Login ו-Logout (private set).
        // אין כאן סיסמה: אחרי ההתחברות אין בה צורך, ולכן לא שומרים אותה.
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

        // נוסף בשלב 8: הכתובת של הדף הראשי של המשתמש, לפי התפקיד שלו.
        // דף ההתחברות משתמש בה כדי להעביר כל משתמש לדף הראשי שלו.
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

        // אירוע שמודיע שהמשתמש המחובר השתנה. הסרגל העליון נרשם אליו כדי להתעדכן.
        public event Action? OnChange;

        // נקראת מדף ההתחברות, אחרי שה-API אישר את האימייל והסיסמה
        public void Login(User user)
        {
            UserId = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            RoleId = user.RoleId;
            Role = user.Role;
            NotifyChanged();
        }

        // נקראת מכפתור ההתנתקות. מחזירה את השירות למצב של אורח.
        public void Logout()
        {
            UserId = 0;
            FullName = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            Role = string.Empty;
            NotifyChanged();
        }

        // מפעילה את האירוע. קודם בודקים שמישהו נרשם אליו: אירוע שאף אחד לא נרשם אליו הוא null.
        private void NotifyChanged()
        {
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }
    }
}
