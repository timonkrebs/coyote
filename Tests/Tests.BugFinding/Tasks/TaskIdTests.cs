// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskIdTests : BaseBugFindingTest
    {
        public TaskIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestExpectedIdInTaskWithAction()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(() =>
                {
                    entry.Value = Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(entry.Value == task.Id, "Unexpected task id.");
                Specification.Assert(entry.Value != task.Id, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestExpectedIdInTaskWithFunction()
        {
            this.TestWithError(async () =>
            {
                var task = Task.Run(() =>
                {
                    return Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(task.Result == task.Id, "Unexpected task id.");
                Specification.Assert(task.Result != task.Id, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestExpectedIdInTaskWithAsynchronousFunction()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                });

                await task;

                Specification.Assert(entry.Value != task.Id, "Unexpected task id.");
                Specification.Assert(entry.Value == task.Id, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestExpectedIdInTaskWithGenericAsynchronousFunction()
        {
            this.TestWithError(async () =>
            {
                var task = Task.Run(async () =>
                {
                    int id = Task.CurrentId.Value;
                    await Task.Delay(1);
                    return id;
                });

                await task;

                Specification.Assert(task.Result != task.Id, "Unexpected task id.");
                Specification.Assert(task.Result == task.Id, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
