// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class ArrayTypeRewritingTests : BaseRewritingTest
    {
        public ArrayTypeRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyObjectArray()
        {
            _ = System.Array.Empty<object>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyNestedObjectArray()
        {
            _ = System.Array.Empty<object[]>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyTask()
        {
            _ = System.Array.Empty<Task>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTask()
        {
            _ = System.Array.Empty<Task<int>>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTaskWithTaskAwaiter()
        {
            _ = System.Array.Empty<Task<TaskAwaiter>>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTaskWithGenericTaskAwaiter()
        {
            _ = System.Array.Empty<Task<TaskAwaiter<int>>>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyTaskAwaiter()
        {
            _ = System.Array.Empty<TaskAwaiter>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTaskAwaiter()
        {
            _ = System.Array.Empty<TaskAwaiter<int>>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTaskAwaiterWithTask()
        {
            _ = System.Array.Empty<TaskAwaiter<Task>>();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingEmptyGenericTaskAwaiterWithGenericTask()
        {
            _ = System.Array.Empty<TaskAwaiter<Task<int>>>();
        }
    }
}
