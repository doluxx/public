using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;

namespace JayrideCodeChallenge.Controllers
{
    [ApiController]
    public class JayrideServiceController : ControllerBase
    {
        private readonly ILogger<JayrideServiceController> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public JayrideServiceController(ILogger<JayrideServiceController> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        [Route("/candidate")]
        public IActionResult GetCandidate()
        {
            dynamic result = new JObject();
            result.name = "test";
            result.phone = "test";

            return Ok(result);
        }

        [HttpGet]
        [Route("/location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetLocation()
        {
            var clientIP = HttpContext.Connection.RemoteIpAddress;
            string ipStackUrl = "http://api.ipstack.com/";
            string access_key = "e8a58b5a2bb7cce2d27e11b9fa2fc4ec";
            string requestUri = string.Format($"{ipStackUrl}{clientIP.MapToIPv4()}?access_key={access_key}&fields=city");

            try
            {
                var httpClient = _clientFactory.CreateClient();
                dynamic responseJson = JObject.Parse(httpClient.GetStringAsync(requestUri).Result);
                if (responseJson.success == false)
                {
                    return BadRequest();
                }

                return Ok(responseJson);
            }
            catch 
            {
                _logger.LogError("Unexpected error occurred");
                return Problem();
            }
        }

        [HttpGet]
        [Route("/listings/{passengerNum}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetListings(int passengerNum)
        {
            string getQuoteListsUri = "https://jayridechallengeapi.azurewebsites.net/api/QuoteRequest";

            try
            {
                var httpClient = _clientFactory.CreateClient();
                dynamic quoteListJson = JObject.Parse(httpClient.GetStringAsync(getQuoteListsUri).Result);
                var result = new JArray();
                foreach (var jObj in quoteListJson.listings)
                {
                    if (int.Parse(jObj.vehicleType.maxPassengers.ToString()) < passengerNum)
                        continue;

                    dynamic qualifiedList = new JObject();
                    qualifiedList.name = jObj.name;
                    qualifiedList.totalPrice = decimal.Parse(jObj.pricePerPassenger.ToString()) * passengerNum;
                    qualifiedList.vehicleType = jObj.vehicleType;

                    result.Add(qualifiedList);
                }

                if (result.Count == 0)
                {
                    return NotFound();
                }

                return Ok(result.OrderBy(jObj => jObj["totalPrice"]));

            }
            catch
            {
                _logger.LogError("Unexpected error occurred");
                return Problem();
            }
        }
    }
}
