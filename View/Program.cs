using View.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// appsettings.json
//     הכתובת של השרת נקראת מהקובץ הזה, ואם היא חסרה התצוגה לא עולה
string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl not found in appsettings.json.");

// HttpClient
//     שולח בקשות לשרת ומכיר את הכתובת שלו
//     כל דף שמבקש אותו מקבל אותו מוכן
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// השירות שזוכר את המשתמש המחובר
// AddScoped
//     עצם אחד לכל לשונית בדפדפן, כך שכל גולש רואה רק את המשתמש של עצמו
// AddSingleton
//     עצם אחד לכל השרת, ולכן כל הגולשים היו רואים את אותו משתמש
builder.Services.AddScoped<View.Services.UserSession>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
