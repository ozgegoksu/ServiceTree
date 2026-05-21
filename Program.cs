using Microsoft.EntityFrameworkCore;
using ServiceTreeDemo.Components;
using ServiceTreeDemo.Data;
using ServiceTreeDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── EF Core / SQLite ────────────────────────────────────────────────────
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Application Services ─────────────────────────────────────────────────
builder.Services.AddSingleton<ServiceTreeService>();
builder.Services.AddHttpClient("healthcheck");

// HealthCheckService shares the same ServiceTreeService singleton
builder.Services.AddSingleton<HealthCheckService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<HealthCheckService>());

var app = builder.Build();

// ── Ensure DB is created and run migrations ──────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

// ── Initialise in-memory cache from DB (and seed if empty) ───────────────
var treeService = app.Services.GetRequiredService<ServiceTreeService>();
await treeService.InitAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();