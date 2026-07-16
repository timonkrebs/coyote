// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class AsyncMethodRewritingTests : BaseRewritingTest
    {
        public AsyncMethodRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingAsyncMethod()
        {
            await Task.CompletedTask;
        }

        // This test intentionally returns 'Task<int>' to exercise rewriting of
        // generic async test methods; the xunit v2 runner executes such tests.
#pragma warning disable xUnit1028 // Test methods must have a supported return type
        [Fact(Timeout = 300000)]
        public async Task<int> TestRewritingGenericAsyncMethod()
        {
            return await Task.FromResult(1);
        }
#pragma warning restore xUnit1028
    }
}
