#region Using Directives
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
#endregion

namespace VotingWeb
{
    public class Program
    {
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
                .UseStartup<Startup>();
        #endregion
    }
}
