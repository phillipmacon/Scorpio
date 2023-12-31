﻿using System;
using System.Threading.Tasks;

using Scorpio.DependencyInjection;

namespace Scorpio.EventBus
{
    /// <summary>
    /// This event handler is an adapter to be able to use an action as <see cref="IEventHandler{TEvent}"/> implementation.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    internal class ActionEventHandler<TEvent> :
        IEventHandler<TEvent>,
        ITransientDependency
    {
        /// <summary>
        /// Function to handle the event.
        /// </summary>
        public Func<object,TEvent, Task> Action { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ActionEventHandler{TEvent}"/>.
        /// </summary>
        /// <param name="handler">Action to handle the event</param>
        public ActionEventHandler(Func<object,TEvent, Task> handler) => Action = handler;

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventData"></param>
        public async Task HandleEventAsync(object sender,TEvent eventData) => await Action(sender,eventData);
    }
}
