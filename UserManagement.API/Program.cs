using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserManagement.API.Filters;
using UserManagement.API.Middlewares;
using UserManagement.API.Modules;
using UserManagement.Core.Model;
using UserManagement.Core.Services;
using UserManagement.Repository;
using UserManagement.Service.Mapping;
using UserManagement.Service.Services;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers(options => options.Filters.Add(new ValidateFilterAttribute()));

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpClient();

        builder.Services.AddScoped(typeof(NotFoundFilter<>));
        builder.Services.AddAutoMapper(typeof(MapProfile));

        // Email settings and service registration
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

        // Register email service based on Provider setting
        var emailProvider = builder.Configuration.GetValue<string>("EmailSettings:Provider") ?? "SMTP";
        if (emailProvider.Equals("GmailApi", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddScoped<IEmailService, GmailApiEmailService>();
        }
        else
        {
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        }

        // Register verification code service
        builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();

        builder.Services.AddDbContext<AppDbContext>(x =>
        {
            x.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"), options =>
            {
                options.MigrationsAssembly(Assembly.GetAssembly(typeof(AppDbContext)).GetName().Name);
            });
        });

        // Register Identity services
        builder.Services.AddIdentity<User, Role>(options =>
        {
            // Password requirements (relaxed for development)
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            // Email verification required
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        }).AddDefaultTokenProviders();

        // Register UserStore, RoleStore and UserIdentityService
        builder.Services.AddScoped<IUserStore<User>, UserIdentityService>();
        builder.Services.AddScoped<IRoleStore<Role>, RoleIdentityService>();
        builder.Services.AddScoped<IUserPasswordStore<User>, UserIdentityService>();
        builder.Services.AddScoped<UserIdentityService>();

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            containerBuilder.RegisterModule(new RepoServiceModule()));

        // Bind JWT settings from configuration
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        // Register services required for JWT
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = jwtSettings.GOOGLE_CLIENT_ID;
            googleOptions.ClientSecret = jwtSettings.GOOGLE_CLIENT_SECRET;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero  // Optional: reduce clock skew time
            };
        });

        builder.Services.AddAuthorization();

        // Add Swagger with JWT Bearer authentication configuration
        builder.Services.AddSwaggerGen(c =>
        {
            // Define the Bearer security scheme
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Enter your Access Token here"
            });

            // Define the Refresh Token security scheme
            c.AddSecurityDefinition("RefreshToken", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "RefreshToken",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                Description = "Enter your Refresh Token here"
            });

            // Apply the security requirements globally
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "RefreshToken"
                }
            },
            new string[] {}
        }
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy => policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader());
        });

        var app = builder.Build();

        // Auto-deploy database: Creates database if not exists, applies migrations or schema updates automatically
        // No manual 'dotnet ef migrations add' commands required
        await UserManagement.Repository.DatabaseInitializer.InitializeDatabaseAsync(
            app.Services, 
            app.Environment.IsDevelopment());

        app.UseCors("AllowAll");

        // Enable Swagger for all environments
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserManagement API v1");
            c.RoutePrefix = "swagger";
        });

        app.UseMiddleware<JwtRefreshMiddleware>();
        app.UseAuthentication();
        //app.UseHttpsRedirection();
        app.UseCustomException();

        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}