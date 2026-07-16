// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskWhenEachTests : BaseBugFindingTest
    {
        public TaskWhenEachTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachYieldsAllTasks()
        {
            this.Test(async () =>
            {
                int effects = 0;
                Task[] tasks = new Task[]
                {
                    Task.Run(() => Interlocked.Increment(ref effects)),
                    Task.Run(() => Interlocked.Increment(ref effects)),
                    Task.Run(() => Interlocked.Increment(ref effects)),
                };

                var yielded = new HashSet<Task>();
                await foreach (Task task in Task.WhenEach(tasks))
                {
                    Specification.Assert(task.IsCompleted, "A yielded task is not completed.");
                    yielded.Add(task);
                }

                Specification.Assert(yielded.Count is 3, "Expected 3 yielded tasks, found {0}.", yielded.Count);
                Specification.Assert(effects is 3, "Expected every task body to have run.");

                // An empty input yields nothing.
                int count = 0;
                await foreach (Task task in Task.WhenEach(Array.Empty<Task>()))
                {
                    count++;
                }

                Specification.Assert(count is 0, "An empty input must yield no tasks.");
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithResults()
        {
            this.Test(async () =>
            {
                var tasks = new List<Task<int>>
                {
                    Task.Run(() => 1),
                    Task.Run(() => 2),
                    Task.Run(() => 3),
                };

                int sum = 0;
                await foreach (Task<int> task in Task.WhenEach(tasks))
                {
                    // The task is completed when yielded, so reading the result cannot block.
                    Specification.Assert(task.IsCompleted, "A yielded task is not completed.");
                    sum += task.Result;
                }

                Specification.Assert(sum is 6, "Expected the results to sum to 6, found {0}.", sum);
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachSpanOverload()
        {
            this.Test(async () =>
            {
                Task[] tasks = new Task[] { Task.Run(() => { }), Task.Run(() => { }) };
                int count = 0;
                await foreach (Task task in Task.WhenEach((ReadOnlySpan<Task>)tasks))
                {
                    Specification.Assert(task.IsCompleted, "A yielded task is not completed.");
                    count++;
                }

                Specification.Assert(count is 2, "Expected 2 yielded tasks, found {0}.", count);
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithInvalidInput()
        {
            this.Test(() =>
            {
                // Argument validation must happen eagerly at the call and match the invoked
                // API: ArgumentNullException for a null collection, ArgumentException for a
                // null entry -- not a deferred NullReferenceException at enumeration time.
                Exception nullCollection = Record.Exception(() => Task.WhenEach((Task[])null));
                Specification.Assert(
                    nullCollection is ArgumentNullException,
                    "Expected ArgumentNullException for a null collection, found {0}.",
                    nullCollection?.GetType().Name ?? "no exception");

                Exception nullEntry = Record.Exception(() => Task.WhenEach(Task.CompletedTask, null));
                Specification.Assert(
                    nullEntry is ArgumentException && nullEntry is not ArgumentNullException,
                    "Expected ArgumentException for a null entry, found {0}.",
                    nullEntry?.GetType().Name ?? "no exception");
            },
            this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithCancellation()
        {
            this.Test(async () =>
            {
                // A token canceled before iteration must cancel the stream on the first
                // move, even when a completed task would otherwise be ready to yield. The
                // enumerator is taken explicitly with the token (the WithCancellation sugar
                // lowers to ConfiguredCancelableAsyncEnumerable, which the rewriting engine
                // does not model yet -- tracked as follow-up work in the upgrade plan).
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                bool canceled = false;
                var enumerator = Task.WhenEach(Task.CompletedTask).GetAsyncEnumerator(cts.Token);
                try
                {
                    await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                Specification.Assert(canceled, "The canceled token did not cancel the stream.");
            },
            this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithCancellationDuringWait()
        {
            this.Test(async () =>
            {
                // A token canceled by ANOTHER operation while the stream is waiting on tasks
                // that never complete must wake the wait and cancel the stream, rather than
                // leaving the consumer paused forever (which would be reported as a deadlock).
                using var cts = new CancellationTokenSource();
                var tcs = new TaskCompletionSource<bool>();
                Task canceler = Task.Run(() => cts.Cancel());

                bool canceled = false;
                var enumerator = Task.WhenEach(tcs.Task).GetAsyncEnumerator(cts.Token);
                try
                {
                    await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                Specification.Assert(canceled, "The canceled token did not cancel the waiting stream.");
                await canceler;
            },
            this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachExploresCompletionOrders()
        {
            this.TestWithError(async () =>
            {
                Task taskA = Task.Run(() => { });
                Task taskB = Task.Run(() => { });

                Task first = null;
                await foreach (Task task in Task.WhenEach(taskA, taskB))
                {
                    first ??= task;
                }

                // The systematic exploration must be able to drive either completion order
                // through the enumerable, so some schedule yields taskB first.
                Specification.Assert(first != taskB, "Task B completed first.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Task B completed first.",
            replay: true);
        }
    }
}
#endif
