// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskFactoryRewritingTests : BaseRewritingTest
    {
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
        public TaskFactoryRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingTaskFactoryStartNew()
        {
            await Task.Factory.StartNew(() => { });
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingGenericTaskFactoryStartNew()
        {
            await Task<int>.Factory.StartNew(() => 1);
        }

        [Fact(Timeout = 300000)]
        public async Task TestRewritingNestedGenericTaskFactoryStartNew()
        {
            await Task<Task<int>>.Factory.StartNew(() => Task.FromResult(1));
        }
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
    }
}
