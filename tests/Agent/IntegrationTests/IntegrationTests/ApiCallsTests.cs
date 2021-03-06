// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTests.RemoteServiceFixtures;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.IntegrationTests
{
    [NetFrameworkTest]
    public class ApiCallsTestsFW : ApiCallsTests<ConsoleDynamicMethodFixtureFW>
    {
        public ApiCallsTestsFW(ConsoleDynamicMethodFixtureFW fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    [NetCoreTest]
    public class ApiCallsTestsCore : ApiCallsTests<ConsoleDynamicMethodFixtureCoreLatest>
    {
        public ApiCallsTestsCore(ConsoleDynamicMethodFixtureCoreLatest fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }
    }

    public abstract class ApiCallsTests<TFixture> : IClassFixture<TFixture> where TFixture : ConsoleDynamicMethodFixture
    {
        private readonly string[] ApiCalls = new string[]
            {
                "TestTraceMetadata",
                "TestGetLinkingMetadata"
            };

        protected readonly TFixture Fixture;

        public ApiCallsTests(TFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Fixture.TestLogger = output;

            foreach (var apiCall in ApiCalls)
            {
                Fixture.AddCommand($"ApiCalls {apiCall}");
            }

            Fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configModifier = new NewRelicConfigModifier(Fixture.DestinationNewRelicConfigFilePath);
                    configModifier.SetOrDeleteDistributedTraceEnabled(true);
                    configModifier.SetLogLevel("finest");
                }
            );

            Fixture.Initialize();
        }

        [Fact]
        public void ExpectedMetrics()
        {
            var expectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric(){ callCount = 1, metricName = "Supportability/ApiInvocation/TraceMetadata" },
                new Assertions.ExpectedMetric(){ callCount = 1, metricName = "Supportability/ApiInvocation/GetLinkingMetadata"}
            };

            var actualMetrics = Fixture.AgentLog.GetMetrics().ToList();

            Assertions.MetricsExist(expectedMetrics, actualMetrics);
        }
    }
}
