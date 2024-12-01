using System;
using System.IO;
using Fork.Adapters.Fork;
using Fork.Adapters.Mojang;
using Fork.Logic.Managers;
using Fork.Logic.Notification;
using Fork.Logic.Persistence;
using Fork.Logic.Services.EntityServices;
using Fork.Logic.Services.FileServices;
using Fork.Logic.Services.StateServices;
using Fork.Logic.Services.WebServices;
using Fork.Util;
using Fork.Util.ExtensionMethods;
using Fork.Util.SwaggerUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using AuthenticationService = Fork.Logic.Services.AuthenticationServices.AuthenticationService;

namespace Fork;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        //TODO CKE Add proper CORS policy
        services.AddCors(policy =>
        {
            policy.AddPolicy("CorsPolicy", opt => opt
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        SqliteConnectionStringBuilder builder = new(Configuration.GetConnectionString("DefaultConnection"));
        builder.DataSource = builder.DataSource.Replace("|datadirectory|",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForkApp",
                "persistence"));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.ToString()));
        //services.AddDbContext<ApplicationDbContext>(options =>
        //     options.UseLazyLoadingProxies().UseSqlite(builder.ToString()).UseLoggerFactory(MyLoggerFactory).EnableSensitiveDataLogging());
        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddControllers(o =>
        {
            o.InputFormatters.Add(new TextPlainInputFormatter());
            o.Filters.Add<ForkExceptionFilterAttribute>();
        }).AddNewtonsoftJson(opt =>
        {
            opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            opt.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
            opt.SerializerSettings.Converters.Add(new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            });
        });


        // Singletons
        services.AddSingleton<ApplicationManager>();
        services.AddSingleton< NotificationCenter>();
        services.AddSingleton< TokenManager>();
        services.AddSingleton< EntityManager>();
        services.AddSingleton<CommandService>();

        // Scoped
        services.AddScoped<AuthenticationService>();

        // Transient
        services.AddTransient< DownloadService>();
        services.AddTransient< ConsoleService>();
        services.AddTransient< ServerService>();
        services.AddTransient< EntityService>();
        services.AddTransient< FileWriterService>();
        services.AddTransient< FileReaderService>();
        services.AddTransient< EntityPostProcessingService>();
        services.AddTransient< ApplicationStateService>();
        services.AddTransient< PlayerService>();
        services.AddTransient< ConsoleInterpreter>();
        // Transient adapters
        services.AddTransient<MojangApiAdapter>();
        services.AddTransient<ForkApiAdapter>();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fork", Version = "v1" });
            c.OperationFilter<TokenSecurityFilter>();
            c.AddSecurityDefinition("Fork Token", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
        });
        services.AddSwaggerGenNewtonsoftSupport();
        services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Eager init NotificationCenter for WebSockets
        app.ApplicationServices.GetService<NotificationCenter>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fork v1"));
        }

        // TODO CKE enable https with self generated certificate
        //app.UseHttpsRedirection();
        app.UseCors("CorsPolicy");

        app.UseRouting();

        app.UseAuthenticationMiddleware();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}