using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ApiPassthrough.Controllers
{

    [RoutePrefix("")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DefaultController : ApiController
    {
        protected JsonSerializerSettings CamelCaseResolver { get; set; }
        public DefaultController()
        {
            CamelCaseResolver = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }
        private HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri(ConfigurationManager.AppSettings["apiUrl"])
        };

        [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch, Route("{*url}")]
        public async System.Threading.Tasks.Task<dynamic> PerformRequest(string url)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                HttpResponseMessage response = null;
                HttpContent requestBody = null;
                string requestUrl = Request.RequestUri.LocalPath;
                string requestMethod = Request.Method.ToString();

                switch (requestMethod)
                {
                    case "GET":
                        response = await httpClient.GetAsync(requestUrl);
                        break;
                    case "POST":
                        requestBody = Request.Content;
                        response = await httpClient.PostAsync(requestUrl, requestBody);
                        break;
                    case "PUT":
                        requestBody = Request.Content;
                        response = await httpClient.PutAsync(requestUrl, requestBody);
                        break;
                    case "DELETE":
                        response = await httpClient.DeleteAsync(requestUrl);
                        break;
                    case "PATCH":
                        response = await httpClient.PutAsync(requestUrl, requestBody);
                        break;
                }
                var json = await response.Content.ReadAsStringAsync();
                json = Regex.Unescape(json).Replace("\r\n", string.Empty).Trim('"');
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
            catch(Exception e)
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ReasonPhrase = e.Message
                };
            }
        }
    }
}
