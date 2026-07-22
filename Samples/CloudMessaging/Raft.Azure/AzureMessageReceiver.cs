// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Newtonsoft.Json;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// This class receives messages from Azure Service Bus and pumps them into the Coyote runtime.
    /// </summary>
    internal class AzureMessageReceiver
    {
        /// <summary>
        /// The client that owns the connection to the Azure Service Bus.
        /// </summary>
        private readonly ServiceBusClient Client;

        /// <summary>
        /// The receiver for receiving messages from the topic.
        /// </summary>
        public ServiceBusReceiver SubscriptionReceiver;

        /// <summary>
        /// Id of the local actor that owns this cluster manager.
        /// </summary>
        private readonly ActorId LocalActorId;

        /// <summary>
        /// The name of the local actor.
        /// </summary>
        private readonly string LocalActorName;

        /// <summary>
        /// The Coyote runtime.
        /// </summary>
        private readonly IActorRuntime ActorRuntime;

        /// <summary>
        /// This event is raised when the cluster receives a ClientResponseEvent
        /// </summary>
        public event EventHandler<ClientResponseEvent> ResponseReceived;

        public AzureMessageReceiver(IActorRuntime runtime, string connectionString, string topicName, ActorId actorId, string subscriptionName)
        {
            this.ActorRuntime = runtime;
            this.LocalActorId = actorId;
            this.LocalActorName = (actorId == null) ? "Client" : actorId.Name;
            this.Client = new ServiceBusClient(connectionString);
            this.SubscriptionReceiver = this.Client.CreateReceiver(topicName, subscriptionName,
                new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await this.ReceiveMessagesAsync(cancellationToken);
        }

        /// <summary>
        /// Handle the receiving of messages from the Azure Message Bus
        /// </summary>
        /// <param name="cancellationToken">A way to cancel the process</param>
        /// <returns>An async task</returns>
        internal async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Receive the next message through Azure Service Bus.
                ServiceBusReceivedMessage message = await this.SubscriptionReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(50));

                // Now if the To field is empty then it is a broadcast (ClientRequest and VoteRequest)
                // otherwise ignore the message if it was meant for someone else.
                if (message != null && (string.IsNullOrEmpty(message.To) || message.To == this.LocalActorName))
                {
                    Event e = default;
                    string messageBody = message.Body.ToString();
                    if (message.Subject == "ClientRequest")
                    {
                        e = JsonConvert.DeserializeObject<ClientRequestEvent>(messageBody);
                    }
                    else if (message.Subject == "ClientResponse")
                    {
                        e = JsonConvert.DeserializeObject<ClientResponseEvent>(messageBody);
                    }
                    else if (message.Subject == "VoteRequest")
                    {
                        var request = JsonConvert.DeserializeObject<VoteRequestEvent>(messageBody);
                        // do not broadcast back to ourselves!
                        if (request.CandidateId != this.LocalActorName)
                        {
                            e = request;
                        }
                    }
                    else if (message.Subject == "VoteResponse")
                    {
                        e = JsonConvert.DeserializeObject<VoteResponseEvent>(messageBody);
                    }
                    else if (message.Subject == "AppendEntriesRequest")
                    {
                        e = JsonConvert.DeserializeObject<AppendLogEntriesRequestEvent>(messageBody);
                    }
                    else if (message.Subject == "AppendEntriesResponse")
                    {
                        e = JsonConvert.DeserializeObject<AppendLogEntriesResponseEvent>(messageBody);
                    }

                    if (e != default)
                    {
                        if (e is ClientResponseEvent clientResponse && this.ResponseReceived != null)
                        {
                            this.ResponseReceived(this, clientResponse);
                        }

                        // Special hack for the Client state machine, it is only expecting one event type, namely ClientResponseEvent
                        if (this.LocalActorId != null && (this.LocalActorName.Contains("Server") || e is ClientResponseEvent))
                        {
                            // Now bring this Service Bus message back into the Coyote framework by passing
                            // it along using the Coyote Runtime SendEvent.
                            this.ActorRuntime.SendEvent(this.LocalActorId, e);
                        }
                    }
                }
            }

            await this.SubscriptionReceiver.CloseAsync();
            await this.Client.DisposeAsync();
        }
    }
}
