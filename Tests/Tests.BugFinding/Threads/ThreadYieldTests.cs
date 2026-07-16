// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ThreadYieldTests : BaseBugFindingTest
    {
        public ThreadYieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestThreadYield()
        {
            this.Test(() =>
            {
                Thread.Yield();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public async Task TestCooperativeThreadYield()
        {
            this.Test(() =>
            {
                bool isDone = false;
                Thread t1 = new Thread(() =>
                {
                    isDone = true;
                });

                Thread t2 = new Thread(() =>
                {
                    while (!isDone)
                    {
                        Thread.Yield();
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();

                Specification.Assert(isDone, "The expected condition was not satisfied.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
    }
}
