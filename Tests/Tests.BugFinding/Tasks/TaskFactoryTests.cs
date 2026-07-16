// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;

namespace Microsoft.Coyote.BugFinding.Tests
{
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
    public class TaskFactoryTests : BaseBugFindingTest
    {
        public TaskFactoryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskWithSynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                }).Unwrap();

                AssertSharedEntryValue(entry, 5);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskWithAsynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                }).Unwrap();

                AssertSharedEntryValue(entry, 5);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewNestedTaskWithSynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 3;
                    }).Unwrap();

                    entry.Value = 5;
                }).Unwrap();

                AssertSharedEntryValue(entry, 5);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewNestedTaskWithAsynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 3;
                    }).Unwrap();

                    entry.Value = 5;
                }).Unwrap();

                AssertSharedEntryValue(entry, 5);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewNestedTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewNestedTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestGenericStartNewTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<int>.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestGenericStartNewTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestGenericStartNewTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestGenericStartNewNestedTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    return await Task<Task<int>>.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestGenericStartNewNestedTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    return await Task<Task<int>>.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewCanceledTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Factory.StartNew(() => { }, ct);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskCancelation()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Factory.StartNew(() => { }, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewCanceledTaskWithAsynchronousAwait()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Factory.StartNew(async () => await Task.Delay(1), ct).Unwrap();
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestStartNewTaskCancelationWithAsynchronousAwait()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Factory.StartNew(async () => await Task.Delay(1), cts.Token).Unwrap();
                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }
    }
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
}
