﻿using System;
using System.Diagnostics;
using NewRelic.Agent.Core.AgentHealth;
using NewRelic.Agent.Core.Logging;
using NewRelic.Agent.Core.Time;
using NewRelic.Agent.Core.Transformers;

namespace NewRelic.Agent.Core.Samplers
{
    public class MemorySampler : AbstractSampler
    {
        private readonly IAgentHealthReporter _agentHealthReporter;
        private readonly IMemorySampleTransformer _memorySampleTransformer;

        public MemorySampler(IScheduler scheduler, IMemorySampleTransformer memorySampleTransformer, IAgentHealthReporter agentHealthReporter)
            : base(scheduler, TimeSpan.FromSeconds(10))
        {
            _agentHealthReporter = agentHealthReporter;
            _memorySampleTransformer = memorySampleTransformer;
        }

        public override void Sample()
        {
            try
            {
                var immutableMemorySample = new ImmutableMemorySample(GetCurrentlyAllocatedMemoryBytes());
                _memorySampleTransformer.Transform(immutableMemorySample);
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to get Memory sample.  No Memory metrics will be reported.  Error : {ex}");
                Stop();
            }
        }

        private static float GetCurrentlyAllocatedMemoryBytes()
        {
            return Process.GetCurrentProcess().PrivateMemorySize64;
        }
    }

    public class ImmutableMemorySample
    {
        public readonly float MemoryPhysical;

        public ImmutableMemorySample(float memoryPhysical)
        {
            MemoryPhysical = memoryPhysical;
        }
    }
}
