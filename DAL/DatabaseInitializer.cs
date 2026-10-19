using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// אתחול בסיס הנתונים של Hobbify.
    /// רץ פעם אחת בכל עליה של ה-API, ובטוח להריץ אותו שוב ושוב (אידמפוטנטי):
    /// מה שכבר קיים לא נוצר מחדש, ונתונים התחלתיים לא מוכנסים פעמיים.
    /// </summary>
    public static class DatabaseInitializer
    {
        public const string DatabaseName = "HobbifyDB";

        /// <summary>
        /// הפעולה הראשית. מקבלת מחרוזת חיבור לשרת בלבד (בלי Database=),
        /// כי בהרצה הראשונה בסיס הנתונים עוד לא קיים.
        /// </summary>
        public static async Task InitializeAsync(string serverConnectionString)
        {
            await CreateDatabaseAsync(serverConnectionString);

            string dbConnectionString = GetDatabaseConnectionString(serverConnectionString);
            await CreateTablesAsync(dbConnectionString);
            await SeedCategoriesAsync(dbConnectionString);
        }

        /// <summary>
        /// מחזירה מחרוזת חיבור שמצביעה על HobbifyDB.
        /// בשלבים הבאים (UserDB ודומיה) משתמשים בה כדי להתחבר לבסיס הנתונים עצמו.
        /// </summary>
        public static string GetDatabaseConnectionString(string serverConnectionString)
        {
            var builder = new MySqlConnectionStringBuilder(serverConnectionString)
            {
                Database = DatabaseName,
                CharacterSet = "utf8mb4" // תמיכה מלאה בעברית
            };
            return builder.ConnectionString;
        }

        // ---------- שלב 1: בסיס הנתונים ----------

        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();

            string sql = $@"
                CREATE DATABASE IF NOT EXISTS {DatabaseName}
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_0900_ai_ci;";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        // ---------- שלב 2: הטבלאות ----------
        // הסדר חשוב: טבלה עם מפתח זר נוצרת רק אחרי הטבלה שהיא מצביעה עליה.
        // ערכי סטטוס, תפקיד ומין נשמרים באנגלית (לשימוש בקוד), ובממשק מציגים אותם בעברית.

        private static async Task CreateTablesAsync(string dbConnectionString)
        {
            string[] tables =
            {
                // 1. Users - כל מי שמתחבר: הורה, מנהל מתנס או מנהל מערכת.
                // HasUpdates מדליק את הפעמון כשמנהל מתנס מטפל בבקשה של ההורה.
                @"CREATE TABLE IF NOT EXISTS Users (
                    UserId       INT AUTO_INCREMENT PRIMARY KEY,
                    FullName     VARCHAR(100) NOT NULL,
                    Email        VARCHAR(150) NOT NULL UNIQUE,
                    Phone        VARCHAR(20)  NULL,
                    PasswordHash VARCHAR(255) NOT NULL,
                    Role         VARCHAR(20)  NOT NULL DEFAULT 'Parent',
                    HasUpdates   BOOLEAN      NOT NULL DEFAULT FALSE,
                    CreatedAt    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT CHK_Users_Role CHECK (Role IN ('Parent', 'Manager', 'SuperAdmin'))
                );",

                // 2. Cities - עיר, תמונת המפה שלה ומיקומה במפת ישראל (באחוזים).
                @"CREATE TABLE IF NOT EXISTS Cities (
                    CityId   INT AUTO_INCREMENT PRIMARY KEY,
                    Name     VARCHAR(100) NOT NULL UNIQUE,
                    MapImage VARCHAR(255) NULL,
                    MapX     DECIMAL(5,2) NULL,
                    MapY     DECIMAL(5,2) NULL
                );",

                // 4. Categories - נוצרת לפני Classes כי חוג מצביע על קטגוריה.
                @"CREATE TABLE IF NOT EXISTS Categories (
                    CategoryId INT AUTO_INCREMENT PRIMARY KEY,
                    Name       VARCHAR(50) NOT NULL UNIQUE
                );",

                // 3. CommunityCenters - מתנס ומיקומו במפת העיר (באחוזים).
                // ManagerUserId ייחודי: לכל מתנס מנהל אחד, ולכל מנהל מתנס אחד.
                @"CREATE TABLE IF NOT EXISTS CommunityCenters (
                    CenterId      INT AUTO_INCREMENT PRIMARY KEY,
                    ManagerUserId INT          NOT NULL UNIQUE,
                    CityId        INT          NOT NULL,
                    Name          VARCHAR(100) NOT NULL,
                    Address       VARCHAR(200) NOT NULL,
                    Phone         VARCHAR(20)  NULL,
                    Description   TEXT         NULL,
                    LogoImage     VARCHAR(255) NULL,
                    MapX          DECIMAL(5,2) NULL,
                    MapY          DECIMAL(5,2) NULL,
                    Status        VARCHAR(20)  NOT NULL DEFAULT 'Pending',
                    CONSTRAINT FK_Centers_Manager FOREIGN KEY (ManagerUserId)
                        REFERENCES Users(UserId) ON DELETE RESTRICT,
                    CONSTRAINT FK_Centers_City FOREIGN KEY (CityId)
                        REFERENCES Cities(CityId) ON DELETE RESTRICT,
                    CONSTRAINT CHK_Centers_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
                );",

                // 5. Classes - החוג עצמו.
                // האילוצים שומרים על נתונים הגיוניים, ובמיוחד SpotsLeft לא יורד מתחת ל-0.
                @"CREATE TABLE IF NOT EXISTS Classes (
                    ClassId      INT AUTO_INCREMENT PRIMARY KEY,
                    CenterId     INT          NOT NULL,
                    CategoryId   INT          NOT NULL,
                    Name         VARCHAR(100) NOT NULL,
                    Summary      TEXT         NULL,
                    Instructor   VARCHAR(100) NULL,
                    ImageFile    VARCHAR(255) NULL,
                    MinAge       TINYINT      NOT NULL,
                    MaxAge       TINYINT      NOT NULL,
                    Gender       VARCHAR(10)  NOT NULL DEFAULT 'Mixed',
                    MonthlyPrice DECIMAL(8,2) NOT NULL,
                    StartDate    DATE         NOT NULL,
                    EndDate      DATE         NOT NULL,
                    Capacity     INT          NOT NULL,
                    SpotsLeft    INT          NOT NULL,
                    CONSTRAINT FK_Classes_Center FOREIGN KEY (CenterId)
                        REFERENCES CommunityCenters(CenterId) ON DELETE RESTRICT,
                    CONSTRAINT FK_Classes_Category FOREIGN KEY (CategoryId)
                        REFERENCES Categories(CategoryId) ON DELETE RESTRICT,
                    CONSTRAINT CHK_Classes_Gender CHECK (Gender IN ('Boys', 'Girls', 'Mixed')),
                    CONSTRAINT CHK_Classes_Ages   CHECK (MinAge <= MaxAge),
                    CONSTRAINT CHK_Classes_Dates  CHECK (StartDate <= EndDate),
                    CONSTRAINT CHK_Classes_Spots  CHECK (SpotsLeft >= 0 AND SpotsLeft <= Capacity)
                );",

                // 6. ClassSchedules - מפגש שבועי אחד. חוג פעמיים בשבוע = שתי שורות.
                // DayOfWeek: 0 = ראשון ... 6 = שבת.
                // מחיקת חוג מוחקת גם את המפגשים שלו (CASCADE).
                @"CREATE TABLE IF NOT EXISTS ClassSchedules (
                    ScheduleId INT AUTO_INCREMENT PRIMARY KEY,
                    ClassId    INT     NOT NULL,
                    DayOfWeek  TINYINT NOT NULL,
                    StartTime  TIME    NOT NULL,
                    EndTime    TIME    NOT NULL,
                    CONSTRAINT FK_Schedules_Class FOREIGN KEY (ClassId)
                        REFERENCES Classes(ClassId) ON DELETE CASCADE,
                    CONSTRAINT CHK_Schedules_Day  CHECK (DayOfWeek BETWEEN 0 AND 6),
                    CONSTRAINT CHK_Schedules_Time CHECK (StartTime < EndTime)
                );",

                // 7. Registrations - בקשת הרשמה, וגם הביקורת (Stars, ReviewComment).
                // כך אין ביקורת בלי הרשמה, ויש לכל היותר ביקורת אחת לכל הרשמה.
                @"CREATE TABLE IF NOT EXISTS Registrations (
                    RegistrationId INT AUTO_INCREMENT PRIMARY KEY,
                    ClassId        INT          NOT NULL,
                    ParentUserId   INT          NOT NULL,
                    ChildFirstName VARCHAR(50)  NOT NULL,
                    ChildLastName  VARCHAR(50)  NOT NULL,
                    ChildAge       TINYINT      NOT NULL,
                    ChildGender    VARCHAR(10)  NOT NULL,
                    ParentName     VARCHAR(100) NOT NULL,
                    ParentPhone    VARCHAR(20)  NOT NULL,
                    ParentEmail    VARCHAR(150) NOT NULL,
                    Notes          TEXT         NULL,
                    Status         VARCHAR(20)  NOT NULL DEFAULT 'Pending',
                    RejectReason   VARCHAR(255) NULL,
                    Stars          TINYINT      NULL,
                    ReviewComment  VARCHAR(255) NULL,
                    CreatedAt      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    HandledAt      DATETIME     NULL,
                    CONSTRAINT FK_Registrations_Class FOREIGN KEY (ClassId)
                        REFERENCES Classes(ClassId) ON DELETE RESTRICT,
                    CONSTRAINT FK_Registrations_Parent FOREIGN KEY (ParentUserId)
                        REFERENCES Users(UserId) ON DELETE RESTRICT,
                    CONSTRAINT CHK_Registrations_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
                    CONSTRAINT CHK_Registrations_ChildGender CHECK (ChildGender IN ('Boy', 'Girl')),
                    CONSTRAINT CHK_Registrations_Stars CHECK (Stars IS NULL OR Stars BETWEEN 1 AND 5)
                );",

                // 8. Favorites - חוגים שההורה שמר. אותו חוג לא נשמר פעמיים לאותו הורה.
                @"CREATE TABLE IF NOT EXISTS Favorites (
                    FavoriteId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId     INT NOT NULL,
                    ClassId    INT NOT NULL,
                    CONSTRAINT FK_Favorites_User FOREIGN KEY (UserId)
                        REFERENCES Users(UserId) ON DELETE CASCADE,
                    CONSTRAINT FK_Favorites_Class FOREIGN KEY (ClassId)
                        REFERENCES Classes(ClassId) ON DELETE CASCADE,
                    CONSTRAINT UQ_Favorites_UserClass UNIQUE (UserId, ClassId)
                );"
            };

            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            foreach (string sql in tables)
            {
                using var command = new MySqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        // ---------- שלב 3: נתונים התחלתיים ----------
        // קטגוריות בסיסיות, רק אם הטבלה ריקה.
        // מנהל המערכת ונתוני הדוגמה (ערים, מתנסים, חוגים) יתווספו בשלב הבא,
        // אחרי שתהיה מחלקת User ודרך לשמור סיסמאות.

        private static async Task SeedCategoriesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            using (var countCommand = new MySqlCommand("SELECT COUNT(*) FROM Categories;", connection))
            {
                long count = Convert.ToInt64(await countCommand.ExecuteScalarAsync());
                if (count > 0)
                {
                    return;
                }
            }

            string sql = @"
                INSERT INTO Categories (Name) VALUES
                ('ספורט'),
                ('אמנות'),
                ('מוזיקה'),
                ('מדע'),
                ('מחול'),
                ('טכנולוגיה');";

            using var insertCommand = new MySqlCommand(sql, connection);
            await insertCommand.ExecuteNonQueryAsync();
        }
    }
}