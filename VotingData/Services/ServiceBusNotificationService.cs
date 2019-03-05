// ReSharper disable All
#region Copyright
//=======================================================================================
// Microsoft 
//
// This sample is supplemental to the technical guidance published on my personal
// blog at https://github.com/paolosalvatori. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// LICENSED UNDER THE APACHE LICENSE, VERSION 2.0 (THE "LICENSE"); YOU MAY NOT USE THESE 
// FILES EXCEPT IN COMPLIANCE WITH THE LICENSE. YOU MAY OBTAIN A COPY OF THE LICENSE AT 
// http://www.apache.org/licenses/LICENSE-2.0
// UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING, SOFTWARE DISTRIBUTED UNDER THE 
// LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY 
// KIND, EITHER EXPRESS OR IMPLIED. SEE THE LICENSE FOR THE SPECIFIC LANGUAGE GOVERNING 
// PERMISSIONS AND LIMITATIONS UNDER THE LICENSE.
//=======================================================================================
#endregion

#region Using Directives

using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using VotingData.Models;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;

#endregion

namespace VotingData.Services
{
    /// <summary>
    /// This class is used to send notifications to a Service Bus queue.
    /// </summary>
    public class ServiceBusNotificationService : NotificationService
    {
        #region Private Instance Fields

        private readonly IConfiguration configuration;
        private readonly NotificationServiceOptions options;
        private readonly QueueClient queueClient;
        private readonly ILogger<NotificationService> logger;
        private readonly TelemetryClient telemetryClient;
        #endregion

        #region Public Constructor
        /// <summary>
        /// Creates a new instance of the ServiceBusNotificationService class
        /// </summary>
        public ServiceBusNotificationService(IConfiguration configuration,
                                             IOptions<NotificationServiceOptions> options,
                                             ILogger<ServiceBusNotificationService> logger)
        {
            try
            {
                this.configuration = configuration;
                if (options?.Value == null)
                {
                    throw new ArgumentNullException(nameof(options), "No configuration is defined for the notification service in the appsettings.json.");
                }

                if (options.Value.ServiceBus == null)
                {
                    throw new ArgumentNullException(nameof(options), "No ServiceBus element is defined in the configuration for the notification service in the appsettings.json.");
                }

                if (string.IsNullOrWhiteSpace(options.Value.ServiceBus.ConnectionString))
                {
                    throw new ArgumentNullException(nameof(options), "No connection string is defined in the configuration of the Service Bus notification service in the appsettings.json.");
                }

                if (string.IsNullOrWhiteSpace(options.Value.ServiceBus.QueueName))
                {
                    throw new ArgumentNullException(nameof(options), "No queue name is defined in the configuration of the Service Bus notification service in the appsettings.json.");
                }

                this.options = options.Value;
                this.logger = logger;
                
                if (this.options.ServiceBus.ConnectionString.ToLower().StartsWith("endpoint="))
                {
                    logger.LogInformation("Using Service Bus connectionString to connect to the Service Bus namespace...");
                    queueClient = new QueueClient(this.options.ServiceBus.ConnectionString,
                                                   this.options.ServiceBus.QueueName);
                }
                else if (this.options.ServiceBus.ConnectionString.ToLower().EndsWith(".servicebus.windows.net"))
                {
                    logger.LogInformation("Using System-Assigned Managed Instance to connect to the Service Bus namespace...");
                    var tokenProvider = new ManagedServiceIdentityTokenProvider();
                    queueClient = new QueueClient(this.options.ServiceBus.ConnectionString,
                                                  this.options.ServiceBus.QueueName,
                                                  tokenProvider);
                }
                else
                {
                    logger.LogError("The Service Bus connectionString format is wrong", this.options.ServiceBus.ConnectionString);
                }
                logger.LogInformation("QueueClient successfully created.ConnectionString = [{connectionstring}] QueueName = [{queuename}]",
                            this.options.ServiceBus.ConnectionString,
                                      this.options.ServiceBus.QueueName);
                var instrumentationKey = configuration["ApplicationInsights:InstrumentationKey"];
                if (string.IsNullOrWhiteSpace(instrumentationKey))
                {
                    return;
                }
                telemetryClient = new TelemetryClient { InstrumentationKey = instrumentationKey };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating an instance of the ServiceBusNotificationService class");
                throw;
            }
        }
        #endregion

        #region Public Overridden Methods
        /// <summary>
        /// Send to a notification to a given queue
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public override async Task SendNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // StartOperation is a helper method that initializes the telemetry item
            // and allows correlation of this operation with its parent and children.
            var queueName = options.ServiceBus.QueueName.FirstLetterToUpper();
            using (var operation = telemetryClient.StartOperation<DependencyTelemetry>($"SendEventTo{queueName}Queue"))
            {
                operation.Telemetry.Type = "Queue";
                operation.Telemetry.Data = $"Send {queueName}";

                var message = new Message(Encoding.UTF8.GetBytes(notification.Body))
                {
                    MessageId = Guid.NewGuid().ToString()
                };


                message.UserProperties.Add("source", "VotingData");
                message.UserProperties.Add("ParentId", operation.Telemetry.Id);
                message.UserProperties.Add("RootId", operation.Telemetry.Context.Operation.Id);

                if (notification.UserProperties != null && notification.UserProperties.Any())
                {
                    foreach (var property in notification.UserProperties)
                    {
                        message.UserProperties.Add(property.Key, property.Value);
                    }
                }

                try
                {
                    await queueClient.SendAsync(message);

                    // Set operation.Telemetry Success and ResponseCode here.
                    operation.Telemetry.Success = true;
                }
                catch (Exception e)
                {
                    telemetryClient.TrackException(e);
                    // Set operation.Telemetry Success and ResponseCode here.
                    operation.Telemetry.Success = false;
                    throw;
                }
                finally
                {
                    telemetryClient.StopOperation(operation);
                }
            }

            stopwatch.Stop();
            logger.LogInformation($"Notification sent to {options.ServiceBus.QueueName} queue in {stopwatch.ElapsedMilliseconds} ms.");
        }

        
        #endregion
    }

    #region String Class Extension Methods
    public static class StringExtensions
    {
        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
            {
                return null;
            }

            if (str.Length > 1)
            {
                return char.ToUpper(str[0]) + str.Substring(1);
            }

            return str.ToUpper();
        }
    }
    #endregion
}