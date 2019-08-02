#region Using Directives

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#endregion

namespace VotingWeb.Controllers
{
    public class VotesController : Controller
    {
        #region Private Fields
        private readonly ILogger<VotesController> logger;
        private static HttpClient httpClient;
        private static object semaphore = new object();
        #endregion

        #region Public Constructor
        public VotesController(IConfiguration configuration,
                               ILogger<VotesController> logger)
        {
            try
            {
                this.logger = logger;
                var url = configuration["VotingDataEndpoint"] ?? "votingdata";
                var endpoint = $"http://{url}/";
                lock (semaphore)
                {
                    if (httpClient == null)
                    {
                        httpClient = new HttpClient
                        {
                            BaseAddress = new Uri(endpoint)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }
        #endregion

        #region Public Methods
        // GET: api/Votes
        [HttpGet("api/votes")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = new List<KeyValuePair<string, int>>();

                
                var response = await httpClient.GetAsync("/api/VoteData");

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return Json(result);
                }

                result.AddRange(JsonConvert.DeserializeObject<List<KeyValuePair<string, int>>>(await response.Content.ReadAsStringAsync()));

                logger.LogInformation($"{result.Count} results retrieved from VotingData service");

                return Json(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }

        //// PUT: api/votes/name
        [HttpPut("api/votes/{name}")]
        public async Task<IActionResult> Put(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Ok();
                }
                logger.LogInformation($"Adding vote for {name}");

                using (var response = await httpClient.PutAsync($"/api/VoteData/{name}", null))
                {
                    return new ContentResult
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = await response.Content.ReadAsStringAsync()
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }

        //// DELETE: api/Votes/name
        [HttpDelete("api/votes/{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                logger.LogInformation($"Deleting votes for {name}");

                using (var response = await httpClient.DeleteAsync($"/api/VoteData/{name}"))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return StatusCode((int)response.StatusCode);
                    }
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }
        #endregion
    }
}