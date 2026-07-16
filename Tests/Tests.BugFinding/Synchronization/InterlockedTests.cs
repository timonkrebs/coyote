// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Interlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class InterlockedTests : BaseBugFindingTest
    {
        public InterlockedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedReadLong()
        {
            this.Test(() =>
            {
                long value = long.MaxValue - 42;
                Assert.Equal(long.MaxValue - 42, Interlocked.Read(ref value));
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedReadULong()
        {
            this.Test(() =>
            {
                ulong value = ulong.MaxValue - 42;
                Assert.Equal(ulong.MaxValue - 42, Interlocked.Read(ref value));
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAddInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(12387, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387, value);
                Assert.Equal(12387, Interlocked.Add(ref value, 0));
                Assert.Equal(12387, value);
                Assert.Equal(12386, Interlocked.Add(ref value, -1));
                Assert.Equal(12386, value);

                value = int.MaxValue;
                Assert.Equal(int.MinValue, Interlocked.Add(ref value, 1));
                Assert.Equal(int.MinValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAddLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(12387, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387, value);
                Assert.Equal(12387, Interlocked.Add(ref value, 0));
                Assert.Equal(12387, value);
                Assert.Equal(12386, Interlocked.Add(ref value, -1));
                Assert.Equal(12386, value);

                value = long.MaxValue;
                Assert.Equal(long.MinValue, Interlocked.Add(ref value, 1));
                Assert.Equal(long.MinValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAddUInt()
        {
            this.Test(() =>
            {
                uint value = 42;
                Assert.Equal(12387u, Interlocked.Add(ref value, 12345u));
                Assert.Equal(12387u, value);
                Assert.Equal(12387u, Interlocked.Add(ref value, 0u));
                Assert.Equal(12387u, value);
                Assert.Equal(9386u, Interlocked.Add(ref value, 4294964295u));
                Assert.Equal(9386u, value);

                value = uint.MaxValue;
                Assert.Equal(0u, Interlocked.Add(ref value, 1));
                Assert.Equal(0u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAddULong()
        {
            this.Test(() =>
            {
                ulong value = 42;
                Assert.Equal(12387u, Interlocked.Add(ref value, 12345));
                Assert.Equal(12387u, value);
                Assert.Equal(12387u, Interlocked.Add(ref value, 0));
                Assert.Equal(12387u, value);
                Assert.Equal(10771u, Interlocked.Add(ref value, 18446744073709550000));
                Assert.Equal(10771u, value);

                value = ulong.MaxValue;
                Assert.Equal(0u, Interlocked.Add(ref value, 1));
                Assert.Equal(0u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedIncrementInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(43, Interlocked.Increment(ref value));
                Assert.Equal(43, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedIncrementLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(43, Interlocked.Increment(ref value));
                Assert.Equal(43, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedIncrementUInt()
        {
            this.Test(() =>
            {
                uint value = 42u;
                Assert.Equal(43u, Interlocked.Increment(ref value));
                Assert.Equal(43u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedIncrementULong()
        {
            this.Test(() =>
            {
                ulong value = 42u;
                Assert.Equal(43u, Interlocked.Increment(ref value));
                Assert.Equal(43u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedDecrementInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(41, Interlocked.Decrement(ref value));
                Assert.Equal(41, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedDecrementLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(41, Interlocked.Decrement(ref value));
                Assert.Equal(41, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedDecrementUInt()
        {
            this.Test(() =>
            {
                uint value = 42u;
                Assert.Equal(41u, Interlocked.Decrement(ref value));
                Assert.Equal(41u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedDecrementULong()
        {
            this.Test(() =>
            {
                ulong value = 42u;
                Assert.Equal(41u, Interlocked.Decrement(ref value));
                Assert.Equal(41u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeInt()
        {
            this.Test(() =>
            {
                int value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeLong()
        {
            this.Test(() =>
            {
                long value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeUInt()
        {
            this.Test(() =>
            {
                uint value = 42;
                Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeULong()
        {
            this.Test(() =>
            {
                ulong value = 42;
                Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeFloat()
        {
            this.Test(() =>
            {
                float value = 42.1f;
                Assert.Equal(42.1f, Interlocked.Exchange(ref value, 12345.1f));
                Assert.Equal(12345.1f, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeDouble()
        {
            this.Test(() =>
            {
                double value = 42.1;
                Assert.Equal(42.1, Interlocked.Exchange(ref value, 12345.1));
                Assert.Equal(12345.1, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeObject()
        {
            this.Test(() =>
            {
                var oldValue = new object();
                var newValue = new object();
                object value = oldValue;

                Assert.Same(oldValue, Interlocked.Exchange(ref value, newValue));
                Assert.Same(newValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeBoxedObject()
        {
            this.Test(() =>
            {
                var oldValue = (object)42;
                var newValue = (object)12345;
                object value = oldValue;

                object valueBeforeUpdate = Interlocked.Exchange(ref value, newValue);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(newValue, value);
                Assert.Equal(12345, (int)value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeInt()
        {
            this.Test(() =>
            {
                int value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeLong()
        {
            this.Test(() =>
            {
                long value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeUInt()
        {
            this.Test(() =>
            {
                uint value = 42;

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
                Assert.Equal(42u, value);

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeULong()
        {
            this.Test(() =>
            {
                ulong value = 42;

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
                Assert.Equal(42u, value);

                Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
                Assert.Equal(12345u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeFloat()
        {
            this.Test(() =>
            {
                float value = 42.1f;

                Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 41.1f));
                Assert.Equal(42.1f, value);

                Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 42.1f));
                Assert.Equal(12345.1f, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeDouble()
        {
            this.Test(() =>
            {
                double value = 42.1;

                Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 41.1));
                Assert.Equal(42.1, value);

                Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 42.1));
                Assert.Equal(12345.1, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeObject()
        {
            this.Test(() =>
            {
                var oldValue = new object();
                var newValue = new object();
                object value = oldValue;

                Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, new object()));
                Assert.Same(oldValue, value);

                Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, oldValue));
                Assert.Same(newValue, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeBoxedObject()
        {
            this.Test(() =>
            {
                var oldValue = (object)42;
                var newValue = (object)12345;
                object value = oldValue;

                object valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, (object)42);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(oldValue, value);
                Assert.Equal(42, (int)value);

                valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, oldValue);
                Assert.Same(oldValue, valueBeforeUpdate);
                Assert.Equal(42, (int)valueBeforeUpdate);
                Assert.Same(newValue, value);
                Assert.Equal(12345, (int)value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

#if NET
        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAndInt()
        {
            this.Test(() =>
            {
                int value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAndLong()
        {
            this.Test(() =>
            {
                long value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAndUInt()
        {
            this.Test(() =>
            {
                uint value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedAndULong()
        {
            this.Test(() =>
            {
                ulong value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
                Assert.Equal(0x02244220u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedOrInt()
        {
            this.Test(() =>
            {
                int value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedOrLong()
        {
            this.Test(() =>
            {
                long value = 0x12345670;
                Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedOrUInt()
        {
            this.Test(() =>
            {
                uint value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedOrULong()
        {
            this.Test(() =>
            {
                ulong value = 0x12345670u;
                Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
                Assert.Equal(0x17755771u, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }
#endif

#if NET10_0_OR_GREATER
        private enum TestColor : byte
        {
            Red = 0,
            Green = 1,
            Blue = 2,
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeByte()
        {
            this.Test(() =>
            {
                byte value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 123));
                Assert.Equal(123, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeSByte()
        {
            this.Test(() =>
            {
                sbyte value = -42;
                Assert.Equal(-42, Interlocked.Exchange(ref value, -123));
                Assert.Equal(-123, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeShort()
        {
            this.Test(() =>
            {
                short value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeUShort()
        {
            this.Test(() =>
            {
                ushort value = 42;
                Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedExchangeEnum()
        {
            this.Test(() =>
            {
                // The generic Exchange lost its reference-type constraint in .NET 9, so it
                // now binds for enums and must round-trip through the model unchanged.
                TestColor value = TestColor.Red;
                Assert.Equal(TestColor.Red, Interlocked.Exchange(ref value, TestColor.Green));
                Assert.Equal(TestColor.Green, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeByte()
        {
            this.Test(() =>
            {
                byte value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 123, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 123, 42));
                Assert.Equal(123, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeSByte()
        {
            this.Test(() =>
            {
                sbyte value = -42;

                Assert.Equal(-42, Interlocked.CompareExchange(ref value, -123, -41));
                Assert.Equal(-42, value);

                Assert.Equal(-42, Interlocked.CompareExchange(ref value, -123, -42));
                Assert.Equal(-123, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeShort()
        {
            this.Test(() =>
            {
                short value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedCompareExchangeUShort()
        {
            this.Test(() =>
            {
                ushort value = 42;

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
                Assert.Equal(42, value);

                Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
                Assert.Equal(12345, value);
            }, configuration: this.GetConfiguration().WithAtomicOperationRaceCheckingEnabled(true));
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedByteLostUpdateDetection()
        {
            this.TestWithError(async () =>
            {
                byte counter = 0;

                // BUG (seeded): a read-modify-write composed from two separate atomic
                // operations instead of a CAS loop, so two concurrent increments can both
                // read 0 and both write 1. The reads and writes are the ONLY scheduling
                // points in these tasks, so the lost update is discoverable only if the
                // byte overloads of CompareExchange/Exchange are intercepted by the model.
                static void BrokenIncrement(ref byte location)
                {
                    byte value = Interlocked.CompareExchange(ref location, 0, 0);
                    Interlocked.Exchange(ref location, (byte)(value + 1));
                }

                Task t1 = Task.Run(() => BrokenIncrement(ref counter));
                Task t2 = Task.Run(() => BrokenIncrement(ref counter));
                await Task.WhenAll(t1, t2);

                byte result = Interlocked.CompareExchange(ref counter, 0, 0);
                Specification.Assert(result is 2, "Detected lost update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200)
                .WithAtomicOperationRaceCheckingEnabled(true),
            expectedError: "Detected lost update.",
            replay: true);
        }

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedInt16LostUpdateDetection()
        {
            this.TestWithError(async () =>
            {
                short counter = 0;

                // Same seeded lost update as the byte variant, over the 16-bit overloads.
                static void BrokenIncrement(ref short location)
                {
                    short value = Interlocked.CompareExchange(ref location, 0, 0);
                    Interlocked.Exchange(ref location, (short)(value + 1));
                }

                Task t1 = Task.Run(() => BrokenIncrement(ref counter));
                Task t2 = Task.Run(() => BrokenIncrement(ref counter));
                await Task.WhenAll(t1, t2);

                short result = Interlocked.CompareExchange(ref counter, 0, 0);
                Specification.Assert(result is 2, "Detected lost update.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200)
                .WithAtomicOperationRaceCheckingEnabled(true),
            expectedError: "Detected lost update.",
            replay: true);
        }
#endif

        [Fact(Timeout = 300000)]
        public async Task TestInterlockedConcurrentIncrementInt()
        {
            this.Test(() =>
            {
                int value = 0;
                const int taskCount = 3;
                const int iterationCount = 10;
                var tasks = new Task[taskCount];
                for (int i = 0; i < taskCount; ++i)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        for (int i = 0; i < iterationCount; ++i)
                        {
                            Interlocked.Increment(ref value);
                        }
                    });
                }

                Task.WaitAll(tasks);
                Assert.Equal(taskCount * iterationCount, value);
            }, configuration: this.GetConfiguration().WithTestingIterations(10)
                .WithAtomicOperationRaceCheckingEnabled(true));
        }
    }
}
