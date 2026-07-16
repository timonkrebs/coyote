// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskRunTests : BaseBugFindingTest
    {
        public TaskRunTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(() =>
                {
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(() =>
                {
                    entry.Value = 3;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 3;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 3;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 3;
                    });

                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                    });

                    entry.Value = 3;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 3;
                    });

                    entry.Value = 5;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 5;
                    });

                    entry.Value = 3;
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(() =>
                {
                    entry.Value = 3;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 3;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    await Task.Delay(100);
                    entry.Value = 3;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    });
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 3;
                        return entry.Value;
                    });
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelAsynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 5;
                        return entry.Value;
                    });
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        entry.Value = 3;
                        return entry.Value;
                    });
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
