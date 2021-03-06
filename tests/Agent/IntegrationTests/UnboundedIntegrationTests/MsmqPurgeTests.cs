// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections.Generic;
using System.Linq;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Testing.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.UnboundedIntegrationTests
{
    [NetFrameworkTest]
    public class MsmqPurgeTests : IClassFixture<RemoteServiceFixtures.MSMQBasicMVCApplicationFixture>
    {
        private readonly RemoteServiceFixtures.MSMQBasicMVCApplicationFixture _fixture;

        public MsmqPurgeTests(RemoteServiceFixtures.MSMQBasicMVCApplicationFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.TestLogger = output;
            _fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configModifier = new NewRelicConfigModifier(fixture.DestinationNewRelicConfigFilePath);
                    configModifier.ForceTransactionTraces();
                },
                exerciseApplication: () =>
                {
                    _fixture.GetMessageQueue_Msmq_Send(true);
                    _fixture.GetMessageQueue_Msmq_Purge();
                }
            );
            _fixture.Initialize();
        }

        [Fact]
        public void Test()
        {
            var expectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric { metricName = @"MessageBroker/Msmq/Queue/Purge/Named/private$\nrtestqueue", callCount = 1},
                new Assertions.ExpectedMetric { metricName = @"MessageBroker/Msmq/Queue/Purge/Named/private$\nrtestqueue", callCount = 1, metricScope = "WebTransaction/MVC/MSMQController/Msmq_Purge"},
            };
            var expectedTransactionTraceSegments = new List<string>
            {
                @"MessageBroker/Msmq/Queue/Purge/Named/private$\nrtestqueue"
            };

            var metrics = _fixture.AgentLog.GetMetrics().ToList();
            var transactionSample = _fixture.AgentLog.TryGetTransactionSample("WebTransaction/MVC/MSMQController/Msmq_Purge");
            var transactionEvent = _fixture.AgentLog.TryGetTransactionEvent("WebTransaction/MVC/MSMQController/Msmq_Purge");

            NrAssert.Multiple(
                () => Assert.NotNull(transactionSample),
                () => Assert.NotNull(transactionEvent)
                );

            NrAssert.Multiple
            (
                () => Assertions.MetricsExist(expectedMetrics, metrics),
                () => Assertions.TransactionTraceSegmentsExist(expectedTransactionTraceSegments, transactionSample)
            );
        }

    }
}
