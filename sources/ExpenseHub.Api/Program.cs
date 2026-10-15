using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers(options =>
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        string connectionString = builder.Configuration.GetConnectionString("ExpenseHub")
            ?? throw new InvalidOperationException("Connection string 'ExpenseHub' não configurada.");
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        AddIdentityAndAuthentication(builder);
        builder.Services.AddScoped<AccountService>();
        builder.Services.AddScoped<UserAdministrationService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
        builder.Services.AddScoped<ExpenseService>();

        WebApplication app = builder.Build();

        await InitializeDatabaseAsync(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth")
            .AllowAnonymous();

        app.MapControllers();

        await app.RunAsync();
    }

    private static void AddIdentityAndAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme, options => options.BearerTokenExpiration = TimeSpan.FromHours(1));

        // Toda rota exige usuário autenticado, exceto as marcadas com AllowAnonymous.
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        builder.Services.AddIdentityCore<IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddOptions<AdminSeedOptions>()
            .Bind(builder.Configuration.GetSection(AdminSeedOptions.SectionName));
        builder.Services.AddScoped<IdentitySeeder>();
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }
}
