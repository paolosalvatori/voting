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
        private readonly HttpClient httpClient;
        #endregion

        #region Public Constructor
        public VotesController(IConfiguration configuration,
                               ILogger<VotesController> logger)
        {
            try
            {
                this.logger = logger;
                var endpoint = $"http://{configuration["VotingDataEndpoint"] ?? "votingdata"}/api/VoteData";
                httpClient = new HttpClient
                {
                    BaseAddress = new Uri(endpoint)
                };
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

                
                var response = await httpClient.GetAsync(string.Empty);

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

                using (var response = await httpClient.PutAsync($"/{name}", null))
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

                using (var response = await httpClient.DeleteAsync($"/{name}"))
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