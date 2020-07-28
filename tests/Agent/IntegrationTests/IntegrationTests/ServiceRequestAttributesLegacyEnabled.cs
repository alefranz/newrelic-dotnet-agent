﻿using System.Collections.Generic;
using System.Linq;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTestHelpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.IntegrationTests
{
    public class ServiceRequestAttributesLegacyEnabled : IClassFixture<RemoteServiceFixtures.WcfAppSelfHosted>
    {
        private readonly RemoteServiceFixtures.WcfAppSelfHosted _fixture;

        public ServiceRequestAttributesLegacyEnabled(RemoteServiceFixtures.WcfAppSelfHosted fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _fixture.TestLogger = output;
            _fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configPath = fixture.DestinationNewRelicConfigFilePath;
                    var configModifier = new NewRelicConfigModifier(configPath);
                    configModifier.ForceTransactionTraces();

                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(configPath, new[] { "configuration", "parameterGroups", "serviceRequestParameters" }, "enabled", "true");
                },
                exerciseApplication: () => _fixture.ReturnString()
            );
            _fixture.Initialize();
        }

        [Fact]
        public void Test()
        {
            var expectedTransactionTraceAttributes = new Dictionary<string, string>
            {
                { "service.request.input", "foo" },
            };

            var transactionSample = _fixture.AgentLog.GetTransactionSamples().FirstOrDefault();
            Assert.NotNull(transactionSample);

            Assertions.TransactionTraceHasAttributes(expectedTransactionTraceAttributes, TransactionTraceAttributeType.Agent, transactionSample);
        }
    }
}
