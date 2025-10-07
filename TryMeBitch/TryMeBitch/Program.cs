using TryMeBitch.Data;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using TryMeBitch.Services;
using static TryMeBitch.Areas.Identity.Pages.Account.RegisterModel;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MRTDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")));

builder.Services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<MRTDbContext>().AddDefaultTokenProviders(); ;
builder.Services.AddRazorPages();
builder.Services.AddHostedService<IoTHubListenerService>();
builder.Services.AddScoped<Blockchain>();

builder.Services.AddHostedService<HVACListener>();
builder.Services.AddScoped<HVAC>();

builder.Services.AddHostedService<IoTHubReceiverService>();
builder.Services.AddHostedService<AlertProcessingService>();
builder.Services.AddScoped<EnergyUsageService>();
builder.Services.AddScoped<LoadMonitoringService>();
builder.Services.AddScoped<DepotEnergySlot>();
builder.Services.AddScoped<TrainLocation>();

builder.Services.AddHostedService<IoTBackgroundService>();

builder.Services.AddSingleton<SmtpEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IEmailSender, TryMeBitch.Areas.Identity.Pages.Account.FakeEmailSender>();


builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});


builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();    

app.Run();
