// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using Microsoft.Coyote.Runtime;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.SystematicFuzzing
{
    public class TaskWhenEachTests : Tests.TaskWhenEachTests
    {
        public TaskWhenEachTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Fuzzing;

        protected override Configuration GetConfiguration()
        {
            return base.GetConfiguration().WithSystematicFuzzingEnabled();
        }
    }
}
#endif
