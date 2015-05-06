PagerDuty .NET API
==================

This project encapsulates the PagerDuty API in a .NET DLL. At the moment,
only the [Integrations API](https://developer.pagerduty.com/documentation/integration/events)
is supported. 

Installation
------------

The DLL is available on [NuGet](https://www.nuget.org/packages/PagerDutyAPI).

Usage
-----

There are two ways to create a client - by directly passing a service key, or 
by passing a service name and a registry root. The latter one is more Windows-like
and lets you keep your service keys safe and all together. Example (more example
code in the unit test suite):

    var client = PagerDutyAPI.IntegrationAPI.MakeClient(apiClientInfo, "HKEY_CURRENT_USER", "Test Service");
            
This will grab the keys from the current user hive using the path ```Software\PagerDuty\ServiceKeys``` relative to the root.

Note that to run the included tests, you need to have a valid API key in the registry under that path 
with the name "Test Service". If you don't want to use the registry, you can pass in the API key directly:

    var client = PagerDutyAPI.IntegrationAPI.MakeClient(apiClientInfo, "<your service key here>");
	
Both calls take an ```APIClientInfo``` object, which is basically a simple wrapper to make the above argument lists a little less noisy. It contains the client info which is passed on to PagerDuty	and will show in the various interfaces as a link for more details (for example, a link back to the monitoring system, etcetera). Creating it is just a matter of:

    var apiClientInfo = new APIClientInfo("PagerDuty", "http://www.pagerduty.com");
	
Once you have the client, there are three calls you can make to it corresponding to PagerDuty's Integration API Events:

    var incidentKey = System.Guid.NewGuid().ToString();
    var data = new Dictionary<String, String> {
        {"what", "the roof"},
        {"state", "on fire"}
    };
    var contexts = new List<Context> {
    new Link("http://www.pagerduty.com", "PagerDuty site"),
    new Image("http://media.giphy.com/media/dV7g3UEFtohfG/giphy.gif", "http://giphy.com")
    };

    var response = client.Trigger("test event", data, incidentKey, contexts);	

In all cases, you get an ```EventAPIResponse``` object back that contains ```Status```, ```Message``` and ```IncidentKey``` properties corresponding to the json returned by the Integration API. 

Retries
-------

By default, the library will retry whenever the result from the HTTP call indicates a retry is necessary. The default is fast enough to not slow down client processes unnecessarily and slow enough to be nice to PagerDuty. You can override the retry mechanism in the ```MakeClient``` calls, see the code for details. 

License
-------

[Apache 2](http://www.apache.org/licenses/LICENSE-2.0)

Contributing
------------

This is a personal project which I may or may not support in my spare time,
so please submit PRs rather than issues, you have a much better chance of
getting stuff fixed that way :-)

1. Fork it ( https://github.com/cdegroot/pagerduty-api-dotnet-client/fork )
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request
