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
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;
using VotingData.Models;
#endregion

namespace VotingData.Services
{
    /// <summary>
    ///  This class is used to read, write, delete and update data from Cosmos DB using Document DB API. 
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class CosmosDbRepositoryService<T> : IRepositoryService<T> where T : Entity, new()
    {
        #region Private Instance Fields
        private readonly RepositoryServiceOptions repositoryServiceOptions;
        private readonly DocumentClient documentClient;
        private readonly ILogger<CosmosDbRepositoryService<T>> logger;
        #endregion

        #region Public Constructor
        /// <summary>
        /// Creates a new instance of the ServiceBusNotificationService class
        /// </summary>
        public CosmosDbRepositoryService(IOptions<RepositoryServiceOptions> options,
                                         ILogger<CosmosDbRepositoryService<T>> logger)
        {
            if (options?.Value == null)
            {
                throw new ArgumentNullException(nameof(options), "No configuration is defined for the repository service in the appsettings.json.");
            }

            if (options.Value.CosmosDb == null)
            {
                throw new ArgumentNullException(nameof(options), "No CosmosDb element is defined in the configuration for the notification service in the appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(options.Value.CosmosDb.EndpointUri))
            {
                throw new ArgumentNullException(nameof(options), "No endpoint uri is defined in the configuration of the Cosmos DB notification service in the appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(options.Value.CosmosDb.PrimaryKey))
            {
                throw new ArgumentNullException(nameof(options), "No primary key is defined in the configuration of the Cosmos DB notification service in the appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(options.Value.CosmosDb.DatabaseName))
            {
                throw new ArgumentNullException(nameof(options), "No database name is defined in the configuration of the Cosmos DB notification service in the appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(options.Value.CosmosDb.CollectionName))
            {
                throw new ArgumentNullException(nameof(options), "No collection name is defined in the configuration of the Cosmos DB notification service in the appsettings.json.");
            }

            repositoryServiceOptions = options.Value;
            this.logger = logger;

            documentClient = new DocumentClient(new Uri(repositoryServiceOptions.CosmosDb.EndpointUri),
                                                 options.Value.CosmosDb.PrimaryKey,
                                                 new ConnectionPolicy
                                                 {
                                                     //ConnectionMode = ConnectionMode.Direct, // Not supported when running the ASP.NET Core app in a Docker container
                                                     ConnectionProtocol = Protocol.Tcp,
                                                     RequestTimeout = TimeSpan.FromMinutes(5)
                                                 });
            CreateDatabaseAndDocumentCollectionIfNotExistsAsync().Wait();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Query a Document by ID from the Azure Cosmos DB database service as an asynchronous operation.
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>A Task that wraps the T entity retrieved.</returns>
        public T QueryByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentNullException(nameof(name), "The name cannot null or empty.");
                }
                var feedOptions = new FeedOptions { 
                    EnableCrossPartitionQuery = true 
                };
                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                                         repositoryServiceOptions.CosmosDb.CollectionName);
                return documentClient.CreateDocumentQuery<T>(documentCollectionUri, feedOptions)
                    .Where(so => so.Name == name)
                    .AsEnumerable()
                    .FirstOrDefault();
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.ListItems,
                                        e,
                                        $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.ListItems,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Reads a Document from the Azure DocumentDB database service as an asynchronous operation.
        /// </summary>
        /// <param name="id">Document id</param>
        /// <returns>A Task that wraps the T entity retrieved.</returns>
        public async Task<T> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentNullException(nameof(id), "Id cannot null or empty.");
                }
                var documentUri = UriFactory.CreateDocumentUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                               repositoryServiceOptions.CosmosDb.CollectionName,
                                                               id);
                var response = await documentClient.ReadDocumentAsync<T>(documentUri);
                return response.Document;
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                 e,
                                 $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Reads all the documents in the document collection.
        /// </summary>
        /// <returns>A Task that wraps a collection of T entities.</returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                                                   repositoryServiceOptions.CosmosDb.CollectionName);
                var feedOptions = new FeedOptions { 
                    MaxItemCount = -1, 
                    EnableCrossPartitionQuery = true 
                };
                var query = documentClient.CreateDocumentQuery<T>(documentCollectionUri, feedOptions);
                return await Task.FromResult<IEnumerable<T>>(query.ToList());
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.ListItems,
                                 e,
                                 $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.ListItems,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Creates a Document as an asynchronous operation in the Azure DocumentDB database service.
        /// </summary>
        /// <param name="entity">The entity to create</param>
        /// <returns>A task.</returns>
        public async Task CreateAsync(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity), "The entity cannot null.");
                }

                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                                                   repositoryServiceOptions.CosmosDb.CollectionName);
                await documentClient.CreateDocumentAsync(documentCollectionUri, entity);
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                 e,
                                 $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Updates a Document as an asychronous operation in the Azure DocumentDB database service.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <returns>A task.</returns>
        public async Task UpdateAsync(T entity)
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity), "The entity cannot null.");
                }

                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                                                   repositoryServiceOptions.CosmosDb.CollectionName);
                await documentClient.UpsertDocumentAsync(documentCollectionUri, entity);
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                 e,
                                 $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Deletes a Document by Id from the Azure DocumentDB database service as an asynchronous operation.
        /// </summary>
        /// <param name="id">Document id</param>
        /// <returns>A Task that wraps the T entity retrieved.</returns>
        public async Task DeleteByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentNullException(nameof(id), "Id cannot null or empty.");
                }
                var documentUri = UriFactory.CreateDocumentUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                               repositoryServiceOptions.CosmosDb.CollectionName,
                                                               id);
                await documentClient.DeleteDocumentAsync(documentUri);
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                 e,
                                 $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                                e,
                                $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }

        /// <summary>
        /// Deletes a Document by Name from the Azure DocumentDB database service as an asynchronous operation.
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>A Task that wraps the T entity retrieved.</returns>
        public async Task DeleteByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentNullException(nameof(name), "The name cannot null or empty.");
                }
                var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                                                                                   repositoryServiceOptions.CosmosDb.CollectionName);
                var feedOptions = new FeedOptions {
                    EnableCrossPartitionQuery = true
                };
                var document = documentClient.
                    CreateDocumentQuery<T>(documentCollectionUri, feedOptions)
                    .Where(so => so.Name == name)
                    .AsEnumerable()
                    .FirstOrDefault();
                if (document == null)
                {
                    return;
                }
                var documentUri = UriFactory.
                    CreateDocumentUri(repositoryServiceOptions.CosmosDb.DatabaseName,
                    repositoryServiceOptions.CosmosDb.CollectionName,
                    document.Id);

                await documentClient.DeleteDocumentAsync(documentUri);
            }
            catch (DocumentClientException e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                    e,
                    $"An error occurred: StatusCode=[{e.StatusCode}] Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                logger.LogError(LoggingEvents.GetItem,
                    e,
                    $"An error occurred: Message=[{e.Message}] BaseException=[{baseException?.Message ?? "NULL"}]");
                throw;
            }
        }
        #endregion

        #region Private Instance Fields
        private async Task CreateDatabaseAndDocumentCollectionIfNotExistsAsync()
        {
            await documentClient.CreateDatabaseIfNotExistsAsync(new Database
            {
                Id = repositoryServiceOptions.CosmosDb.DatabaseName
            });
            await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(repositoryServiceOptions.CosmosDb.DatabaseName),
            new DocumentCollection
            {
                Id = repositoryServiceOptions.CosmosDb.CollectionName
            });
        }
        #endregion
    }
}
