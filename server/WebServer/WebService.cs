using System;
using System.Web.Http.SelfHost;
using System.Web.Http;

namespace RemoteInvoker
{
    class WebService
    {  
        public static void StartWebService()
        {
            /*
             * using the following example:
             * https://www.dotnetcurry.com/aspnet/896/self-host-aspnet-webapi-without-iis
             * use http://localhost/api/Contacts to retrieve xml with contacts
             */

            Uri baseAddres;
            baseAddres = new Uri("https://localhost:443");

            // Set up server configuration
            HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(baseAddres);
            config.MaxReceivedMessageSize = 2147483647;
            config.Routes.MapHttpRoute(
              name: "DefaultApi",
              routeTemplate: "api/{controller}/{id}",
              defaults: new { id = RouteParameter.Optional }
            );
            config.MessageHandlers.Add(new RequestHandler());


            // Create server
            var server = new HttpSelfHostServer(config);
            // Start listening
            server.OpenAsync().Wait();
            Console.WriteLine("Web API Self hosted on " + baseAddres);
        }
    }
}
