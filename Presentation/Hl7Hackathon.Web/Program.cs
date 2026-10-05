using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Web.Components;
using Hl7Hackathon.Web.Extensions;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
AppSettings appSettings = builder.AddAppSettings();

builder.AddSerilog();
builder.AddTelemetry();

builder.Services.AddDatabase(appSettings);
builder.Services.AddFhirHttpClient(appSettings);
builder.Services.AddDependencyInjection();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var app = builder.Build();
app.RunMigrations();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
