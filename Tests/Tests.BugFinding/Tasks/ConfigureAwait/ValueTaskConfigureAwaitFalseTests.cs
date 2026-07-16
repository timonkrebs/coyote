// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ValueTaskConfigureAwaitFalseTests : BaseBugFindingTest
    {
        public ValueTaskConfigureAwaitFalseTests(ITestOutputHelper output)
            : base(output)
        {
        }

#if NET
        private static async ValueTask WriteAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            entry.Value = value;
        }
#endif

        private static async ValueTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1).ConfigureAwait(false);
            entry.Value = value;
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestAwaitSynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 5).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitSynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 3).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestAwaitAsynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 5).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitAsynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 3).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        private static async ValueTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            await WriteAsync(entry, value).ConfigureAwait(false);
        }
#endif

        private static async ValueTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1).ConfigureAwait(false);
            await WriteWithDelayAsync(entry, value).ConfigureAwait(false);
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedSynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 5).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedSynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 3).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedAsynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 5).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedAsynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 3).ConfigureAwait(false);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestAwaitSynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultAsync(5).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitSynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultAsync(3).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestAwaitAsynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultWithDelayAsync(5).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitAsynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultWithDelayAsync(3).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        private static async ValueTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            return await entry.GetWriteResultAsync(value).ConfigureAwait(false);
        }
#endif

        private static async ValueTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return await entry.GetWriteResultWithDelayAsync(value).ConfigureAwait(false);
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedSynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 5).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedSynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 3).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedAsynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestAwaitNestedAsynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 3).ConfigureAwait(false);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
