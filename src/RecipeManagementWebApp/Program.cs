using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeManagementWebApp.Components;
using RecipeManagementWebApp.Data;
using RecipeManagementWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var recipeDbConnectionString = builder.Configuration.GetConnectionString("RecipeDb")
    ?? throw new InvalidOperationException("Missing 'ConnectionStrings:RecipeDb' configuration value.");

builder.Services.AddDbContext<RecipeDbContext>(options => options.UseSqlite(recipeDbConnectionString));

builder.Services.Configure<PhotoStorageOptions>(
    builder.Configuration.GetSection(PhotoStorageOptions.SectionName));
builder.Services.AddScoped<IPhotoStorageService, PhotoStorageService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IPantryService, PantryService>();
builder.Services.AddScoped<ICookLogService, CookLogService>();

var app = builder.Build();

var dbPath = new SqliteConnectionStringBuilder(recipeDbConnectionString).DataSource;
if (Path.GetDirectoryName(dbPath) is { Length: > 0 } dbDirectory)
{
    Directory.CreateDirectory(dbDirectory);
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RecipeDbContext>().Database.EnsureCreated();
}

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
