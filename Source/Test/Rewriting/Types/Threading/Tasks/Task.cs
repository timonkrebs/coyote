// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTaskFactory = System.Threading.Tasks.TaskFactory;
using SystemTasks = System.Threading.Tasks;
using SystemTimeout = System.Threading.Timeout;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Provides methods for creating tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Task
    {
        /// <summary>
        /// Gets a task that has already completed successfully.
        /// </summary>
        public static SystemTask CompletedTask { get; } = SystemTask.CompletedTask;

        /// <summary>
        /// The default task factory.
        /// </summary>
        private static SystemTaskFactory DefaultFactory = new SystemTaskFactory();

        /// <summary>
        /// Provides access to factory methods for creating controlled task and generic task instances.
        /// </summary>
        public static SystemTaskFactory Factory
        {
            get
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    return DefaultFactory;
                }

                return runtime.TaskFactory;
            }
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task object that
        /// represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask Run(Action action) => Run(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task
        /// object that represents that work.
        /// </summary>
        public static SystemTask Run(Action action, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(action, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(action, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task
        /// object that represents that work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task object that
        /// represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTasks.Task<TResult> Run<TResult>(Func<TResult> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the task returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask Run(Func<SystemTask> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the task
        /// returned by the function. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTask Run(Func<SystemTask> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler).Unwrap();
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// generic task returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult> Run<TResult>(Func<SystemTasks.Task<TResult>> function) =>
            Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the generic
        /// task returned by the function. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTasks.Task<TResult> Run<TResult>(Func<SystemTasks.Task<TResult>> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler).Unwrap();
        }

        /// <summary>
        /// Creates a task that completes after a time delay.
        /// </summary>
        public static SystemTask Delay(int millisecondsDelay)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(millisecondsDelay);
            }

            return runtime.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), default);
        }

        /// <summary>
        /// Creates a task that completes after a time delay.
        /// </summary>
        public static SystemTask Delay(int millisecondsDelay, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(millisecondsDelay, cancellationToken);
            }

            return runtime.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(delay);
            }

            return runtime.ScheduleDelay(delay, default);
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(delay, cancellationToken);
            }

            return runtime.ScheduleDelay(delay, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(params SystemTask[] tasks)
        {
            SystemTask task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(IEnumerable<SystemTask> tasks)
        {
            SystemTask task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(params SystemTasks.Task<TResult>[] tasks)
        {
            SystemTasks.Task<TResult[]> task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(IEnumerable<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<TResult[]> task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(params SystemTask[] tasks)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when any task in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(IEnumerable<SystemTask> tasks)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET
        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(SystemTask task1, SystemTask task2)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(task1, task2);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            SystemTasks.Task<TResult> task1, SystemTasks.Task<TResult> task2)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(task1, task2);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            params SystemTasks.Task<TResult>[] tasks)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when any task in the specified
        /// enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            IEnumerable<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(params SystemTask[] tasks) =>
            CoyoteRuntime.Current.SchedulingPolicy is SchedulingPolicy.None ?
                SystemTask.WhenEach(tasks) :
                new WhenEachAsyncEnumerable<SystemTask>(ValidateAndSnapshotTasks(tasks));

        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(ReadOnlySpan<SystemTask> tasks) =>
            WhenEach(tasks.ToArray());

        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(IEnumerable<SystemTask> tasks) =>
            CoyoteRuntime.Current.SchedulingPolicy is SchedulingPolicy.None ?
                SystemTask.WhenEach(tasks) :
                new WhenEachAsyncEnumerable<SystemTask>(ValidateAndSnapshotTasks(tasks));

        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            params SystemTasks.Task<TResult>[] tasks) =>
            CoyoteRuntime.Current.SchedulingPolicy is SchedulingPolicy.None ?
                SystemTask.WhenEach(tasks) :
                new WhenEachAsyncEnumerable<SystemTasks.Task<TResult>>(ValidateAndSnapshotTasks(tasks));

        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            ReadOnlySpan<SystemTasks.Task<TResult>> tasks) =>
            WhenEach(tasks.ToArray());

        /// <summary>
        /// Creates an async enumerable that yields the supplied tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            IEnumerable<SystemTasks.Task<TResult>> tasks) =>
            CoyoteRuntime.Current.SchedulingPolicy is SchedulingPolicy.None ?
                SystemTask.WhenEach(tasks) :
                new WhenEachAsyncEnumerable<SystemTasks.Task<TResult>>(ValidateAndSnapshotTasks(tasks));

        /// <summary>
        /// Validates the tasks argument like the invoked API does and snapshots it: a null
        /// collection or a null entry must throw eagerly at the call, not lazily when the
        /// (deferred) iterator is first moved, and not as a NullReferenceException from the
        /// controlled wait.
        /// </summary>
        private static List<TTask> ValidateAndSnapshotTasks<TTask>(IEnumerable<TTask> tasks)
            where TTask : SystemTask
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            var snapshot = new List<TTask>(tasks);
            if (snapshot.Contains(null))
            {
                throw new ArgumentException("The tasks argument included a null value.", nameof(tasks));
            }

            return snapshot;
        }

        /// <summary>
        /// The async enumerable a controlled WhenEach returns. Like the invoked API, only
        /// the first enumeration yields the tasks; later enumerators observe an
        /// already-drained stream instead of splitting one stream between consumers.
        /// </summary>
        private sealed class WhenEachAsyncEnumerable<TTask> : IAsyncEnumerable<TTask>
            where TTask : SystemTask
        {
            /// <summary>
            /// The tasks not yielded yet.
            /// </summary>
            private readonly List<TTask> Remaining;

            /// <summary>
            /// Set once the first enumerator has been handed out.
            /// </summary>
            private int IsEnumerated;

            internal WhenEachAsyncEnumerable(List<TTask> remaining)
            {
                this.Remaining = remaining;
            }

            public IAsyncEnumerator<TTask> GetAsyncEnumerator(SystemCancellationToken cancellationToken = default)
            {
                var tasks = System.Threading.Interlocked.Exchange(ref this.IsEnumerated, 1) is 0 ?
                    this.Remaining : new List<TTask>();
                return WhenEachInCompletionOrder(tasks, cancellationToken).GetAsyncEnumerator(default);
            }
        }

        /// <summary>
        /// Yields the given tasks in completion order under scheduler control. Each round
        /// performs the controlled equivalent of a WaitAny (pausing the consuming operation
        /// until at least one remaining task completes at a scheduling point), then yields
        /// a completed task; when several are already complete, the choice among them is a
        /// controlled nondeterministic one, so every completion order the real enumerable
        /// could surface stays reachable during exploration. Every await the consumer
        /// performs on the returned enumerable resolves inline on an already-completed
        /// value, so no task machinery escapes the scheduler's control.
        /// </summary>
        private static async IAsyncEnumerable<TTask> WhenEachInCompletionOrder<TTask>(
            List<TTask> remaining,
            [System.Runtime.CompilerServices.EnumeratorCancellation] SystemCancellationToken cancellationToken)
            where TTask : SystemTask
        {
            // Inline no-op await: gives the method the async-iterator shape the signature
            // requires without introducing an uncontrolled asynchronous hop.
            await SystemTask.CompletedTask;
            while (remaining.Count > 0)
            {
                // The token arrives through WithCancellation/GetAsyncEnumerator. Like the
                // invoked API, it is observed both before and during the wait: the controlled
                // pause below also wakes when the token is canceled, so a cancellation issued
                // by another controlled operation while every remaining task is still running
                // surfaces as OperationCanceledException instead of a reported deadlock.
                cancellationToken.ThrowIfCancellationRequested();

                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy != SchedulingPolicy.None)
                {
                    TaskServices.WaitUntilAnyTaskCompletesOrCanceled(runtime, remaining.ToArray(), cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var completed = new List<int>();
                for (int i = 0; i < remaining.Count; i++)
                {
                    if (remaining[i].IsCompleted)
                    {
                        completed.Add(i);
                    }
                }

                int index;
                if (completed.Count is 0)
                {
                    // Under the fuzzing policy the controlled wait only injects a delay
                    // rather than guaranteeing a completion, so fall through to the real
                    // (token-aware) wait, exactly like the WaitAny model above does.
                    index = SystemTask.WaitAny(remaining.ToArray(), cancellationToken);
                }
                else if (completed.Count is 1)
                {
                    index = completed[0];
                }
                else
                {
                    // Several tasks are already complete: the real enumerable yields them in
                    // true completion order, which is schedule-dependent, so the pick among
                    // them is a controlled nondeterministic choice -- systematic exploration
                    // then covers each order a real execution could observe.
                    index = completed[runtime.GetNextNondeterministicIntegerChoice(completed.Count, null, null)];
                }

                TTask next = remaining[index];
                remaining.RemoveAt(index);
                yield return next;
            }
        }
#endif

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params SystemTask[] tasks) =>
            WaitAll(tasks, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified time interval.
        /// </summary>
        public static bool WaitAll(SystemTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAll(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution within
        /// a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(SystemTask[] tasks, int millisecondsTimeout) =>
            WaitAll(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided task objects to complete execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(SystemTask[] tasks, SystemCancellationToken cancellationToken) =>
            WaitAll(tasks, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified
        /// number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public static bool WaitAll(SystemTask[] tasks, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None && tasks != null)
            {
                // TODO: support timeouts during testing, this would become false if there is a timeout.
                TaskServices.WaitUntilAllTasksComplete(runtime, tasks);
            }

            return SystemTask.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params SystemTask[] tasks) =>
            WaitAny(tasks, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified time interval.
        /// </summary>
        public static int WaitAny(SystemTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAny(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within
        /// a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(SystemTask[] tasks, int millisecondsTimeout) =>
            WaitAny(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(SystemTask[] tasks, SystemCancellationToken cancellationToken) =>
            WaitAny(tasks, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified
        /// number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public static int WaitAny(SystemTask[] tasks, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None && tasks != null)
            {
                // TODO: support timeouts during testing, this would become -1 if there is a timeout.
                TaskServices.WaitUntilAnyTaskCompletes(runtime, tasks);
            }

            return SystemTask.WaitAny(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for the specified task to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SystemTask task) => Wait(task, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for the specified task to complete execution within a specified time interval.
        /// </summary>
        public static bool Wait(SystemTask task, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait(task, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for the specified task to complete execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SystemTask task, int millisecondsTimeout) =>
            Wait(task, millisecondsTimeout, default);

        /// <summary>
        /// Waits for the specified task to complete execution. The wait terminates if a cancellation
        /// token is canceled before the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SystemTask task, SystemCancellationToken cancellationToken) =>
            Wait(task, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for the specified task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        public static bool Wait(SystemTask task, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                TaskServices.WaitUntilTaskCompletes(runtime, task);
            }

            return task.Wait(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that has completed successfully with the specified result.
        /// </summary>
        public static SystemTasks.Task<TResult> FromResult<TResult>(TResult result)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromResult(result);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTask FromCanceled(SystemCancellationToken cancellationToken)
        {
            SystemTask task = SystemTask.FromCanceled(cancellationToken);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTasks.Task<TResult> FromCanceled<TResult>(SystemCancellationToken cancellationToken)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromCanceled<TResult>(cancellationToken);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTask FromException(Exception exception)
        {
            SystemTask task = SystemTask.FromException(exception);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTasks.Task<TResult> FromException<TResult>(Exception exception)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromException<TResult>(exception);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Returns a task awaiter for the specified task.
        /// </summary>
        public static TaskAwaiter GetAwaiter(SystemTask task) => new TaskAwaiter(task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredTaskAwaitable ConfigureAwait(SystemTask task,
            bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable(task, continueOnCapturedContext);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        public static YieldAwaitable Yield() => new YieldAwaitable(default);
    }

    /// <summary>
    /// Provides methods for creating generic tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Task<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// The default generic task factory.
        /// </summary>
        private static SystemTasks.TaskFactory<TResult> DefaultFactory = new SystemTasks.TaskFactory<TResult>();

        /// <summary>
        /// Provides access to factory methods for creating controlled generic task instances.
        /// </summary>
        public static SystemTasks.TaskFactory<TResult> Factory
        {
            get
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    return DefaultFactory;
                }

                // TODO: cache this per runtime.
                return new SystemTasks.TaskFactory<TResult>(SystemCancellationToken.None,
                    SystemTaskCreationOptions.HideScheduler, SystemTaskContinuationOptions.HideScheduler,
                    runtime.ControlledTaskScheduler);
            }
        }

        /// <summary>
        /// Gets the result value of the specified generic task.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static TResult get_Result(SystemTasks.Task<TResult> task)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                TaskServices.WaitUntilTaskCompletes(runtime, task);
            }

            return task.Result;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Returns a generic task awaiter for the specified generic task.
        /// </summary>
        public static TaskAwaiter<TResult> GetAwaiter(SystemTasks.Task<TResult> task) =>
            new TaskAwaiter<TResult>(task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredTaskAwaitable<TResult> ConfigureAwait(
            SystemTasks.Task<TResult> task, bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable<TResult>(task, continueOnCapturedContext);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
