﻿using NewRelic.Agent.Configuration;
using NewRelic.Agent.Core.Aggregators;
using NewRelic.Agent.Core.Database;
using NewRelic.Agent.Core.Metric;
using NewRelic.Agent.Core.Time;
using NewRelic.Agent.Core.Transactions;
using NewRelic.Agent.Core.WireModels;
using NewRelic.Agent.Extensions.Parsing;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.Core.Logging;
using System;
using System.Collections.Generic;
using static NewRelic.Agent.Core.WireModels.MetricWireModel;

namespace NewRelic.Agent.Core.Wrapper.AgentWrapperApi.Builders
{
	public class DatastoreSegmentData : AbstractSegmentData
	{
		private readonly static ConnectionInfo EmptyConnectionInfo = new ConnectionInfo(null, null, null);

		public string Operation => _parsedSqlStatement.Operation;
		public DatastoreVendor DatastoreVendorName => _parsedSqlStatement.DatastoreVendor;
		public string Model => _parsedSqlStatement.Model;
		public string CommandText { get; set; }
		public string Host => _connectionInfo.Host;
		public string PortPathOrId => _connectionInfo.PortPathOrId;
		public string DatabaseName => _connectionInfo.DatabaseName;
		public Func<object> GetExplainPlanResources { get; set; }
		public Func<object, ExplainPlan> GenerateExplainPlan { get; set; }
		public Func<bool> DoExplainPlanCondition { get; set; }

		public IDictionary<string, IConvertible> QueryParameters { get; set; }

		private object _explainPlanResources;
		private ExplainPlan _explainPlan;

		private ConnectionInfo _connectionInfo;
		private ParsedSqlStatement _parsedSqlStatement;

		public ExplainPlan ExplainPlan => _explainPlan;

		public DatastoreSegmentData(ParsedSqlStatement parsedSqlStatement, string commandText = null, ConnectionInfo connectionInfo = null, IDictionary<string, IConvertible> queryParameters = null)
		{
			this._connectionInfo = connectionInfo ?? EmptyConnectionInfo;
			this._parsedSqlStatement = parsedSqlStatement;
			CommandText = commandText;
			QueryParameters = queryParameters;
		}

		internal override void AddTransactionTraceParameters(IConfigurationService configurationService, Segment segment, IDictionary<string, object> segmentParameters, ImmutableTransaction immutableTransaction)
		{
			if (ExplainPlan != null)
			{
				segmentParameters["explain_plan"] = new ExplainPlanWireModel(ExplainPlan);
			}

			if (CommandText != null)
			{
				segmentParameters["sql"] = immutableTransaction.GetSqlObfuscatedAccordingToConfig(CommandText, DatastoreVendorName);
			}

			if (configurationService.Configuration.InstanceReportingEnabled)
			{
				segmentParameters["host"] = Host;
				segmentParameters["port_path_or_id"] = PortPathOrId;
			}

			if (configurationService.Configuration.DatabaseNameReportingEnabled)
			{
				segmentParameters["database_name"] = DatabaseName;
			}

			if (QueryParameters != null)
			{
				segmentParameters["query_parameters"] = QueryParameters;
			}
		}

		internal override IEnumerable<KeyValuePair<string, object>> Finish()
		{
			if (GetExplainPlanResources == null)
				return null;

			// Ensures we aren't running explain plan twice
			if (_explainPlanResources != null)
				return null;

			try
			{
				// Using invoke for thread safety, DoExplainPlanCondition is nullable
				if (DoExplainPlanCondition?.Invoke() == true)
				{
					_explainPlanResources = GetExplainPlanResources();
				}
				else
				{
					GetExplainPlanResources = null;
					GenerateExplainPlan = null;
				}
			}
			catch (Exception exception)
			{
				Log.DebugFormat("Unable to retrieve resources for explain plan: {0}", exception);
			}
			return null;
		}


		public void ExecuteExplainPlan(SqlObfuscator obfuscator)
		{
			// Don't re-run an explain plan if one already exists
			if (_explainPlan != null)
				return;
			
			try
			{
				// Using invoke for thread safety, DoExplainPlanCondition is nullable
				if (DoExplainPlanCondition?.Invoke() == true)
				{
					var explainPlan = GenerateExplainPlan?.Invoke(_explainPlanResources);
					if (explainPlan != null)
					{
						foreach (var data in explainPlan.ExplainPlanDatas)
						{
							foreach (var index in explainPlan.ObfuscatedHeaders)
							{
								data[index] = obfuscator.GetObfuscatedSql(data[index].ToString(), DatastoreVendorName);
							}
						}

						_explainPlan = new ExplainPlan(explainPlan.ExplainPlanHeaders, explainPlan.ExplainPlanDatas, explainPlan.ObfuscatedHeaders);
					}
				}
			}
			catch (Exception exception)
			{
				Log.DebugFormat("Unable to execute explain plan: {0}", exception);
			}
		}

		public override bool IsCombinableWith(AbstractSegmentData otherSegment)
		{

			var otherTypedSegment = otherSegment as DatastoreSegmentData;
			if (otherTypedSegment == null)
				return false;

			if (Operation != otherTypedSegment.Operation)
				return false;

			if (DatastoreVendorName != otherTypedSegment.DatastoreVendorName)
				return false;

			if (Model != otherTypedSegment.Model)
				return false;

			return true;
		}

		public override string GetTransactionTraceName()
		{
			var name = (Model == null) ? MetricNames.GetDatastoreOperation(DatastoreVendorName, Operation) : MetricNames.GetDatastoreStatement(DatastoreVendorName, Model, Operation);
			return name.ToString();
		}

		public override void AddMetricStats(Segment segment, TimeSpan durationOfChildren, TransactionMetricStatsCollection txStats, IConfigurationService configService)
		{
			var duration = segment.Duration.Value;
			var exclusiveDuration = TimeSpanMath.Max(TimeSpan.Zero, duration - durationOfChildren);

			if (!string.IsNullOrEmpty(Model))
			{
				MetricBuilder.TryBuildDatastoreStatementMetric(DatastoreVendorName, _parsedSqlStatement, duration, exclusiveDuration, txStats);
				MetricBuilder.TryBuildDatastoreVendorOperationMetric(DatastoreVendorName, Operation, duration, exclusiveDuration, txStats, true);
			}
			else
			{
				MetricBuilder.TryBuildDatastoreVendorOperationMetric(DatastoreVendorName, Operation, duration, exclusiveDuration, txStats, false);
			}

			MetricBuilder.TryBuildDatastoreRollupMetrics(DatastoreVendorName, duration, exclusiveDuration, txStats);

			if (configService.Configuration.InstanceReportingEnabled)
			{
				MetricBuilder.TryBuildDatastoreInstanceMetric(DatastoreVendorName, Host,
				PortPathOrId, duration, duration, txStats);
			}
		}

		public override Segment CreateSimilar(Segment segment, TimeSpan newRelativeStartTime, TimeSpan newDuration, IEnumerable<KeyValuePair<string, object>> newParameters)
		{
			return new TypedSegment<DatastoreSegmentData>(newRelativeStartTime, newDuration, segment, newParameters);
		}
	}
}
