﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using Microsoft.Net.Http;
using Microsoft.Xml.Linq;
using PrePrompt.Samples.Client.Authentication;
using PrePrompt.Samples.Client.Twitter;
using PrePrompt.Samples.Common;

namespace PrePrompt.Samples.Client
{
    public class Program
    {
        private const string TWITTER = "http://api.twitter.com/";

        public static void Main()
        {
            var client = new HttpClient("http://code.deetc.e.ipl.pt/");
            client.Channel = new BasicAuthenticationChannel(new WebRequestChannel(),
                                                            _ => Tuple.Create("", ""), 
                                                            client);

            var response = client.Get("ls/1011v/svn/private/");
            response.EnsureSuccessStatusCode();
            Console.WriteLine(response);
        }

        public class JsonDataContractFormatter : IContentFormatter
        {
            private readonly DataContractJsonSerializer _serializer;

            public JsonDataContractFormatter(DataContractJsonSerializer serializer)
            {
                _serializer = serializer;
            }

            public IEnumerable<MediaTypeHeaderValue> SupportedMediaTypes
            {
                get { return new[] { new MediaTypeHeaderValue("application/json") }; }
            }

            public void WriteToStream(object instance, Stream stream)
            {
                _serializer.WriteObject(stream, instance);
            }

            public object ReadFromStream(Stream stream)
            {
                return _serializer.ReadObject(stream);
            }
        }

        private static void twitterExample()
        {
            var client = new HttpClient(TWITTER)
            {
                Channel = new HttpMessageInspector(new WebRequestChannel())
            };

            var request = new HttpRequestMessage(HttpMethod.Get, "1/users/show/{0}.xml".FormatWith("duarte_nunes"));
            request.With().Accept("application/xml");

            HttpResponseMessage response = client.Send(request);
            response.EnsureSuccessStatusCode();

            Console.WriteLine(response.Content.ReadAsString());

            XElement element = response.Content.ReadAsXElement();
            Console.WriteLine(element.Element("description").Value);

            var formatter = new JsonDataContractFormatter(new DataContractJsonSerializer(typeof (TwitterUser)));
            var c = new RestyClient<TwitterUser>(TWITTER, new[] { formatter });
            Tuple<HttpStatusCode, TwitterUser> result = c.ExecuteGet("1/users/show/{0}.json".FormatWith("duarte_nunes"));
            Console.WriteLine(result.Item1);
            Console.WriteLine(result.Item2.Description);
            Console.WriteLine(result.Item2.ProfileImageUrl);
        }
    }
}