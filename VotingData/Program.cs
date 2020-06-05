#region Using Directives
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;
#endregion

namespace VotingData
{
    public class Program
    {
        #region Private Static Fields
        private static string applicationInsightsInstrumentationKey = null;
        #endregion

        #region Public Methods
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .CaptureStartupErrors(true)
                .UseSetting(WebHostDefaults.DetailedErrorsKey, "true")
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(GetApplicationInsightsInstrumentationKey)
                .ConfigureLogging(
                    builder =>
                    {
                        // providing an instrumentation key here is required if you are using
                        // standalone package Microsoft.Extensions.Logging.ApplicationInsights
                        // or if you want to capture logs from early in the application startup 
                        // pipeline from Startup.cs or Program.cs itself.
                        builder.AddApplicationInsights(applicationInsightsInstrumentationKey);

                        // Adding the filter below to ensure logs of all severity from Program.cs
                        // is sent to ApplicationInsights.
                        // Replace YourAppName with the namespace of your application's Program.cs
                        builder.AddFilter<ApplicationInsightsLoggerProvider>("VotingData.Program", LogLevel.Trace);

                        // Adding the filter below to ensure logs of all severity from Startup.cs
                        // is sent to ApplicationInsights.
                        // Replace YourAppName with the namespace of your application's Startup.cs
                        builder.AddFilter<ApplicationInsightsLoggerProvider>("VotingData.Startup", LogLevel.Trace);
                    }
                );
        #endregion

        #region Private Methods
        private static void GetApplicationInsightsInstrumentationKey(WebHostBuilderContext context, IConfigurationBuilder configurationBuilder)
        {
            // Read from default configuration providers: config files and environment variables
            var builtConfig = configurationBuilder.Build();

            var keyVaultName = builtConfig["KeyVault:Name"];
            var keyVaultConnectionString = builtConfig["KeyVault:ConnectionString"];

            if (!string.IsNullOrWhiteSpace(keyVaultName))
            {
                var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";

                if (string.IsNullOrWhiteSpace(keyVaultConnectionString) ||
                   string.Compare(keyVaultConnectionString, "none", true) == 0)
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient((authority, resource, scope) => azureServiceTokenProvider.KeyVaultTokenCallback(authority, resource, scope));
                    configurationBuilder.AddAzureKeyVault(keyVaultUrl,
                                                          keyVaultClient,
                                                          new DefaultKeyVaultSecretManager());
                }
                else
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider(keyVaultConnectionString);
                    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    configurationBuilder.AddAzureKeyVault(keyVaultUrl,
                                                          keyVaultClient,
                                                          new DefaultKeyVaultSecretManager());
                }
            }

            // Read configuration from Key Vault
            builtConfig = configurationBuilder.Build();

            // Read the Application Insights Instrumentation Key stored in Key Vault
            applicationInsightsInstrumentationKey = builtConfig["ApplicationInsights:InstrumentationKey"];
        }
        #endregion
    }
}