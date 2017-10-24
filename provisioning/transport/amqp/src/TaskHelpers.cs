// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public static class TaskHelpers
    {
        public static void Fork(this Task thisTask)
        {
            Fork(thisTask, "TaskExtensions.Fork");
        }

        public static void Fork(this Task thisTask, string tracingInfo)
        {
            //Fx.Assert(thisTask != null, "task is required!");
            //thisTask.ContinueWith(t => Fx.Exception.TraceHandled(t.Exception, tracingInfo), TaskContinuationOptions.OnlyOnFaulted);
            throw new NotImplementedException("TaskHelpers.Fork(" + thisTask + " ,"+ tracingInfo + " ");
        }

        public static IAsyncResult ToAsyncResult(this Task task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        (t, st) => ((AsyncCallback)state)(t),
                        callback,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(
                t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            tcs.TrySetResult(null);
                            break;
                        case TaskStatus.Canceled:
                            tcs.TrySetCanceled();
                            break;
                        case TaskStatus.Faulted:
                            tcs.TrySetException(t.Exception.InnerExceptions);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("task", "t.Status not defined");
                    }

                    callback?.Invoke(tcs.Task);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        public static void EndAsyncResult(IAsyncResult asyncResult)
        {
            Task task = asyncResult as Task;
            if (task == null)
            {
                throw new ArgumentException($"Given {nameof(asyncResult)} is not subclass of Task.");
            }

            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                throw ae.GetBaseException();
            }
        }
    }
}
