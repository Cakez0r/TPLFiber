using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utility
{
    /// <summary>
    /// A synchronised fiber that uses the default thread pool and TPL
    /// </summary>
    public class Fiber
    {
        private ConcurrentExclusiveSchedulerPair m_schedulers = new ConcurrentExclusiveSchedulerPair();

        /// <summary>
        /// Enqueue an async function to be awaited
        /// </summary>
        /// <param name="f">The function that should be awaited</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task<T> EnqueueAsync<T>(Func<Task<T>> f, bool exclusive = false)
        {
            Task<Task<T>> wrapper = new Task<Task<T>>(() =>
            {
                //The inner task represents the eventual completion of awaiting the async function
                //This runs on the default task scheduler

                //See https://blogs.msdn.com/b/pfxteam/archive/2011/10/24/10229468.aspx

                Task<T> inner = Task<T>.Run(f);

                //The outer task waits for the inner task to complete on one of the fiber's schedulers
                inner.Wait();

                return inner;
            });

            Start(wrapper, exclusive);

            //The outer task is "unwrapped" so that callers can await the inner task
            return wrapper.Unwrap(); 
        }

        /// <summary>
        /// Enqueue an async function to be awaited
        /// </summary>
        /// <param name="f">The function that should be awaited</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task EnqueueAsync(Func<Task> f, bool exclusive = false)
        {
            Task<Task> wrapper = new Task<Task>(() =>
            {
                Task inner = Task.Run(f);
                inner.Wait();
                return inner;
            });

            Start(wrapper, exclusive);

            return wrapper.Unwrap();
        }

        /// <summary>
        /// Enqueue a function
        /// </summary>
        /// <param name="f">The function to enqueue</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task<T> Enqueue<T>(Func<T> f, bool exclusive = false)
        {
            Task<T> task = new Task<T>(f);

            Start(task, exclusive);

            return task;
        }

        /// <summary>
        /// Enqueue a function
        /// </summary>
        /// /// <param name="f">The function to enqueue</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task Enqueue(Action f, bool exclusive = false)
        {
            Task task = new Task(f);

            Start(task, exclusive);

            return task;
        }

        /// <summary>
        /// Schedule a function to be enqueued in the future
        /// </summary>
        /// <param name="f">The function to schedule</param>
        /// <param name="waitTime">How long to wait before enqueueing the function</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task Schedule(Action f, TimeSpan waitTime, bool exclusive = false)
        {
            return Task.Delay(waitTime).ContinueWith((t) => Enqueue(f, exclusive)).Unwrap();
        }

        /// <summary>
        /// Schedule a function to be enqueued in the future
        /// </summary>
        /// <param name="f">The function to schedule</param>
        /// <param name="waitTime">How long to wait before enqueueing the function</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task<T> Schedule<T>(Func<T> f, TimeSpan waitTime, bool exclusive = false)
        {
            return Task.Delay(waitTime).ContinueWith((t) => Enqueue<T>(f, exclusive)).Unwrap();
        }

        /// <summary>
        /// Schedule an async function to be awaited in the future
        /// </summary>
        /// <param name="f">The function to schedule</param>
        /// <param name="waitTime">How long to wait before awaiting the function</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task ScheduleAsync(Func<Task> f, TimeSpan waitTime, bool exclusive = false)
        {
            return Task.Delay(waitTime).ContinueWith((t) => EnqueueAsync(f, exclusive)).Unwrap();
        }

        /// <summary>
        /// Schedule an async function to be awaited in the future
        /// </summary>
        /// <param name="f">The function to schedule</param>
        /// <param name="waitTime">How long to wait before awaiting the function</param>
        /// <param name="exclusive">Should the function run exclusively?</param>
        public Task<T> ScheduleAsync<T>(Func<Task<T>> f, TimeSpan waitTime, bool exclusive = false)
        {
            return Task.Delay(waitTime).ContinueWith((t) => EnqueueAsync<T>(f, exclusive)).Unwrap();
        }

        private void Start(Task t, bool exclusive)
        {
            t.Start(exclusive ? m_schedulers.ExclusiveScheduler : m_schedulers.ConcurrentScheduler);
        }
    }
}
