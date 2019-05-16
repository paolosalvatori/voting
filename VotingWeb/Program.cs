#region Using Directives
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
#endregion

namespace VotingWeb
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
                .UseApplicationInsights()
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
                        builder.AddFilter<ApplicationInsightsLoggerProvider>("VotingWeb.Program", LogLevel.Trace);

                        // Adding the filter below to ensure logs of all severity from Startup.cs
                        // is sent to ApplicationInsights.
                        // Replace YourAppName with the namespace of your application's Startup.cs
                        builder.AddFilter<ApplicationInsightsLoggerProvider>("VotingWeb.Startup", LogLevel.Trace);
                    }
                );
        #endregion

        #region Private Methods
        private static void GetApplicationInsightsInstrumentationKey(WebHostBuilderContext webHostBuilderContext, IConfigurationBuilder configurationBuilder)
        {
            var configuration = configurationBuilder.Build();

            // Read the Application Insights Instrumentation Key
            applicationInsightsInstrumentationKey = configuration["ApplicationInsights:InstrumentationKey"];
        }
        #endregion
    }
}
