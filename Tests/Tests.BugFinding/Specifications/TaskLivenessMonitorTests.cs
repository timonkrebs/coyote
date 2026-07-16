// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests.Specifications
{
    public class TaskLivenessMonitorTests : BaseBugFindingTest
    {
        public TaskLivenessMonitorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Monitor.Event
        {
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Notify), typeof(Done))]
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(() =>
                {
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    Specification.Monitor<LivenessMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        Specification.Monitor<LivenessMonitor>(new Notify());
                    });
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                }

                await WriteAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                }

                await WriteWithDelayAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(() =>
                {
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestLivenessMonitorInvocationInNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<LivenessMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                    });
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "LivenessMonitor detected liveness bug in hot state 'Init' at the end of program execution.",
            replay: true);
        }
    }
}
