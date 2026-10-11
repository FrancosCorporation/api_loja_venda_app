using System;
using System.Reflection;
using System.Text;
using api_loja_venda_app.DependencyServices;
using api_loja_venda_app.Models;
using api_loja_venda_app.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace api_loja_venda_app
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<AppSettings>()
                .Bind(Configuration.GetSection(nameof(AppSettings)))
                .Configure(settings =>
                {
                    var environmentKey = Environment.GetEnvironmentVariable("JWT_SECURITY_KEY");
                    if (!string.IsNullOrWhiteSpace(environmentKey))
                    {
                        settings.SecurityKey = environmentKey;
                    }
                })
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.SecurityKey) &&
                                      Encoding.UTF8.GetByteCount(settings.SecurityKey) >= 48 &&
                                      !IsPlaceholder(settings.SecurityKey),
                    "JWT_SECURITY_KEY must contain at least 48 UTF-8 bytes of generated secret material.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer),
                    "AppSettings:Issuer must be configured.")
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience),
                    "AppSettings:Audience must be configured.")
                .Validate(settings => settings.AccessTokenMinutes is > 0 and <= 60,
                    "AppSettings:AccessTokenMinutes must be between 1 and 60.")
                .Validate(settings => settings.PasswordResetTokenMinutes is > 0 and <= 120,
                    "AppSettings:PasswordResetTokenMinutes must be between 1 and 120.")
                .ValidateOnStart();

            services.AddOptions<DatabaseSetting>()
                .Bind(Configuration.GetSection(nameof(DatabaseSetting)))
                .Configure(settings =>
                {
                    var environmentConnection = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
                    if (!string.IsNullOrWhiteSpace(environmentConnection))
                    {
                        settings.ConnectionString = environmentConnection;
                    }
                })
                .Validate(settings => IsMongoConnectionString(settings.ConnectionString),
                    "MONGODB_CONNECTION_STRING must be a valid mongodb:// or mongodb+srv:// connection string.")
                .ValidateOnStart();

            services.AddSingleton<IDatabaseSetting>(sp => sp.GetRequiredService<IOptions<DatabaseSetting>>().Value);
            services.AddSingleton<IMongoDbService, MongoDbService>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddControllers();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "api_loja_venda_app", Version = "v1" });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile));
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "api_loja_venda_app v1"));
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static bool IsPlaceholder(string value)
        {
            return value.Contains("<", StringComparison.Ordinal) ||
                   value.Contains("replace", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("changeme", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("example", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMongoConnectionString(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == "mongodb" || uri.Scheme == "mongodb+srv") &&
                   !string.IsNullOrWhiteSpace(uri.Host);
        }
    }
}
