// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public static class TaskConstants
    {
        static readonly Task<bool> BooleanTrueTask = Task.FromResult(true);
        static readonly TaskCompletionSource<int> Int32ZeroPromise = CreateCompletedPromise();

        static TaskCompletionSource<int> CreateCompletedPromise()
        {
            var promise = new TaskCompletionSource<int>();
            promise.TrySetResult(0);
            return promise;
        }

        /// <summary>
        ///     A task that has been completed with the value <c>true</c>.
        /// </summary>
        public static Task<bool> BooleanTrue => BooleanTrueTask;

        /// <summary>
        ///     A task that has been completed with the value <c>false</c>.
        /// </summary>
        public static Task<bool> BooleanFalse => GetDefault<bool>();

        /// <summary>
        ///     A task that has been completed with the value <c>0</c>.
        /// </summary>
        public static Task<int> Int32Zero => Int32ZeroPromise.Task;

        /// <summary>
        ///     A task that has been completed with the value <c>-1</c>.
        /// </summary>
        public static Task<int> Int32NegativeOne { get; } = Task.FromResult(-1);

        /// <summary>
        ///     A <see cref="Task" /> that has been completed.
        /// </summary>
        public static Task Completed => BooleanTrueTask;

        public static TaskCompletionSource<int> CompletedPromise => Int32ZeroPromise;

        /// <summary>
        ///     A <see cref="Task" /> that will never complete.
        /// </summary>
        public static Task Never => GetNeverCompleteTask<bool>();

        /// <summary>
        ///     A task that has been canceled.
        /// </summary>
        public static Task Canceled => GetCanceledTask<bool>();

        public static TaskCompletionSource<T> CreateCanceledPromise<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs;
        }

        /// <summary>
        ///     A task that has been completed with the default value of <typeparamref name="T" />.
        /// </summary>
        public static Task<T> GetDefault<T>()
        {
            return Task.FromResult(default(T));
        }

        /// <summary>
        ///     A <see cref="Task" /> that will never complete.
        /// </summary>
        public static Task<T> GetNeverCompleteTask<T>()
        {
            return new TaskCompletionSource<T>().Task;
        }

        /// <summary>
        ///     A task that has been canceled.
        /// </summary>
        public static Task<T> GetCanceledTask<T>()
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            taskCompletionSource.SetCanceled();

            return taskCompletionSource.Task;
        }
    }
}
