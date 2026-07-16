// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests.Specifications
{
    public class TaskSafetyMonitorTests : BaseBugFindingTest
    {
        public TaskSafetyMonitorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Notify : Monitor.Event
        {
        }

        private class SafetyMonitor : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(Notify), nameof(HandleNotify))]
            private class Init : State
            {
            }

            private void HandleNotify()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                }

                await WriteWithDelayAsync();
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInParallelTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(() =>
                {
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    Specification.Monitor<SafetyMonitor>(new Notify());
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestSafetyMonitorInvocationInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Specification.RegisterMonitor<SafetyMonitor>();
                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        Specification.Monitor<SafetyMonitor>(new Notify());
                    });
                });
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
