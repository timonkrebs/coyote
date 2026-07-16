// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using SystemLock = System.Threading.Lock;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for locks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Lock
    {
        /// <summary>
        /// The locks entered through <see cref="EnterScope"/> by the current thread that have
        /// not been disposed yet. During controlled testing the returned scopes are default
        /// instances that carry no lock identity, so the association between a scope and its
        /// lock is tracked here; lock scopes are lexically nested within a synchronous code
        /// region, so a per-thread stack suffices.
        /// </summary>
        [ThreadStatic]
        private static Stack<SystemLock> EnteredScopes;

        /// <summary>
        /// Acquires an exclusive lock on the specified lock instance.
        /// </summary>
        public static void Enter(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Monitor.SynchronizedBlock.Lock(instance);
            }
            else
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                    runtime.TryGetExecutingOperation(out ControlledOperation current))
                {
                    runtime.DelayOperation(current);
                }

                instance.Enter();
            }
        }

        /// <summary>
        /// Attempts to acquire an exclusive lock on the specified lock instance.
        /// </summary>
        public static bool TryEnter(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                return Monitor.SynchronizedBlock.Lock(instance).IsLockTaken;
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }

            return instance.TryEnter();
        }

        /// <summary>
        /// Attempts, for the specified number of milliseconds, to acquire an exclusive
        /// lock on the specified lock instance.
        /// </summary>
        public static bool TryEnter(SystemLock instance, int millisecondsTimeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // TODO: how to implement this timeout?
                return Monitor.SynchronizedBlock.Lock(instance).IsLockTaken;
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }

            return instance.TryEnter(millisecondsTimeout);
        }

        /// <summary>
        /// Attempts, for the specified amount of time, to acquire an exclusive
        /// lock on the specified lock instance.
        /// </summary>
        public static bool TryEnter(SystemLock instance, TimeSpan timeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // TODO: how to implement this timeout?
                return Monitor.SynchronizedBlock.Lock(instance).IsLockTaken;
            }
            else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }

            return instance.TryEnter(timeout);
        }

        /// <summary>
        /// Releases an exclusive lock on the specified lock instance.
        /// </summary>
        public static void Exit(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var block = Monitor.SynchronizedBlock.Find(instance) ??
                    throw new SystemThreading.SynchronizationLockException();
                block.Exit();
            }
            else
            {
                instance.Exit();
            }
        }

        /// <summary>
        /// Acquires an exclusive lock on the specified lock instance and returns
        /// a disposable scope that releases the lock upon disposal.
        /// </summary>
        public static SystemLock.Scope EnterScope(SystemLock instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Monitor.SynchronizedBlock.Lock(instance);
                EnteredScopes ??= new Stack<SystemLock>();
                EnteredScopes.Push(instance);
                return default;
            }

            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.DelayOperation(current);
            }

            return instance.EnterScope();
        }

        /// <summary>
        /// Determines whether the current thread holds the lock on the specified lock instance.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsHeldByCurrentThread(SystemLock instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var block = Monitor.SynchronizedBlock.Find(instance);
                return block != null && block.IsEntered();
            }

            return instance.IsHeldByCurrentThread;
        }

        /// <summary>
        /// Provides methods for lock scopes that can be controlled during testing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static class Scope
        {
            /// <summary>
            /// Releases the exclusive lock that was acquired by the <see cref="EnterScope"/>
            /// invocation that returned the specified scope.
            /// </summary>
            public static void Dispose(ref SystemLock.Scope scope)
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // During controlled testing the scope carries no lock identity (see the
                    // remarks on 'EnteredScopes'), so the lock to release is resolved from
                    // the current thread's stack of entered scopes.
                    if (EnteredScopes is null || EnteredScopes.Count is 0)
                    {
                        throw new SystemThreading.SynchronizationLockException();
                    }

                    var instance = EnteredScopes.Pop();
                    var block = Monitor.SynchronizedBlock.Find(instance) ??
                        throw new SystemThreading.SynchronizationLockException();
                    block.Exit();
                }
                else
                {
                    scope.Dispose();
                }
            }
        }
    }
}
#endif
