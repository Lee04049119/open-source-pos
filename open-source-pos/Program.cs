using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Services;
using Repositories;
using Repositories.SqlServer;
using Repositories.Log;
using Repositories.Common;
using Models;

namespace open_source_pos
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Optional per-machine LAN settings (gitignored). Written by PosNetworkSetup WinForms tool.
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app, builder.Environment, builder.Configuration);

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            // Add Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Open Source POS API",
                    Version = "v1",
                    Description = "A simple open source Point of Sale system API"
                });

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

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
                        Array.Empty<string>()
                    }
                });
            });

            // CORS: set Cors:AllowedOrigins in appsettings.{Environment}.json (see appsettings.Development.json / Production).
            // Empty/missing list falls back to allowing any origin (development convenience only).
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", BuildCorsPolicy);
                options.AddPolicy("corsGlobalPolicy", BuildCorsPolicy);
            });

            void BuildCorsPolicy(CorsPolicyBuilder builder)
            {
                var origins = GetMergedCorsOrigins(configuration);
                if (origins != null && origins.Length > 0)
                {
                    builder.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
                else
                {
                    builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            }

            // Obtain database connection string
            var connectionString = configuration.GetValue<string>("DBConnection:ConnectionString");
            var FNNConnectionString = configuration.GetValue<string>("DBConnection:FNNConnectionString");

            // Configure dependency injection
            services.AddScoped<IConnection>(x => new Connection(FNNConnectionString, connectionString, connectionString));

            services.AddScoped<IRepository, Repository>();
            services.AddScoped<ILogIt, LogIt>();
            services.AddScoped<IFLogIt, FLogIt>();
            services.AddScoped<ILogRepository, LogRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IPOSService, POSService>();
            services.AddScoped<IPOSRepository, POSRepository>();

            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IItemRepository, ItemRepository>();

            // Read email settings
            services.Configure<EmailConfig>(configuration.GetSection("Email"));
            services.Configure<SMSoptions>(configuration.GetSection("SMSTwilio"));
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // JWT Authentication configuration
            var appSettingsSection = configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        var user = userService.GetById(userId);
                        if (user == null)
                        {
                            context.Fail("Unauthorized");
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization();
        }

        /// <summary>
        /// Merges Cors:AllowedOrigins with origins derived from Lan:Host (see appsettings.Local.json).
        /// </summary>
        private static string[] GetMergedCorsOrigins(IConfiguration configuration)
        {
            var fromFile = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            var trimmed = fromFile
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();

            var host = configuration["Lan:Host"]?.Trim();
            if (string.IsNullOrEmpty(host))
                return trimmed;

            var angularPort = configuration["Lan:AngularPort"] ?? "4200";
            var httpApiPort = configuration["Lan:HttpApiPort"] ?? "5000";
            var httpsApiPort = configuration["Lan:HttpsApiPort"] ?? "5001";
            var imagePort = configuration["Lan:ImagePort"] ?? "9096";

            var extra = new List<string>
            {
                $"http://{host}:{angularPort}",
                $"https://{host}:{angularPort}",
                $"http://{host}:{httpApiPort}",
                $"https://{host}:{httpsApiPort}",
                $"http://{host}:{imagePort}",
                $"https://{host}:{imagePort}",
            };

            return trimmed
                .Concat(extra)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static void ConfigurePipeline(WebApplication app, IWebHostEnvironment env, IConfiguration configuration)
        {
            // Configure Swagger for development
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Open Source POS API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                });
            }
            else
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Open Source POS API V1");
                    c.RoutePrefix = "swagger";
                });
            }

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}