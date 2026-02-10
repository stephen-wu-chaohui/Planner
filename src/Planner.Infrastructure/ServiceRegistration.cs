using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Planner.Infrastructure.Persistence;
using System.Text;
using Planner.Infrastructure.Auth;

namespace Planner.Infrastructure;

public static class ServiceRegistration {
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config) {

        services.AddDbContext<PlannerDbContext>(opt =>
            opt.UseSqlServer(
                config.GetConnectionString("PlannerDb"),
                sql => sql.MigrationsAssembly(
                    typeof(PlannerDbContext).Assembly.FullName)));
        
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SigningKey)),

                    ClockSkew = TimeSpan.FromSeconds(5)
                };
                
                // Support reading JWT from cookie as fallback
                options.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        // First check Authorization header (default)
                        if (string.IsNullOrEmpty(context.Token)) {
                            // Fallback to cookie if no header present
                            context.Token = context.Request.Cookies["planner-auth"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options => {
            options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Admin"));
        });


        return services;
    }
}
