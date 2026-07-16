// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class MethodSignatureRewritingTests : BaseRewritingTest
    {
        public MethodSignatureRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static TaskAwaiter GetTaskAwaiter(TaskAwaiter taskAwaiter) => taskAwaiter;
        private static TaskAwaiter<T> GetGenericTaskAwaiter<T>(TaskAwaiter<T> taskAwaiter) => taskAwaiter;

        [Fact(Timeout = 5000)]
        public async Task TestRewritingTaskAwaiterInMethodSignature()
        {
            GetTaskAwaiter(default(TaskAwaiter));
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingGenericTaskAwaiterInMethodSignature()
        {
            GetGenericTaskAwaiter<int>(default(TaskAwaiter<int>));
        }
    }
}
