// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskRewritingTests : BaseRewritingTest
    {
        public TaskRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingTaskWhenAll()
        {
            await Task.WhenAll(Task.CompletedTask);
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingGenericTaskWhenAll()
        {
            await Task.WhenAll(Task.FromResult(1));
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingTaskWhenAny()
        {
            await Task.WhenAny(Task.CompletedTask);
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingGenericTaskWhenAny()
        {
            await Task.WhenAny(Task.FromResult(1));
        }
    }
}
