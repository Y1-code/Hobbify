using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// כל השאילתות של טבלת Users, ורק הן.
    /// מחרוזת החיבור מגיעה מבחוץ (מה-API), ו-UserDB לא יודע מאיפה היא נקראה.
    ///
    /// כל ערך שמגיע מהמשתמש נכנס לשאילתה כפרמטר (@Email וכו') ולא מודבק לטקסט,
    /// כדי למנוע SQL Injection.
    /// סיסמאות נשמרות מוצפנות ב-BCrypt ואף פעם לא כטקסט רגיל.
    /// </summary>
    public class UserDB
    {
        private readonly string connectionString;

        // שאילתת בסיס אחת לכל השליפות, כדי שהעמודות יהיו תמיד באותו סדר.
        private const string SelectUsers = @"
            SELECT UserId, FullName, Email, Phone, Role, HasUpdates, IsActive, CreatedAt
            FROM Users";

        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // ---------- הוספה ובדיקות ----------

        /// <summary>
        /// מוסיפה משתמש ומחזירה את המזהה החדש שלו.
        /// הסיסמה מוצפנת כאן, לפני שהיא נשמרת.
        /// </summary>
        public async Task<int> AddUserAsync(User user, string password)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO Users (FullName, Email, Phone, PasswordHash, Role)
                VALUES (@FullName, @Email, @Phone, @PasswordHash, @Role);
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(password));
            command.Parameters.AddWithValue("@Role", user.Role);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        /// <summary>
        /// האם האימייל כבר רשום. ההשוואה לא מבדילה בין אותיות גדולות וקטנות.
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email;", connection);
            command.Parameters.AddWithValue("@Email", email);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
        }

        /// <summary>
        /// בדיקת התחברות. מחזירה את המשתמש אם האימייל והסיסמה נכונים והמשתמש פעיל, אחרת null.
        /// הסיסמה נבדקת מול ההצפנה ב-BCrypt, ולא בהשוואת טקסט ב-SQL.
        /// משתמש חסום (IsActive = FALSE) לא יכול להתחבר, גם עם סיסמה נכונה.
        /// </summary>
        public async Task<User?> CheckLoginAsync(string email, string password)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT UserId, FullName, Email, Phone, Role, HasUpdates, IsActive, CreatedAt, PasswordHash
                FROM Users
                WHERE Email = @Email AND IsActive = TRUE;";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null; // אין משתמש עם האימייל הזה
            }

            string hash = reader.GetString("PasswordHash");
            if (!BCrypt.Net.BCrypt.Verify(password, hash))
            {
                return null; // סיסמה שגויה
            }

            return ReadUser(reader);
        }

        // ---------- שליפות ----------

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(SelectUsers + " WHERE UserId = @UserId;", connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadUser(reader) : null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(SelectUsers + " ORDER BY UserId;", connection);

            var users = new List<User>();
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(ReadUser(reader));
            }
            return users;
        }

        /// <summary>
        /// כמה משתמשים יש בכל תפקיד (GROUP BY). שימושי לדף הניהול.
        /// </summary>
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                "SELECT Role, COUNT(*) AS Total FROM Users GROUP BY Role ORDER BY Role;", connection);

            var result = new Dictionary<string, int>();
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result[reader.GetString("Role")] = Convert.ToInt32(reader["Total"]);
            }
            return result;
        }

        // ---------- עדכונים ----------
        // כל פעולת עדכון מחזירה true אם עודכנה שורה, ו-false אם המשתמש לא נמצא.

        public async Task<bool> UpdateRoleAsync(int userId, string role)
        {
            return await ExecuteUpdateAsync(
                "UPDATE Users SET Role = @Value WHERE UserId = @UserId;", userId, role);
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            return await ExecuteUpdateAsync(
                "UPDATE Users SET PasswordHash = @Value WHERE UserId = @UserId;",
                userId, BCrypt.Net.BCrypt.HashPassword(newPassword));
        }

        public async Task<bool> UpdateEmailAsync(int userId, string email)
        {
            return await ExecuteUpdateAsync(
                "UPDATE Users SET Email = @Value WHERE UserId = @UserId;", userId, email);
        }

        public async Task<bool> UpdateDetailsAsync(int userId, string fullName, string? phone)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                "UPDATE Users SET FullName = @FullName, Phone = @Phone WHERE UserId = @UserId;", connection);
            command.Parameters.AddWithValue("@FullName", fullName);
            command.Parameters.AddWithValue("@Phone", (object?)phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// הפעמון: true כשמנהל מתנס מטפל בבקשה של ההורה, false כשההורה לוחץ על הפעמון.
        /// </summary>
        public async Task<bool> SetHasUpdatesAsync(int userId, bool hasUpdates)
        {
            return await ExecuteUpdateAsync(
                "UPDATE Users SET HasUpdates = @Value WHERE UserId = @UserId;", userId, hasUpdates);
        }

        /// <summary>
        /// חסימה (false) או החזרה לפעילות (true) של משתמש.
        /// זו הדרך הרגילה "להוציא" משתמש מהמערכת, במקום למחוק אותו.
        /// </summary>
        public async Task<bool> SetActiveAsync(int userId, bool isActive)
        {
            return await ExecuteUpdateAsync(
                "UPDATE Users SET IsActive = @Value WHERE UserId = @UserId;", userId, isActive);
        }

        // ---------- מחיקה ----------

        /// <summary>
        /// מוחקת משתמש. מיועד רק לניקוי משתמשי בדיקה. במערכת עצמה חוסמים עם SetActiveAsync.
        /// אם למשתמש יש הרשמות או מתנס, MySQL חוסם את המחיקה (מפתח זר עם RESTRICT),
        /// ואז מוחזר false.
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand("DELETE FROM Users WHERE UserId = @UserId;", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1451) // שורה אחרת מצביעה על המשתמש
            {
                return false;
            }
        }

        // ---------- עזר פרטי ----------

        /// <summary>
        /// הופכת שורה מהטבלה לאובייקט User. כל השליפות משתמשות בה, כדי שהקוד לא יחזור על עצמו.
        /// </summary>
        private static User ReadUser(MySqlDataReader reader)
        {
            int phoneIndex = reader.GetOrdinal("Phone");

            return new User(
                reader.GetInt32("UserId"),
                reader.GetString("FullName"),
                reader.GetString("Email"),
                reader.IsDBNull(phoneIndex) ? null : reader.GetString(phoneIndex),
                reader.GetString("Role"),
                reader.GetBoolean("HasUpdates"),
                reader.GetBoolean("IsActive"),
                reader.GetDateTime("CreatedAt"));
        }

        /// <summary>
        /// עדכון של עמודה אחת לפי מזהה משתמש.
        /// שם העמודה כתוב בקוד (לא מגיע מהמשתמש), והערך עובר כפרמטר.
        /// </summary>
        private async Task<bool> ExecuteUpdateAsync(string sql, int userId, object value)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}