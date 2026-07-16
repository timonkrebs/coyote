// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using SystemLock = System.Threading.Lock;
using SystemMonitor = System.Threading.Monitor;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class LockTests : BaseBugFindingTest
    {
        public LockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestLockMutualExclusion()
        {
            this.Test(async () =>
            {
                var l = new SystemLock();
                int x = 0;
                int y = 0;
                Task t1 = Task.Run(() =>
                {
                    l.Enter();
                    try
                    {
                        x = 1;
                        y = 1;
                    }
                    finally
                    {
                        l.Exit();
                    }
                });

                Task t2 = Task.Run(() =>
                {
                    l.Enter();
                    try
                    {
                        Specification.Assert(x == y, "Detected torn pair.");
                    }
                    finally
                    {
                        l.Exit();
                    }
                });

                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLockScopeMutualExclusion()
        {
            this.Test(async () =>
            {
                var l = new SystemLock();
                int x = 0;
                int y = 0;

                // This mirrors the code that the C# 13+ compiler emits for a 'lock' statement
                // over a 'System.Threading.Lock' field: EnterScope paired with Scope.Dispose.
                Task t1 = Task.Run(() =>
                {
                    var scope = l.EnterScope();
                    try
                    {
                        x = 1;
                        y = 1;
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                });

                Task t2 = Task.Run(() =>
                {
                    var scope = l.EnterScope();
                    try
                    {
                        Specification.Assert(x == y, "Detected torn pair.");
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                });

                await Task.WhenAll(t1, t2);
            },
            this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLockReentrancy()
        {
            this.Test(() =>
            {
                var l = new SystemLock();
                Specification.Assert(!l.IsHeldByCurrentThread, "Lock is unexpectedly held.");

                l.Enter();
                Specification.Assert(l.IsHeldByCurrentThread, "Lock is not held after Enter.");

                var scope = l.EnterScope();
                Specification.Assert(l.IsHeldByCurrentThread, "Lock is not held after EnterScope.");

                Specification.Assert(l.TryEnter(), "TryEnter failed on a reentrant acquire.");
                l.Exit();

                scope.Dispose();
                Specification.Assert(l.IsHeldByCurrentThread, "Lock is not held after disposing the inner scope.");

                l.Exit();
                Specification.Assert(!l.IsHeldByCurrentThread, "Lock is still held after the final Exit.");
            },
            this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestLockAndMonitorAreIndependent()
        {
            this.TestWithError(async () =>
            {
                var l = new SystemLock();

                // At runtime 'System.Threading.Lock' is a synchronization mechanism that is
                // independent from the monitor table, so holding the lock does not exclude a
                // thread that monitor-locks the same instance upcast to object. The modeled
                // lock must preserve that independence (it keys on a surrogate object instead
                // of the instance the Monitor model keys on), or these two critical sections
                // would falsely serialize and the torn pair below would become unobservable.
#pragma warning disable CS9216 // The conversion away from Lock is exactly what this test pins.
                object monitorView = l;
#pragma warning restore CS9216
                int x = 0;
                int y = 0;
                Task t1 = Task.Run(() =>
                {
                    l.Enter();
                    try
                    {
                        x = 1;
                        SchedulingPoint.Interleave();
                        y = 1;
                    }
                    finally
                    {
                        l.Exit();
                    }
                });

                Task t2 = Task.Run(() =>
                {
                    SystemMonitor.Enter(monitorView);
                    try
                    {
                        Specification.Assert(x == y, "Detected torn pair.");
                    }
                    finally
                    {
                        SystemMonitor.Exit(monitorView);
                    }
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Detected torn pair.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestLockRaceDetection()
        {
            this.TestWithError(async () =>
            {
                var l = new SystemLock();
                int x = 0;
                int y = 0;
                Task t1 = Task.Run(async () =>
                {
                    // BUG: the pair is written without acquiring the lock, so a
                    // concurrent locked reader can observe the torn intermediate
                    // state; the yield is a controlled scheduling point that lets
                    // the exploration surface it.
                    x = 1;
                    await Task.Yield();
                    y = 1;
                });

                Task t2 = Task.Run(() =>
                {
                    l.Enter();
                    try
                    {
                        Specification.Assert(x == y, "Detected torn pair.");
                    }
                    finally
                    {
                        l.Exit();
                    }
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Detected torn pair.",
            replay: true);
        }
    }
}
#endif
