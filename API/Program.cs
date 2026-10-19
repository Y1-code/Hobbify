var builder = WebApplication.CreateBuilder(args);

// מחרוזת החיבור לשרת MySQL נקראת מ-User Secrets (בסביבת Development).
// אם היא חסרה, השרת לא עולה, במקום לעלות בלי בסיס נתונים.
string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set it with dotnet user-secrets.");

// מחרוזת חיבור שמצביעה על HobbifyDB עצמו, בשביל מחלקות ה-DAL.
string dbConnectionString = DAL.DatabaseInitializer.GetDatabaseConnectionString(serverConnectionString);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// רישום UserDB: כל Controller שמבקש UserDB בבנאי יקבל אותו אוטומטית (Dependency Injection).
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

var app = builder.Build();

// יצירת HobbifyDB, הטבלאות והנתונים ההתחלתיים לפני שהשרת מתחיל לקבל בקשות.
await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();