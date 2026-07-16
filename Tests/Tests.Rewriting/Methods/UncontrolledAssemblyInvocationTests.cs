// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class UncontrolledAssemblyInvocationTests : BaseRewritingTest
    {
        public UncontrolledAssemblyInvocationTests(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class TaskProvider
        {
            public static Task GetTask() => Task.CompletedTask;

            public static Task<T> GetGenericTask<T>() => Task.FromResult<T>(default(T));

            public static Task<T[]> GetGenericTaskArray<T>() => Task.FromResult<T[]>(default(T[]));

            public static Task<(int, bool)> GetValueTupleTask() => Task.FromResult((0, true));

            public static Task<(TLeft, TRight)> GetGenericValueTupleTask<TLeft, TRight>() =>
                Task.FromResult((default(TLeft), default(TRight)));

            public static Task<(T, (TLeft, TRight))> GetGenericNestedValueTupleTask<T, TLeft, TRight>() =>
                Task.FromResult((default(T), (default(TLeft), default(TRight))));
        }

        /// <summary>
        /// Helper class for task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class GenericTaskProvider<TLeft, TRight>
        {
            public static class Nested<TNested>
            {
                public static Task GetTask() => Task.CompletedTask;

                public static Task<T> GetGenericMethodTask<T>() => Task.FromResult<T>(default(T));

                public static Task<TRight> GetGenericTypeTask<T>() => Task.FromResult<TRight>(default(TRight));

                public static Task<(T, TRight, TNested)> GetGenericValueTupleTask<T>() =>
                    Task.FromResult((default(T), default(TRight), default(TNested)));
            }
        }

#if NET
        /// <summary>
        /// Helper class for value task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class ValueTaskProvider
        {
            public static ValueTask GetTask() => ValueTask.CompletedTask;

            public static ValueTask<T> GetGenericTask<T>() => ValueTask.FromResult<T>(default(T));

            public static ValueTask<T[]> GetGenericTaskArray<T>() => ValueTask.FromResult<T[]>(default(T[]));
        }

        /// <summary>
        /// Helper class for value task rewriting tests.
        /// </summary>
        /// <remarks>
        /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
        /// </remarks>
        [SkipRewriting("Must not be rewritten.")]
        private static class GenericValueTaskProvider<TLeft, TRight>
        {
            public static class Nested<TNested>
            {
                public static ValueTask GetTask() => ValueTask.CompletedTask;

                public static ValueTask<T> GetGenericMethodTask<T>() => ValueTask.FromResult<T>(default(T));

                public static ValueTask<TRight> GetGenericTypeTask<T>() => ValueTask.FromResult<TRight>(default(TRight));
            }
        }
#endif
#pragma warning restore CA1000 // Do not declare static members on generic types

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericAwaiter()
        {
            this.Test(async () =>
            {
                await new Helpers.GenericTaskAwaiter();
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new Helpers.TaskAwaiter<int>();
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericTaskArray()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericTaskArray<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetValueTupleTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericValueTupleTask<int, bool>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericNestedValueTupleTask()
        {
            this.Test(async () =>
            {
                var task = TaskProvider.GetGenericNestedValueTupleTask<int, bool, short>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericArrayTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool[]>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTupleTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericValueTupleTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericTaskFromGenericClassLargeStack()
        {
            this.Test(async () =>
            {
                var obj1 = new object();
                var obj2 = new object();
                var obj3 = new object();
                var obj4 = new object();
                var obj5 = new object();
                var task = GenericTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

#if NET
        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetTask();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTask()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetGenericTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTaskArray()
        {
            this.Test(async () =>
            {
                var task = ValueTaskProvider.GetGenericTaskArray<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericArrayValueTaskFromGenericClass()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool[]>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTaskFromGenericMethod()
        {
            this.Test(async () =>
            {
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericMethodTask<int>();
                await task;
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestUncontrolledMethodReturnsGenericValueTaskFromGenericClassLargeStack()
        {
            this.Test(async () =>
            {
                var obj1 = new object();
                var obj2 = new object();
                var obj3 = new object();
                var obj4 = new object();
                var obj5 = new object();
                var task = GenericValueTaskProvider<object, bool>.Nested<short>.GetGenericTypeTask<int>();
                await task;
            });
        }
#endif
    }
}
