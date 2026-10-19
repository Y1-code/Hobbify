namespace Model
{
    /// <summary>
    /// משתמש במערכת: הורה, מנהל מתנס או מנהל מערכת.
    /// מחלקה אחת לכל התפקידים, כי לכולם אותם נתונים וההבדל הוא רק במה שמותר להם.
    ///
    /// אין כאן סיסמה בכוונה: האובייקט הזה נשלח מה-API לתצוגה כ-JSON,
    /// והסיסמה (גם מוצפנת) לא צריכה לצאת מהשרת. היא מטופלת רק ב-UserDB.
    /// </summary>
    public class User : BaseEntity
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string Role { get; set; } = Roles.Parent;
        public bool HasUpdates { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// בנאי ריק: נדרש כדי ש-ASP.NET יוכל להמיר JSON לאובייקט.
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// משתמש חדש שעוד לא נשמר. המזהה, תאריך היצירה והדגלים HasUpdates ו-IsActive נקבעים בבסיס הנתונים.
        /// </summary>
        public User(string fullName, string email, string? phone, string role = Roles.Parent)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            Role = role;
        }

        /// <summary>
        /// משתמש שנקרא מהטבלה, עם כל העמודות. את המזהה מעבירים למחלקת הבסיס.
        /// </summary>
        public User(int id, string fullName, string email, string? phone,
                    string role, bool hasUpdates, bool isActive, DateTime createdAt)
            : base(id)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            Role = role;
            HasUpdates = hasUpdates;
            IsActive = isActive;
            CreatedAt = createdAt;
        }

        public override string ToString()
        {
            return $"{FullName} ({Role})";
        }
    }
}