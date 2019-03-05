#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VotingData.Services;
using VotingData.Models;
using Newtonsoft.Json;

#endregion

namespace VotingData.Controllers
{
    [Route("api/[controller]")]
    public class VoteDataController : Controller
    {
        #region Private Instance Fields
        private readonly ILogger<VoteDataController> logger;
        private readonly INotificationService notificationService;
        private readonly IRepositoryService<Vote> repositoryService;
        #endregion

        #region Public Constructors
        public VoteDataController(IRepositoryService<Vote> repositoryService,
                                  INotificationService notificationService,
                                  ILogger<VoteDataController> logger)
        {
            this.repositoryService = repositoryService;
            this.notificationService = notificationService;
            this.logger = logger;
        }
        #endregion

        #region Public Methods
        // GET api/VoteData
        [HttpGet]
        [ProducesResponseType(typeof(KeyValuePair<string, int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                logger.LogInformation(LoggingEvents.ListItems, "Listing all items");
                var items = await repositoryService.GetAllAsync();
                return Json(items.Select(vote => new KeyValuePair<string,int>(vote.Name, vote.Votes)));
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(LoggingEvents.MethodCallDuration, $"GetAll method completed in {stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        // PUT api/VoteData/name
        [HttpPut("{name}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Put(string name)
        {
            var stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();

                if (string.IsNullOrWhiteSpace(name))
                {
                    logger.LogWarning(LoggingEvents.GetItemNotFound, "Name cannot be null.");
                    return BadRequest();
                }

                var vote = repositoryService.QueryByName(name) ?? new Vote
                {
                    Name = name,
                    Votes = 0
                };

                vote.Votes++;

                await repositoryService.UpdateAsync(vote);

                logger.LogInformation(LoggingEvents.UpdateItem, "Now {name} has {votes} votes.", name, vote.Votes);

                var json = JsonConvert.SerializeObject(vote, Formatting.Indented);

                await SendNotificationAsync(json, new Dictionary<string, object> {
                    { "name", name },
                    { "votes", vote.Votes}

                });

                return Ok();
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation($"Update method completed in {stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        // DELETE api/VoteData/name
        [HttpDelete("{name}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string name)
        {
            var stopwatch = new Stopwatch();


            try
            {
                stopwatch.Start();

                if (string.IsNullOrWhiteSpace(name))
                {
                    logger.LogWarning(LoggingEvents.GetItemNotFound, "Name cannot be null.");
                    return BadRequest();
                }
                
                await repositoryService.DeleteByNameAsync(name);

                logger.LogInformation(LoggingEvents.DeleteItem, "{name} was deleted.", name);

                var vote = new Vote
                {
                    Name = name,
                    Votes = 0
                };
                var json = JsonConvert.SerializeObject(vote, Formatting.Indented);

                await SendNotificationAsync(json, new Dictionary<string, object>
                    { { "name", name },
                    { "votes", 0}}
                );

                return NoContent();
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation($"Delete method completed in {stopwatch.ElapsedMilliseconds} ms.");
            }
        }
        #endregion

        #region Private Methods

        private async Task SendNotificationAsync(string body, Dictionary<string, object> userProperties)
        {
            if (notificationService != null)
            {
                if (string.IsNullOrWhiteSpace(body) && (userProperties == null || userProperties.Count == 0))
                {
                    return;
                }
                if (userProperties == null)
                {
                    userProperties = new Dictionary<string, object>();
                }
                userProperties.Add("timestamp", DateTime.Now);
                await notificationService.SendNotificationAsync(new Notification
                {
                    Body = body,
                    UserProperties = userProperties
                });
            }
        }
        #endregion
    }
}