// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledValueTaskTests : BaseBugFindingTest
    {
        public UncontrolledValueTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

#if NET
        [Fact(Timeout = 5000)]
        public async Task TestDetectedUncontrolledValueTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledValueTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public async Task TestDetectedUncontrolledGenericValueTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledGenericValueTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public async Task TestDetectedUncontrolledValueTaskAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new UncontrolledValueTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
#endif
    }
}
