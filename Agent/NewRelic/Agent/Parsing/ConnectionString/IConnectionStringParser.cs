using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Core.NewRelic.Cache;

namespace NewRelic.Parsing.ConnectionString
{
	public interface IConnectionStringParser
	{
		ConnectionInfo GetConnectionInfo();
	}

	public static class ConnectionInfoParser
	{
		private const uint CacheCapacity = 1000;
		private static readonly SimpleCache<string, ConnectionInfo> _connectionInfoCache = new SimpleCache<string, ConnectionInfo>(CacheCapacity);

		private static readonly ConnectionInfo Empty = new ConnectionInfo(null, null, null);

		public static ConnectionInfo FromConnectionString(DatastoreVendor vendor, string connectionString)
		{
			return _connectionInfoCache.GetOrAdd(connectionString, () =>
			{
				IConnectionStringParser parser = GetConnectionParser(vendor, connectionString);
				return parser?.GetConnectionInfo() ?? Empty;
			});
		}

		private static IConnectionStringParser GetConnectionParser(DatastoreVendor vendor, string connectionString)
		{
			switch (vendor)
			{
				case DatastoreVendor.MSSQL:
					return new MsSqlConnectionStringParser(connectionString);
				case DatastoreVendor.MySQL:
					return new MySqlConnectionStringParser(connectionString);
				case DatastoreVendor.Postgres:
					return new PostgresConnectionStringParser(connectionString);
				case DatastoreVendor.Oracle:
					return new OracleConnectionStringParser(connectionString);
				case DatastoreVendor.IBMDB2:
					return new IbmDb2ConnectionStringParser(connectionString);
				case DatastoreVendor.Redis:
					return new StackExchangeRedisConnectionStringParser(connectionString);
				default:
					return null;
			}
		}
	}
}