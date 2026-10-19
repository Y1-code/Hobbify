using System.ComponentModel.DataAnnotations;

namespace Model
{
    // מחלקות קטנות שמתארות את מה שהתצוגה שולחת ל-API.
    // התכונות בסוגריים המרובעים ([Required] וכו') נבדקות אוטומטית ב-API:
    // אם משהו חסר או לא תקין, ה-API מחזיר 400 עם הסבר, והקוד בכלל לא רץ.

    /// <summary>הרשמה של הורה חדש.</summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "יש להזין שם מלא")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "יש להזין אימייל")]
        [EmailAddress(ErrorMessage = "כתובת האימייל לא תקינה")]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "יש להזין סיסמה")]
        [MinLength(6, ErrorMessage = "הסיסמה צריכה להכיל לפחות 6 תווים")]
        public string Password { get; set; } = "";
    }

    /// <summary>התחברות עם אימייל וסיסמה.</summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "יש להזין אימייל")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "יש להזין סיסמה")]
        public string Password { get; set; } = "";
    }

    /// <summary>עדכון פרטים אישיים (שם וטלפון).</summary>
    public class UpdateDetailsRequest
    {
        [Required(ErrorMessage = "יש להזין שם מלא")]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }
    }
}