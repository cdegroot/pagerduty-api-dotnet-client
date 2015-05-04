/*
 * Copyright (c) 2015 Cees de Groot
 * Apache License, Version 2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Net;

namespace PagerDutyAPI
{
    // <summary>
    // Client information. This will expand into the "client" and "client_url"
    // fields in the Integration API. 
    // </summary>
    public class APIClientInfo {
        readonly string name;
        readonly string url;

        public APIClientInfo(string name, string url) {
            this.name = name;
            this.url = url;
        }

        public string Name {
            get { return name; }
        }
        public string Url {
            get { return url; }
        }

    }
    
    // <summary> 
    // Base class for context support. This allows you to send
    // extra rich data along with a trigger event.
    // </summary>
    public class Context {
        readonly string @type;
        
        protected Context(string context_type) {
            this.@type = context_type;
        }
    }
    
    // <summary>
    // A link-type context
    // </summary>
    public class Link : Context {
        readonly string href;
        readonly string text;
        public Link(string href, string text): base("link)") {
            this.href = href;
            this.text = text;
        }
    }
    
    // <summary>
    // An image-type context
    // </summary>
    public class Image : Context {
        readonly string src;
        readonly string href;
        public Image(string src, string href): base("image") {
            this.src = src;
            this.href = href;
        }
    }
    

    class BaseRequest {
        public string service_key { get; set; }
        public string event_type { get; set; }
        public string description { get; set; }
        public string incident_key { get; set; }
       public string details { get; set; }
    }

    // <summary>
    // Wrapper representing the JSON body for a Trigger Event
    // </summary>
    class TriggerRequest: BaseRequest {      
        public string client { get; set; }
        public string client_url { get; set; }
        public List<Context> context { get; set; }
 
        public static TriggerRequest MakeRequest(
            APIClientInfo client, string serviceKey,
            string description, string incidentKey, string data, List<Context> context) {

             return new TriggerRequest() {
                 event_type = "trigger",
                 service_key = serviceKey,
                 description = description,
                 incident_key = incidentKey,
                 client = client.Name,
                 client_url = client.Url,
                 details = data,
                 context = context,
             };
        }
    }
    
    // <summary>
    // Wrapper representing the JSON body for an Acknowledge Event
    // </summary>
    class AcknowledgeRequest: BaseRequest {        
        public static AcknowledgeRequest MakeRequest(
            string serviceKey, string description, 
            string incidentKey, string data) {

            return new AcknowledgeRequest() {
                event_type = "acknowledge",
                service_key = serviceKey,
                description = description,
                incident_key = incidentKey,               
                details = data
            };
        }
    }

    // <summary>
    // Wrapper representing the JSON body for a Resolve Event
    // </summary>
    class ResolveRequest: BaseRequest {
       public static ResolveRequest MakeRequest(
            string serviceKey,
            string description, string incidentKey, string data) {

            return new ResolveRequest() {
                event_type = "resolve",
                service_key = serviceKey,
                description = description,
                incident_key = incidentKey,
                details = data
            };
        }
    }

    // <summary>
    // Representation of API response
    // </summary>
    public class EventAPIResponse {
        public string status { get; set; }
        public string message { get; set; }
        public string incident_key { get; set; }

        public string Status { get { return status; } }
        public string Message { get { return message; } }
        public string IncidentKey { get { return incident_key; } }

        public bool IsSuccess() { 
            return "success".Equals(status); 
        }
    }

    // <summary>
    // Integration API main class. Instance of this class are bound
    // to a certain service key and API client to make the argument
    // list on actual event sending methods as small as possible
    // </summary>
    public class IntegrationAPI
    {
        const string EVENT_API_URL = "https://events.pagerduty.com//generic/2010-04-15/create_event.json";
        const string REGISTRY_PATH = "\\Software\\PagerDuty\\ServiceKeys";
        // Default retry: 0, 5, 10, 20 seconds.
        readonly Retry DEFAULT_RETRY = new Retry(TimeSpan.FromSeconds(5), 3, true);

        // <summary>
        // Make a new instance of a client, bound to the indicated client information
        // and service key.
        // </summary>
        // <param name=apiClientInfo>The client information for this instance</param>
        // <param name=serviceKey>The service key to use</param>
        // <param name=retry>The Retry instance to use</param>
        public static IntegrationAPI MakeClient(
                APIClientInfo apiClientInfo, 
                string serviceKey,
                Retry retry = null) {
            RestClient client = new RestClient(EVENT_API_URL);
            return new IntegrationAPI(client, apiClientInfo, serviceKey, retry);
        }

        // <summary>
        // Make a new instance of a client, getting the key from the Windows registry.
        // </summary>
        // <param name=apiClientInfo>The client information for this instance</param>
        // <param name=root>The registry root to use</param>
        // <param name=serviceName>The service name to look up</param>
        // <param name=retry>The Retry instance to use</param>
        public static IntegrationAPI MakeClient(
                APIClientInfo apiClientInfo, 
                string root, 
                string serviceName,
                Retry retry = null) {
            var path = root + REGISTRY_PATH;
            var key = (string) Microsoft.Win32.Registry.GetValue(path, serviceName, "notfound");
            if (key == null || key.Equals("notfound")) {
                throw new ApplicationException("Registry value for service " + serviceName + " not found in " + path);
            }
            return MakeClient(apiClientInfo, key, retry);
        }

        readonly RestClient client;
        readonly APIClientInfo apiClientInfo;
        readonly string serviceKey;
        readonly Retry retry;

        private IntegrationAPI(RestClient client, APIClientInfo apiClientInfo, string serviceKey, Retry retry) {
            this.client = client;
            this.serviceKey = serviceKey;
            this.apiClientInfo = apiClientInfo;
			this.retry = retry ?? DEFAULT_RETRY;
        }

        // <summary>
        // Send a trigger to PagerDuty
        // </summary>
        // <param name="description">The description (summary) of the trigger</param>
        // <param name="data">Extra optional data to send along</param>
        // <param name="incidentKey">The incidentKey (if null, PagerDuty will create one)</param>
        public EventAPIResponse Trigger(string description, string data, string incidentKey = null, List<Context> context = null) {
            var trigger = TriggerRequest.MakeRequest(apiClientInfo, serviceKey, description, incidentKey, data, context);
            return Execute(trigger);
        }

        
        // <summary>
        // Send an acknowledgement to PagerDuty
        // </summary>
        // <param name="incidentKey">The incident key for the open incident</param>
        // <param name="description">Description for the acknowledgement</param>
        // <param name="data">Extra optional data to send along</param>
        public EventAPIResponse Acknowledge(string incidentKey, string description, string data) {
            var acknowledge = AcknowledgeRequest.MakeRequest(serviceKey, description, incidentKey, data);
            return Execute(acknowledge);
        }

        // <summary>
        // Send a resolve to PagerDuty
        // </summary>
        // <param name="incidentKey">The incident key for the open incident</param>
        // <param name="description">Description for the resolve</param>
        // <param name="data">Extra optional data to send along</param>
        public EventAPIResponse Resolve(string incidentKey, string description, string data) {
            var resolve = ResolveRequest.MakeRequest(serviceKey, description, incidentKey, data);
            return Execute(resolve);
        }

        // Send the request and return the resulting data (or an error)
        private EventAPIResponse Execute(BaseRequest request) {
            return retry.Do(() => TryExecute(request));

        }

        private IEither<Exception,EventAPIResponse> TryExecute(BaseRequest request) {
            var restRequest = new RestRequest(Method.POST);
            restRequest.AddJsonBody(request);
            var response = client.Execute<EventAPIResponse>(restRequest);
            if (response.ErrorException != null) {
                const string message = "Error retrieving response. Check inner details";
                return Either.Left<Exception, EventAPIResponse>(
                    new ApplicationException(message, response.ErrorException));
            }
         
            if (response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.InternalServerError ||
                response.StatusCode == HttpStatusCode.BadGateway ||
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.GatewayTimeout) {
                
                return Either.Left<Exception, EventAPIResponse>(
                    new ApplicationException("Bad HTTP Status Code: " + response.StatusCode));
            }

            return Either.Right<Exception, EventAPIResponse>(response.Data);
        }       
    }
}
