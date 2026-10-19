var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// מחרוזת החיבור לשרת MySQL נקראת מ-User Secrets (בסביבת Development).
// אם היא חסרה, השרת לא עולה, במקום לעלות בלי בסיס נתונים.
string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set it with dotnet user-secrets.");

// יצירת HobbifyDB, הטבלאות והקטגוריות לפני שהשרת מתחיל לקבל בקשות.
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