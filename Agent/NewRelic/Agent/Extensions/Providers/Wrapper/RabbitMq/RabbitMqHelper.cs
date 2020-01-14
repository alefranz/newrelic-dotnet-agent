﻿using System.Collections.Generic;
using System.Text;
using NewRelic.Agent.Api;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.SystemExtensions;

namespace NewRelic.Providers.Wrapper.RabbitMq
{
	public class RabbitMqHelper
	{
		private const string TempQueuePrefix = "amq.";
		private const string BasicPropertiesType = "RabbitMQ.Client.Framing.BasicProperties";
		
		public const string VendorName = "RabbitMQ";
		public const string AssemblyName = "RabbitMQ.Client";
		public const string TypeName = "RabbitMQ.Client.Framing.Impl.Model";

		public static MessageBrokerDestinationType GetBrokerDestinationType(string queueNameOrRoutingKey)
		{
			if (queueNameOrRoutingKey.StartsWith(TempQueuePrefix))
				return MessageBrokerDestinationType.TempQueue;

			return queueNameOrRoutingKey.Contains(".") ? MessageBrokerDestinationType.Topic : MessageBrokerDestinationType.Queue;
		}

		public static string ResolveDestinationName(MessageBrokerDestinationType destinationType,
			string queueNameOrRoutingKey)
		{
			return (destinationType == MessageBrokerDestinationType.TempQueue ||
			        destinationType == MessageBrokerDestinationType.TempTopic)
				? null
				: queueNameOrRoutingKey;
		}

		public static bool TryGetPayloadFromHeaders(Dictionary<string, object> messageHeaders, IAgent agent,
			out string serializedPayload)
		{
			if (agent.TryGetDistributedTracePayloadFromHeaders(messageHeaders, out var payload))
			{
				serializedPayload = Encoding.UTF8.GetString((byte[])(payload));
				return true;
			}
			 
			serializedPayload = null;
			return false;
		}

		public static ISegment CreateSegmentForPublishWrappers(InstrumentedMethodCall instrumentedMethodCall, ITransaction transaction, int basicPropertiesIndex)
		{
			// ATTENTION: We have validated that the use of dynamic here is appropriate based on the visibility of the data we're working with.
			// If we implement newer versions of the API or new methods we'll need to re-evaluate.
			// never null. Headers property can be null.
			var basicProperties = instrumentedMethodCall.MethodCall.MethodArguments.ExtractAs<dynamic>(basicPropertiesIndex);

			var routingKey = instrumentedMethodCall.MethodCall.MethodArguments.ExtractNotNullAs<string>(1);
			var destType = GetBrokerDestinationType(routingKey);
			var destName = ResolveDestinationName(destType, routingKey);

			var segment = transaction.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, destType, MessageBrokerAction.Produce, VendorName, destName);

			//If the RabbitMQ version doesn't provide the BasicProperties parameter we just bail.
			if (basicProperties.GetType().ToString() != BasicPropertiesType)
			{
				return segment;
			}

			// We're relying on CreateDistibutedTracePayload to do all the necessary checking realated to whether we return an empty payload or not.
			var distributedTracePayload = transaction.CreateDistributedTracePayload();

			if (!distributedTracePayload.IsEmpty())
			{
				// if null, setup a new dictionary  and replace the null Headers property with it.
				var headers = basicProperties.Headers as Dictionary<string, object>;

				if (headers == null)
				{
					headers = new Dictionary<string, object>();
					basicProperties.Headers = headers;
				}

				headers[Constants.DistributedTracePayloadKey] = distributedTracePayload.HttpSafe();
			}

			return segment;
		}
	}
}
