﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cqrs.Events;
using EventStore.ClientAPI;

namespace Cqrs.EventStore
{
	public class EventStore<TAuthenticationToken> : IEventStore<TAuthenticationToken>
	{
		private const string CqrsEventStoreStreamNamePattern = "{0}/{1}";

		protected IEventBuilder<TAuthenticationToken> EventBuilder { get; set; }

		protected IEventDeserialiser<TAuthenticationToken> EventDeserialiser { get; set; }

		protected IEventStoreConnectionHelper EventStoreConnectionHelper { get; set; }

		public EventStore(IEventBuilder<TAuthenticationToken> eventBuilder, IEventDeserialiser<TAuthenticationToken> eventDeserialiser, IEventStoreConnectionHelper eventStoreConnectionHelper)
		{
			EventBuilder = eventBuilder;
			EventDeserialiser = eventDeserialiser;
			EventStoreConnectionHelper = eventStoreConnectionHelper;
		}

		#region Implementation of IEventStore

		public void Save<T>(IEvent<TAuthenticationToken> @event)
		{
			EventData eventData = EventBuilder.CreateFrameworkEvent(@event);
			using (IEventStoreConnection connection = EventStoreConnectionHelper.GetEventStoreConnection())
			{
				connection.AppendToStream(string.Format(CqrsEventStoreStreamNamePattern, typeof(T).FullName, @event.Id), ExpectedVersion.Any, new[] { eventData });
			}
		}

		/// <remarks>
		/// The value of <paramref name="fromVersion"/> is zero based but the internals indexing of the EventStore is offset by <see cref="StreamPosition.Start"/>.
		/// Adjust the value of <paramref name="fromVersion"/> by <see cref="StreamPosition.Start"/>
		/// </remarks>
		public IEnumerable<IEvent<TAuthenticationToken>> Get<T>(Guid aggregateId, int fromVersion = -1)
		{
			StreamEventsSlice eventCollection;
			int startPosition = StreamPosition.Start;
			if (fromVersion > -1)
				startPosition = fromVersion + StreamPosition.Start;
			using (IEventStoreConnection connection = EventStoreConnectionHelper.GetEventStoreConnection())
			{
				eventCollection = connection.ReadStreamEventsForward(string.Format(CqrsEventStoreStreamNamePattern, typeof(T).FullName, aggregateId), startPosition, 200, false);
			}
			return eventCollection.Events.Select(EventDeserialiser.Deserialise);
		}

		#endregion

		protected virtual void ListenForNotificationsOnConnection(IEventStoreConnection connection)
		{
			connection.SubscribeToAll(true, DisplayNotificationArrival, DisplaySubscriptionDropped);
		}

		private void DisplayNotificationArrival(EventStoreSubscription subscription, ResolvedEvent notification)
		{
			RecordedEvent @event = notification.Event;
			const string chatClientEventTypePrefix = "Cqrs EventStore Client ";
			if (/*string.Format("{0}{1}", chatClientEventTypePrefix, ProcessName) == @event.EventType ||*/ !@event.EventType.StartsWith(chatClientEventTypePrefix))
				return;
			Console.WriteLine("{0} : {1}", @event.EventType.Substring(chatClientEventTypePrefix.Length), @event.EventType);
		}

		private void DisplaySubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reasonDropped, Exception exception)
		{
			Console.WriteLine("Opps");
		}
	}
}
