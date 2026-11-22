var builder = WebApplication.CreateBuilder(args);

// User Secrets
//     כאן נשמרת מחרוזת החיבור לבסיס הנתונים, והיא לא עולה לגיטהאב
//     אם היא חסרה, השרת לא עולה, במקום לעלות בלי בסיס נתונים
string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set it with dotnet user-secrets.");

// מחרוזת חיבור שמצביעה על בסיס הנתונים עצמו, בשביל שכבת הגישה לנתונים
string dbConnectionString = DAL.DatabaseInitializer.GetDatabaseConnectionString(serverConnectionString);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Dependency Injection
//     רישום המחלקה של המשתמשים, כל בקר שמבקש אותה בבנאי מקבל אותה אוטומטית
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

var app = builder.Build();

// יצירת בסיס הנתונים, הטבלאות והנתונים ההתחלתיים לפני שהשרת מתחיל לקבל בקשות
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