using SudokuApp.Server.Services;
using SudokuApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddHttpClient<IPuzzleProvider, ApiNinjasPuzzleProvider>(client =>
{
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["ApiNinjas:ApiKey"]);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
