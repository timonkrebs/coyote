// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Specifications
{
    public class OverloadedEventHandlerTests : BaseActorBugFindingTest
    {
        public OverloadedEventHandlerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(MonitorUnitEvent), nameof(HandleUnitEvent))]
            public class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(MonitorUnitEvent.Instance);

#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }

#pragma warning disable CA1801 // Parameter not used
#pragma warning disable IDE0060 // Parameter not used
#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent(int k)
#pragma warning restore CA1822 // Mark members as static
            {
            }
#pragma warning restore IDE0060 // Parameter not used
#pragma warning restore CA1801 // Parameter not used
        }

        [Fact(Timeout = 300000)]
        public async Task TestOverloadedMonitorEventHandler()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Safety>();
            });
        }
    }
}
