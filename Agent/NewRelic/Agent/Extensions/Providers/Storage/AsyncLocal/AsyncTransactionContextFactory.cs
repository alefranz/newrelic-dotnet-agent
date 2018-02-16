﻿using NewRelic.Agent.Extensions.Providers;
using System;

namespace NewRelic.Providers.Storage.AsyncLocal
{
	public class AsyncTransactionContextFactory : IContextStorageFactory
	{
		public bool IsAsyncStorage => true;

		public String Name => GetType().FullName;

		public bool IsValid => true;

		public ContextStorageType Type => ContextStorageType.AsyncLocal;

		public IContextStorage<T> CreateContext<T>(string key)
		{
			return new AsyncTransactionContext<T>();
		}
	}
}