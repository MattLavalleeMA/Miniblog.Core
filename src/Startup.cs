using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Miniblog.Core.Configuration;
using Miniblog.Core.Services;
using Miniblog.Core.Services.Azure;
using Miniblog.Core.StartupHelpers;
using WebEssentials.AspNetCore.OutputCaching;
using WebEssentials.AspNetCore.Pwa;
using WebMarkupMin.AspNetCore2;
using WebMarkupMin.Core;
using WilderMinds.MetaWeblog;
using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using MetaWeblogService = Miniblog.Core.Services.MetaWeblogService;
using WmmNullLogger = WebMarkupMin.Core.Loggers.NullLogger;

namespace Miniblog.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config = ConfigBuilder.BuildAppConfiguration(hostingContext, config, args);
                })
                .UseStartup<Startup>()
                .UseKestrel(a => a.AddServerHeader = false);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.Configure<BlogSettings>(Configuration.GetSection(nameof(BlogSettings)));
            services.Configure<BlobStorageSettings>(Configuration.GetSection(nameof(BlobStorageSettings)));
            services.Configure<UserSettings>(Configuration.GetSection(nameof(UserSettings)));

            // Redis Cache
            services.AddMiniblogRedisCache(Configuration);

            // Azure Blob Storage
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IBlogService, BlobBlogService>();

            // User Authentication & Authorization
            services.AddSingleton<IUserService, BlogUserService>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login/";
                    options.LogoutPath = "/logout/";
                });

            // Windows Live Writer support (https://github.com/shawnwildermuth/MetaWeblog)
            services.AddMetaWeblog<MetaWeblogService>();

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(new PwaOptions {OfflineRoute = "/Shared/Offline/"});

            // Output caching (https://github.com/madskristensen/WebEssentials.AspNetCore.OutputCaching)
            services.AddOutputCaching(options => { options.Profiles["default"] = new OutputCacheProfile {Duration = 3600}; });


            // HTML minification (https://github.com/Taritsyn/WebMarkupMin)
            services.AddWebMarkupMin(options =>
                {
                    options.AllowMinificationInDevelopmentEnvironment = true;
                    options.DisablePoweredByHttpHeaders = true;
                })
                .AddHtmlMinification(options =>
                {
                    options.MinificationSettings.RemoveOptionalEndTags = false;
                    options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
                });
            services.AddSingleton<IWmmLogger, WmmNullLogger>(); // Used by HTML minifier

            // Bundling, minification and Sass transpilation (https://github.com/ligershark/WebOptimizer)
            services.AddWebOptimizer(pipeline =>
            {
                pipeline.MinifyJsFiles();
                pipeline.CompileScssFiles()
                    .InlineImages(500);
            });

            // ASP.NET Core Health Checks ( )https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2)
            services.AddHealthChecks()
                .AddAzureBlobStorage(Configuration.GetSection(nameof(BlobStorageSettings))["ConnectionString"], "Blob Storage")
                .AddRedis(Configuration.GetSection(nameof(RedisCacheSettings))["ConnectionString"], "Redis");
        }

        // This method gets called by the runtime. Use this method to configure middleware for the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            app.Use((context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                return next();
            });

            app.UseStatusCodePagesWithReExecute("/Shared/Error");
            app.UseWebOptimizer();

            app.UseStaticFilesWithCache();

            if (Configuration.GetValue<bool>("forcessl"))
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.UseMetaWeblog("/metaweblog");
            app.UseAuthentication();

            app.UseOutputCaching();
            app.UseWebMarkupMin();

            app.UseHealthChecks("/health");

            app.UseMvc(routes => { routes.MapRoute("default", "{controller=Blog}/{action=Index}/{id?}"); });
        }
    }
}