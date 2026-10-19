namespace Model
{
    /// <summary>
    /// שמות התפקידים כפי שהם נשמרים בעמודה Role בטבלת Users.
    /// משתמשים בקבועים האלה במקום לכתוב את המחרוזת ידנית, כדי שטעות הקלדה לא תעבור.
    /// </summary>
    public static class Roles
    {
        public const string Parent = "Parent";         // הורה
        public const string Manager = "Manager";       // מנהל מתנס
        public const string SuperAdmin = "SuperAdmin"; // מנהל מערכת

        public static readonly string[] All = { Parent, Manager, SuperAdmin };

        public static bool IsValid(string? role)
        {
            return role != null && All.Contains(role);
        }
    }
}