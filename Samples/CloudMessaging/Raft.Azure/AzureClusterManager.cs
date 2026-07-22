// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Coyote.Actors;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    internal class AzureClusterManager : ClusterManager
    {
        [DataContract]
        public class RegisterMessageBusEvent : Event
        {
            public ServiceBusSender TopicSender;
        }

        public ServiceBusSender TopicSender;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            var reg = initialEvent as RegisterMessageBusEvent;
            this.TopicSender = reg.TopicSender;
            return base.OnInitializeAsync(initialEvent);
        }

        public override async Task BroadcastVoteRequestAsync(Event e)
        {
            var request = e as VoteRequestEvent;
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(request))
            {
                Subject = "VoteRequest",
                ReplyTo = request.CandidateId
            };

            await this.TopicSender.SendMessageAsync(message);
        }

        public override async Task SendVoteResponseAsync(Event e)
        {
            var response = e as VoteResponseEvent;

            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(response))
            {
                Subject = "VoteResponse",
                To = response.TargetId
            };

            await this.TopicSender.SendMessageAsync(message);
        }

        public override async Task BroadcastClientRequestAsync(Event e)
        {
            var req = e as ClientRequestEvent;
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(req))
            {
                Subject = "ClientRequest"
            };

            await this.TopicSender.SendMessageAsync(message);
        }

        public override async Task SendClientResponseAsync(Event e)
        {
            var response = e as ClientResponseEvent;
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(response))
            {
                Subject = "ClientResponse"
            };

            await this.TopicSender.SendMessageAsync(message);
        }

        public override async Task SendAppendEntriesRequestAsync(Event e)
        {
            var request = e as AppendLogEntriesRequestEvent;
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(request))
            {
                Subject = "AppendEntriesRequest",
                To = request.To,
                ReplyTo = request.LeaderId
            };

            await this.TopicSender.SendMessageAsync(message);
        }

        public override async Task SendAppendEntriesResponseAsync(Event e)
        {
            var response = e as AppendLogEntriesResponseEvent;
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(response))
            {
                Subject = "AppendEntriesResponse",
                To = response.To,
                ReplyTo = response.SenderId
            };

            await this.TopicSender.SendMessageAsync(message);
        }
    }
}
