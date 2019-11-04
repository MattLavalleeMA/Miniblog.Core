using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace Miniblog.Core.StartupHelpers
{
    public static class ConfigBuilder
    {
        public static IConfigurationBuilder BuildAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder configBuilder, string[] args)
        {
            configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                .AddCommandLine(args)
                .AddEnvironmentVariables();

            if (context.HostingEnvironment.IsDevelopment())
            {
                configBuilder.AddUserSecrets<Startup>();
            }

            IConfigurationRoot built = configBuilder.Build();
            if (!built
                .GetSection("keyVault")
                .Exists())
            {
                return configBuilder;
            }

            try
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                configBuilder.AddAzureKeyVault($"https://{built["AzureKeyVault:Name"]}.vault.azure.net/", keyVaultClient, new PrefixKeyVaultSecretManager(built["AzureKeyVault:Prefix"]));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return configBuilder;
        }
    }
}