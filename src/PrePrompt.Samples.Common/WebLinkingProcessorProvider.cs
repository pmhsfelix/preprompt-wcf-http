﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using Microsoft.ServiceModel.Description;
using Microsoft.ServiceModel.Dispatcher;
using Microsoft.ServiceModel.Http;

namespace PrePrompt.Samples.Common
{
    public class WebLinkingProcessorProvider : IProcessorProvider
    {
        private readonly IProcessorProvider _inner;

        public WebLinkingProcessorProvider(WebLinksRegistry config, IProcessorProvider inner)
        {
            LinksRegistry = config;
            _inner = inner;
        }

        public WebLinksRegistry LinksRegistry { get; private set; }

        public void RegisterRequestProcessorsForOperation(HttpOperationDescription operation, IList<Processor> processors,
                                                          MediaTypeProcessorMode mode)
        {
            if (_inner != null)
            {
                _inner.RegisterRequestProcessorsForOperation(operation, processors, mode);
            }
            processors.Add(new WebLinkingProcessor(LinksRegistry, operation, mode));
        }

        public void RegisterResponseProcessorsForOperation(HttpOperationDescription operation, IList<Processor> processors,
                                                           MediaTypeProcessorMode mode)
        {
            if (_inner != null)
            {
                _inner.RegisterResponseProcessorsForOperation(operation, processors, mode);
            }
            processors.Add(new WebLinkingProcessor(LinksRegistry, operation, mode));
        }
    }

    internal class WebLinkingProcessor : Processor
    {
        private readonly WebLinksRegistry _registry;
        private readonly MethodInfo _method;
        private readonly MediaTypeProcessorMode _mode;

        public WebLinkingProcessor(WebLinksRegistry registry, HttpOperationDescription httpOperationDescription,
                                   MediaTypeProcessorMode mode)
        {
            _registry = registry;
            _method = httpOperationDescription.SyncMethod ?? httpOperationDescription.BeginMethod;
            _mode = mode;
        }

        protected override ProcessorResult OnExecute(object[] input)
        {
            var httpResponse = (HttpResponseMessage)input[0];
            WebLinkCollection linkCollection;

            if (_mode == MediaTypeProcessorMode.Request)
            {
                linkCollection = _registry.GetLinksFor(_method);
                httpResponse.GetProperties().Add(linkCollection);
            }
            else
            {
                linkCollection = httpResponse.GetProperties().OfType<WebLinkCollection>().Single();
                addLinkHeader(httpResponse, linkCollection);
            }

            return new ProcessorResult { Output = new object[] { linkCollection } };
        }

        protected override IEnumerable<ProcessorArgument> OnGetInArguments()
        {
            return new[] { new ProcessorArgument(HttpPipelineFormatter.ArgumentHttpResponseMessage, typeof(HttpResponseMessage)) };
        }

        protected override IEnumerable<ProcessorArgument> OnGetOutArguments()
        {
            return new[] { new ProcessorArgument("webLinks", typeof(WebLinkCollection)) };
        }

        private static void addLinkHeader(HttpResponseMessage httpResponse, WebLinkCollection linkCollection)
        {
            var links = linkCollection
                .Links
                .Aggregate(new StringBuilder(), (b, target) => b.Append(extractLinkDescription(target)).Append(','))
                .RemoveLastCharacter()
                .ToString();

            if (links.IsNullOrEmpty() == false)
            {
                httpResponse.Headers.AddWithoutValidation("Link", links);
            }
        }

        private static string extractLinkDescription(WebLinkTarget target)
        {
            //
            // TODO: Add support for properties and multiple relation types.
            //

            return "<{0}>;rel=\"{1}\"".FormatWith(target.Uri, target.RelationType);
        }
    }
}