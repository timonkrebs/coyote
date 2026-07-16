// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Rewriting;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledDeadlockTests : BaseBugFindingTest
    {
        public UncontrolledDeadlockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [SkipRewriting("Must not be rewritten.")]
        private class ManualResetEventStub : IDisposable
        {
            private readonly ManualResetEvent Handle;

            internal ManualResetEventStub(bool initialState)
            {
                this.Handle = new ManualResetEvent(initialState);
            }

            internal void Set() => this.Handle.Set();
            internal void Reset() => this.Handle.Reset();
            internal bool WaitOne() => this.Handle.WaitOne();

            public void Dispose() => this.Handle.Dispose();
        }

        [Fact(Timeout = 300000)]
        public async Task TestUncontrolledDeadlock()
        {
            this.TestWithError(async () =>
            {
                var handle = new ManualResetEventStub(true);
                Task task = Task.Run(async () =>
                {
                    handle.WaitOne();
                    await Task.Delay(1);
                    handle.Set();
                    handle.Reset();
                });

                handle.WaitOne();
                await Task.Delay(1);
                handle.Set();
                handle.Reset();
                await task;
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithDeadlockTimeout(10)
                .WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Potential deadlock or hang detected. The periodic deadlock detection monitor", e);
            });
        }

        [Fact(Timeout = 300000)]
        public async Task TestUncontrolledDeadlockReportedAsNoBug()
        {
            this.Test(async () =>
            {
                var handle = new ManualResetEventStub(true);
                Task task = Task.Run(async () =>
                {
                    handle.WaitOne();
                    await Task.Delay(1);
                    handle.Set();
                    handle.Reset();
                });

                handle.WaitOne();
                await Task.Delay(1);
                handle.Set();
                handle.Reset();
                await task;
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithPotentialDeadlocksReportedAsBugs(false)
                .WithDeadlockTimeout(10)
                .WithTestingIterations(10));
        }
    }
}
