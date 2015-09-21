﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cqrs.Domain.Exception;
using Cqrs.Events;
using Cqrs.Infrastructure;

namespace Cqrs.Domain
{
	public abstract class AggregateRoot<TAuthenticationToken> : IAggregateRoot<TAuthenticationToken>
	{
		private ReaderWriterLockSlim Lock { get; set; }

		private IList<IEvent<TAuthenticationToken>> Changes { get; set; }

		public Guid Id { get; protected set; }

		public int Version { get; protected set; }

		protected AggregateRoot()
		{
			Lock = new ReaderWriterLockSlim();
			Changes = new List<IEvent<TAuthenticationToken>>();
		}

		public IEnumerable<IEvent<TAuthenticationToken>> GetUncommittedChanges()
		{
			Lock.EnterReadLock();
			try
			{
				return Changes;
			}
			finally
			{
				Lock.ExitReadLock();
			}
		}

		public void MarkChangesAsCommitted()
		{
			Lock.EnterWriteLock();
			try
			{
				Version = Version + Changes.Count;
				Changes.Clear();
			}
			finally
			{
				Lock.ExitWriteLock();
			}
		}

		public void LoadFromHistory(IEnumerable<IEvent<TAuthenticationToken>> history)
		{
			foreach (IEvent<TAuthenticationToken> @event in history.OrderBy(e =>e.Version))
			{
				if (@event.Version != Version + 1)
					throw new EventsOutOfOrderException(@event.Id, GetType(), Version + 1, @event.Version);
				ApplyChange(@event, false);
			}
		}

		protected void ApplyChange(IEvent<TAuthenticationToken> @event)
		{
			ApplyChange(@event, true);
		}

		private void ApplyChange(IEvent<TAuthenticationToken> @event, bool isNew)
		{
			Lock.EnterWriteLock();
			try
			{
				this.AsDynamic().Apply(@event);
				if (isNew)
				{
					Changes.Add(@event);
				}
				else
				{
					Id = @event.Id;
					Version++;
				}
			}
			finally
			{
				Lock.ExitWriteLock();
			}
		}
	}
}