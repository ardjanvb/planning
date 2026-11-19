using Domain.Extensions;
using MudBlazor.Services;
using Services.Extensions;
using Web.AppState;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddScoped<SessionState>();

var app = builder.Build();

app.Services.EnsureDatabaseCreated();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
