using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Antiforgery: JS calls (fetch) send the token in this header, forms use the hidden field.
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// gzip/brotli for HTML, css, js and JSON: much smaller downloads
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IdGenerator>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EmailSender>();
builder.Services.AddSingleton<ImageStore>();

// Chatbot knowledge file (free, no API): Data/chat-knowledge.json is read at start-up and re-read automatically when it changes.
builder.Services.AddSingleton(sp => new ChatKnowledge(
    Path.Combine(builder.Environment.ContentRootPath, "Data", "chat-knowledge.json"),
    sp.GetRequiredService<ILogger<ChatKnowledge>>()));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "nexus_session";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/auth/denied";
        // API calls should get a status code, not an HTML redirect
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 403;
            else ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
Ui.WebRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

// css/js/images that carry ?v=<hash> (asp-append-version) never change under the same URL, so the browser may keep them for a year
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Query.ContainsKey("v"))
            ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=31536000,immutable";
    }
});

// friendly 404 / 403 pages for browser navigation (API calls keep their plain status codes / JSON bodies)
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/error", "?code={0}"));

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Create the SQL Server database/tables (if missing) and load demo data on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.InitializeAsync(db, app.Configuration);
}

app.Run();
