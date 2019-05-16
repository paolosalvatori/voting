#region Using Directives

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using VotingData.Models;
using VotingData.Services;

// ReSharper disable All

#endregion

namespace VotingData
{
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
            services.AddOptions();
            var aiStorageFolder = Configuration.GetValue("ApplicationInsightsStorageFolder", "ApplicationInsights");
            if (aiStorageFolder != null)
            {
                // For Linux OS
                services.AddSingleton<ITelemetryChannel>(new ServerTelemetryChannel { StorageFolder = aiStorageFolder });
            }
            services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.Configure<RepositoryServiceOptions>(Configuration.GetSection("RepositoryService"));
            services.Configure<NotificationServiceOptions>(Configuration.GetSection("NotificationService"));
            services.AddSingleton<INotificationService, ServiceBusNotificationService>();
            services.AddSingleton<IRepositoryService<Vote>, CosmosDbRepositoryService<Vote>>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "Voting API",
                    Description = "REST API exposed by the VotingData service",
                    TermsOfService = "None",
                    Contact = new Contact {
                        Name = @"Paolo Salvatori",
                        Email = "paolos@microsoft.com",
                        Url = "https://github.com/paolosalvatori"
                    },
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Voting API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseMvc();
        }
    }
}
