﻿#region Copyright
// // -----------------------------------------------------------------------
// // <copyright company="cdmdotnet Limited">
// // 	Copyright cdmdotnet Limited. All rights reserved.
// // </copyright>
// // -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cqrs.Events;
using Cqrs.Logging;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace Cqrs.Azure.DocumentDb.Events
{
	public class AzureDocumentDbEventStore<TAuthenticationToken> : EventStore<TAuthenticationToken>
	{
		protected IAzureDocumentDbEventStoreConnectionHelper AzureDocumentDbEventStoreConnectionHelper { get; private set; }

		protected IAzureDocumentDbHelper AzureDocumentDbHelper { get; private set; }

		public AzureDocumentDbEventStore(IEventBuilder<TAuthenticationToken> eventBuilder, IEventDeserialiser<TAuthenticationToken> eventDeserialiser, ILog logger, IAzureDocumentDbHelper azureDocumentDbHelper, IAzureDocumentDbEventStoreConnectionHelper azureDocumentDbEventStoreConnectionHelper)
			: base(eventBuilder, eventDeserialiser, logger)
		{
			AzureDocumentDbHelper = azureDocumentDbHelper;
			AzureDocumentDbEventStoreConnectionHelper = azureDocumentDbEventStoreConnectionHelper;
		}

		public override IEnumerable<IEvent<TAuthenticationToken>> Get<T>(Guid aggregateId, bool useLastEventOnly = false, int fromVersion = -1)
		{
			return GetAsync<T>(aggregateId, useLastEventOnly, fromVersion).Result;
		}

		protected async Task<IEnumerable<IEvent<TAuthenticationToken>>> GetAsync<T>(Guid aggregateId, bool useLastEventOnly = false, int fromVersion = -1)
		{
			using (DocumentClient client = AzureDocumentDbEventStoreConnectionHelper.GetEventStoreConnectionClient())
			{
				Database database = AzureDocumentDbHelper.CreateOrReadDatabase(client, AzureDocumentDbEventStoreConnectionHelper.GetEventStoreConnectionLogStreamName()).Result;
				DocumentCollection collection = AzureDocumentDbHelper.CreateOrReadCollection(client, database, "CqrsEventStore").Result;

				IOrderedQueryable<EventData> query = client.CreateDocumentQuery<EventData>(collection.SelfLink);
				string streamName = string.Format(CqrsEventStoreStreamNamePattern, typeof(T).FullName, aggregateId);

				IEnumerable<EventData> results = query.Where(x => x.AggregateId == streamName);

				return results
					.ToList()
					.OrderByDescending(x => x.EventId)
					.Select(EventDeserialiser.Deserialise);
			}
		}

		protected override async void PersitEvent(EventData eventData)
		{
			Logger.LogInfo("Persisting aggregate root event", string.Format("{0}\\PersitEvent", GetType().Name));
			try
			{
				using (DocumentClient client = AzureDocumentDbEventStoreConnectionHelper.GetEventStoreConnectionClient())
				{
					Database database = AzureDocumentDbHelper.CreateOrReadDatabase(client, AzureDocumentDbEventStoreConnectionHelper.GetEventStoreConnectionLogStreamName()).Result;
					DocumentCollection collection = AzureDocumentDbHelper.CreateOrReadCollection(client, database, "CqrsEventStore").Result;

					Logger.LogInfo("Creating document for event asyncronously", string.Format("{0}\\PersitEvent", GetType().Name));
					await client.CreateDocumentAsync(collection.SelfLink, eventData);
				}
			}
			finally
			{
				Logger.LogInfo("Persisting aggregate root event... Done", string.Format("{0}\\PersitEvent", GetType().Name));
			}
		}
	}
}