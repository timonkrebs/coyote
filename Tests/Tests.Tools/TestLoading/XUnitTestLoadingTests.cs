// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Tests.Common;
using Xunit;

namespace Microsoft.Coyote.Tools.Tests
{
    public class XUnitTestLoadingTests : BaseToolsTest
    {
        public XUnitTestLoadingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        /// <summary>
        /// Entry points loaded through <see cref="TestMethodInfo"/> below. Under xUnit v3 the
        /// test classes take the v3 <see cref="ITestOutputHelper"/> in their constructor, which
        /// the loader cannot inject (its injection path is bound to the v2 abstractions for
        /// compatibility with external consumers), so the loaded methods live in this fixture
        /// with a parameterless constructor.
        /// </summary>
        public class TestFixture
        {
#pragma warning disable CA1822 // Mark members as static
            [Test]
            public void VoidTest() => Assert.True(true);

            [Test]
            public Task TaskTest()
            {
                Assert.True(true);
                return Task.CompletedTask;
            }
#pragma warning restore CA1822 // Mark members as static
        }

        [Fact(Timeout = 300000)]
        public async Task TestVoidEntryPoint() => this.CheckTestMethod(nameof(TestFixture.VoidTest));

        [Fact(Timeout = 300000)]
        public async Task TestTaskEntryPoint() => this.CheckTestMethod(nameof(TestFixture.TaskTest));

        private void CheckTestMethod(string name)
        {
            Configuration config = this.GetConfiguration();
            config.AssemblyToBeAnalyzed = Assembly.GetExecutingAssembly().Location;
            config.TestMethodName = name;
            var logWriter = new LogWriter(config);
            logWriter.SetLogger(new TestOutputLogger(this.TestOutput));
            using var testMethodInfo = TestMethodInfo.Create(config, logWriter);
            Assert.Equal(Assembly.GetExecutingAssembly(), testMethodInfo.Assembly);
            Assert.Equal($"{typeof(TestFixture).FullName}.{name}", testMethodInfo.Name);
            if (testMethodInfo.Method is Action action)
            {
                action();
            }
            else if (testMethodInfo.Method is Func<Task> function)
            {
                function();
            }
            else
            {
                Assert.Fail("Unexpected test method type.");
            }
        }
    }
}
