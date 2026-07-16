// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskCancellationTests : BaseBugFindingTest
    {
        public TaskCancellationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestAlreadyCanceledParallelTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(() => { }, ct);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestAlreadyCanceledAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                }, ct);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestAlreadyCanceledParallelTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(() => 3, ct);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestAlreadyCanceledAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    return 3;
                }, ct);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestCancelParallelTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(() => { }, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestCancelAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                }, cts.Token);

                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestCancelParallelTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(() => 3, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestCancelAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    return 3;
                }, cts.Token);

                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(1);
                    }, cts.Token);
                });

                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 300000)]
        public async Task TestCancelNestedAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.Delay(1);
                        return 3;
                    }, cts.Token);
                });

                cts.Cancel();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
