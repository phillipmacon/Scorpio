﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using Scorpio.Data;
using Scorpio.Initialization;
using Scorpio.Threading;

namespace Scorpio.EventBus
{
    internal class RabbitEventBus : EventBusBase, IInitializable
    {
        private readonly IBus _bus;
        private readonly RabbitMQEventBusOptions _options;
#if NET5_0_OR_GREATER
        private Exchange _exchange;
        private Queue _queue;
#else
        private IExchange _exchange;
        private IQueue _queue;

#endif      
        protected ConcurrentDictionary<string, Type> EventTypes { get; }
        /// <summary>
        /// Reference to the Logger.
        /// </summary>
        public ILogger<RabbitEventBus> Logger { get; }

        public IRabbitMqEventDataSerializer Serializer { get; }

        public RabbitEventBus(IServiceProvider serviceProvider, IBus bus, IEventErrorHandler errorHandler, IRabbitMqEventDataSerializer serializer, IOptions<RabbitMQEventBusOptions> options) : base(serviceProvider, errorHandler, options)
        {
            Logger = serviceProvider.GetService<ILogger<RabbitEventBus>>();
            _bus = bus;
            Serializer = serializer;
            _options = options.Value;
            EventTypes = new ConcurrentDictionary<string, Type>();
        }
        public virtual void Initialize()
        {
            _exchange = _bus.Advanced.ExchangeDeclare(_options.ExchangeName, c => c.AsDurable(true).WithType("direct"));
            _queue = _bus.Advanced.QueueDeclare(_options.ClientName, c => c.AsDurable(true).AsExclusive(false).AsAutoDelete(false));
            _bus.Advanced.Consume(_queue, ProcessEventAsync);
            SubscribeHandlers(Options.GetEventHandlers());
        }

#if NET5_0_OR_GREATER
        private async Task<AckStrategy> ProcessEventAsync(ReadOnlyMemory<byte> buffer, MessageProperties messageProperties, MessageReceivedInfo messageReceivedInfo)
#else
        private async Task<AckStrategy> ProcessEventAsync(byte[] buffer, MessageProperties messageProperties, MessageReceivedInfo messageReceivedInfo)
#endif
        {
            var eventName = messageReceivedInfo.RoutingKey;
            var eventType = EventTypes.GetOrDefault(eventName);
            if (eventType == null)
            {
                return AckStrategies.Ack;
            }
            var eventData = Serializer.Deserialize(buffer, eventType);
            await TriggerHandlersAsync(eventType, null, eventData, errorContext =>
            {
                var retryAttempt = 0;
                if (messageProperties.Headers != null &&
                    messageProperties.Headers.ContainsKey(EventErrorHandlerBase.RetryAttemptKey))
                {
                    retryAttempt = (int)messageProperties.Headers[EventErrorHandlerBase.RetryAttemptKey];
                }
                errorContext.EventData = eventData;
                errorContext.SetProperty(EventErrorHandlerBase.HeadersKey, messageProperties);
                errorContext.SetProperty(EventErrorHandlerBase.RetryAttemptKey, retryAttempt);
            });
            return AckStrategies.Ack;
        }

        public override Task PublishAsync(object sender, Type eventType, object eventData) => PublishAsync(eventType, eventData, null);

        public Task PublishAsync(Type eventType, object eventData, MessageProperties properties, Dictionary<string, object> headersArguments = null)
        {
            var eventName = EventNameAttribute.GetNameOrDefault(eventType);
            var body = Serializer.Serialize(eventData);
            if (properties == null)
            {
                properties = new MessageProperties
                {
                    DeliveryMode = 2,
                    MessageId = Guid.NewGuid().ToString("N")
                };
            }
            SetEventMessageHeaders(properties, headersArguments);
            return _bus.Advanced.PublishAsync(_exchange, eventName, true, properties, body);
        }

        private void SetEventMessageHeaders(MessageProperties properties, Dictionary<string, object> headersArguments)
        {
            if (headersArguments == null)
            {
                return;
            }

            properties.Headers ??= new Dictionary<string, object>();

            foreach (var header in headersArguments)
            {
                properties.Headers[header.Key] = header.Value;
            }
        }

        public override IDisposable Subscribe(Type eventType, IEventHandlerFactory factory)
        {
            GetOrCreateHandlerFactories(eventType)
                           .Locking(factories =>
                           {
                               factories.Add(factory);
                               if (factories.Count == 1)
                               {
                                   factories.Binding = _bus.Advanced.Bind(_exchange, _queue, EventNameAttribute.GetNameOrDefault(eventType));
                               }
                               return factories;
                           });

            return new DisposeAction(() => Unsubscribe(eventType, factory));
        }


        public override void Unsubscribe(Type eventType, IEventHandlerFactory factory) =>
            GetOrCreateHandlerFactories(eventType).Locking(factories => factories.Action(f => f.Remove(factory)))
                                                  .Action(f => !f.Any(), f => _bus.Advanced.Unbind(f.Binding));

        public override void UnsubscribeAll(Type eventType) => GetOrCreateHandlerFactories(eventType).Locking(factories => factories.Action(f => f.Clear()).Action(f => _bus.Advanced.Unbind(f.Binding)));


        private new EventHandlerFactoryList GetOrCreateHandlerFactories(Type eventType)
        {
            return HandlerFactories.GetOrAdd(eventType, (type) =>
            {
                var eventName = EventNameAttribute.GetNameOrDefault(type);
                EventTypes[eventName] = type;
                return new EventHandlerFactoryList();
            }) as EventHandlerFactoryList;
        }

        private class EventHandlerFactoryList : List<IEventHandlerFactory>
        {
#if NET5_0_OR_GREATER
            public Binding<Queue> Binding { get; set; }
#else
            public IBinding Binding { get; set; }

#endif   
        }
    }
}
