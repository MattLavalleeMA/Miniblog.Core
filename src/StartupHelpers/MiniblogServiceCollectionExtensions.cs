using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Miniblog.Core.Configuration;
using Miniblog.Core.Services;

namespace Miniblog.Core.StartupHelpers
{
    public static class MiniblogServiceCollectionExtensions
    {
        public static IServiceCollection AddMiniblogRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            Ensure.Argument.IsNotNull(services);
            Ensure.Argument.IsNotNull(configuration);

            if (configuration.GetChildren()
                .All(t => t.Key != nameof(RedisCacheSettings)))
            {
                return services;
            }

            services.Configure<RedisCacheSettings>(configuration.GetSection(nameof(RedisCacheSettings)));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetSection(nameof(RedisCacheSettings))["ConnectionString"];
                options.InstanceName = configuration.GetSection(nameof(RedisCacheSettings))["InstanceName"];
            });

            services.AddSingleton<ICacheService, CacheService>();

            return services;
        }
    }
}
