﻿using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.SystemExtensions;

namespace NewRelic.Providers.Wrapper.RabbitMq
{
    public class QueuePurgeWrapper : IWrapper
    {
        public bool IsTransactionRequired => true;

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            var method = methodInfo.Method;
            var canWrap = method.MatchesAny(assemblyName: "RabbitMQ.Client", typeName: "RabbitMQ.Client.Framing.Impl.Model", methodName: "_Private_QueuePurge");
            return new CanWrapResponse(canWrap);
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgentWrapperApi agentWrapperApi, ITransaction transaction)
        {
            // (IModel) uint QueuePurge(string queue)
            var queue = instrumentedMethodCall.MethodCall.MethodArguments.ExtractNotNullAs<string>(0);
            var destType = RabbitMqHelper.GetBrokerDestinationType(queue);
            var destName = RabbitMqHelper.ResolveDestinationName(destType, queue);

            var segment = transaction.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, destType, MessageBrokerAction.Purge, RabbitMqHelper.VendorName, destName);
            return Delegates.GetDelegateFor(segment);
        }
    }
}
