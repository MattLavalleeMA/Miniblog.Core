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

            services.Configure<BlogSettings>(Configuration.GetSection("blog"));
            services.Configure<BlobStorageSettings>(Configuration.GetSection("blobStorage"));
            services.Configure<RedisSettings>(Configuration.GetSection("redis"));
            services.Configure<UserSettings>(Configuration.GetSection("user"));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetSection("redis")["ConnectionString"];
                options.InstanceName = Configuration.GetSection("redis")["InstanceName"];
            });

            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IUserServices, BlogUserServices>();
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IBlogService, BlobBlogService>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddMetaWeblog<MetaWeblogService>();

            // Progressive Web Apps https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker
            services.AddProgressiveWebApp(new PwaOptions {OfflineRoute = "/Shared/Offline/"});

            // Output caching (https://github.com/madskristensen/WebEssentials.AspNetCore.OutputCaching)
            services.AddOutputCaching(options =>
                {
                    options.Profiles["default"] = new OutputCacheProfile {Duration = 3600};
                }
            );

            // Cookie authentication.
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login/";
                    options.LogoutPath = "/logout/";
                });

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

            services.AddHealthChecks()
                .AddAzureBlobStorage(Configuration.GetSection("blobStorage")["ConnectionString"], "Blob Storage")
                .AddRedis(Configuration.GetSection("redis")["ConnectionString"], "Redis");
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