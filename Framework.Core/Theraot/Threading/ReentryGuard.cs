﻿// Needed for Workaround

using System;
using System.Diagnostics;
using Theraot.Collections.ThreadSafe;
using Theraot.Threading.Needles;

namespace Theraot.Threading
{
    /// <summary>
    /// Represents a context to execute operation without reentry.
    /// </summary>
    [DebuggerNonUserCode]
    public sealed class ReentryGuard
    {
        private readonly UniqueId _id;
        private readonly SafeQueue<Action> _workQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReentryGuard" /> class.
        /// </summary>
        public ReentryGuard()
        {
            _workQueue = new SafeQueue<Action>();
            _id = RuntimeUniqueIdProvider.GetNextId();
        }

        /// <summary>
        /// Gets a value indicating whether or not the current thread did enter.
        /// </summary>
        public bool IsTaken => ReentryGuardHelper.IsTaken(_id);

        /// <summary>
        /// Executes an operation-
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise Execute(Action operation)
        {
            var result = AddExecution(operation, _workQueue);
            ExecutePending(_workQueue, _id);
            return result;
        }

        /// <summary>
        /// Executes an operation-
        /// </summary>
        /// <typeparam name="T">The return value of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise<T> Execute<T>(Func<T> operation)
        {
            var result = AddExecution(operation, _workQueue);
            ExecutePending(_workQueue, _id);
            return result;
        }

        private static IPromise AddExecution(Action action, SafeQueue<Action> queue)
        {
            var promised = new Promise(false);
            var result = new ReadOnlyPromise(promised, false);
            queue.Add
            (
                () =>
                {
                    try
                    {
                        action.Invoke();
                        promised.SetCompleted();
                    }
                    catch (Exception exception)
                    {
                        promised.SetError(exception);
                    }
                }
            );
            return result;
        }

        private static IPromise<T> AddExecution<T>(Func<T> action, SafeQueue<Action> queue)
        {
            var promised = new PromiseNeedle<T>(false);
            var result = new ReadOnlyPromiseNeedle<T>(promised, false);
            queue.Add
            (
                () =>
                {
                    try
                    {
                        promised.Value = action.Invoke();
                    }
                    catch (Exception exception)
                    {
                        promised.SetError(exception);
                    }
                }
            );
            return result;
        }

        private static void ExecutePending(SafeQueue<Action> queue, UniqueId id)
        {
            var didEnter = false;
            try
            {
                didEnter = ReentryGuardHelper.Enter(id);
                if (!didEnter)
                {
                    // called from inside this method - skip
                    return;
                }
                while (queue.TryTake(out var action))
                {
                    action.Invoke();
                }
            }
            finally
            {
                if (didEnter)
                {
                    ReentryGuardHelper.Leave(id);
                }
            }
        }
    }
}