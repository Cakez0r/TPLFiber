using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using Utility;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class FiberTests
    {
        [TestMethod]
        public void WriteExecutionOrderIsCorrect()
        {
            const int WRITE_COUNT = 10000;

            ConcurrentQueue<int?> queue = new ConcurrentQueue<int?>();

            Fiber fiber = new Fiber();

            for (int i = 0; i < WRITE_COUNT; i++)
            {
                int? x = i;
                fiber.Enqueue(() => 
                {
                    queue.Enqueue(x);
                }, true);
            }

            while (queue.Count != WRITE_COUNT)
            {
                Thread.Sleep(0);
            }

            Assert.IsTrue(queue.Zip(Enumerable.Range(0, WRITE_COUNT), (i, j) => i == j).All(b => b));
        }

        [TestMethod]
        public void ReadWriteExecutionOrderIsCorrect()
        {
            int? state = 0;

            Fiber fiber = new Fiber();

            fiber.Enqueue(() =>
            {
                Thread.Sleep(100);
                Assert.AreEqual(state.Value, 0);
            });

            fiber.Enqueue(() =>
            {
                state = 1;
            }, true);

            Task finalTask = fiber.Enqueue(() =>
            {
                Assert.AreEqual(state.Value, 1);
            });

            finalTask.Wait();
        }

        [TestMethod]
        public void WriteAsyncExecutionOrderIsCorrect()
        {
            const int WRITE_COUNT = 1000;

            ConcurrentQueue<int?> queue = new ConcurrentQueue<int?>();

            Fiber fiber = new Fiber();

            for (int i = 0; i < WRITE_COUNT; i++)
            {
                int? x = i;
                Task t = fiber.EnqueueAsync(async () =>
                {
                    await Task.Delay(0);
                    queue.Enqueue(x);
                }, true);
            }

            while (queue.Count != WRITE_COUNT)
            {
                Thread.Sleep(10);
            }

            Assert.IsTrue(queue.Zip(Enumerable.Range(0, WRITE_COUNT), (i, j) => i == j).All(b => b));
        }

        [TestMethod]
        public void ReadWriteAsyncExecutionOrderIsCorrect()
        {
            int? state = 0;

            Fiber fiber = new Fiber();

            fiber.EnqueueAsync(async () =>
            {
                await Task.Delay(100);
                Assert.AreEqual(state.Value, 0);
            });

            fiber.EnqueueAsync(async () =>
            {
                await Task.Delay(0);
                state = 1;
            }, true);

            Task finalTask = fiber.EnqueueAsync(async () =>
            {
                await Task.Delay(0);
                Assert.AreEqual(state.Value, 1);
            });

            finalTask.Wait();
        }

        [TestMethod]
        public void ReadsCorrectValue()
        {
            Fiber fiber = new Fiber();
            int random = new Random().Next();

            Task<int> result = fiber.Enqueue(() => random);
            result.Wait();

            Assert.AreEqual(result.Result, random);
        }

        [TestMethod]
        public void ReadsCorrectValueAsync()
        {
            Fiber fiber = new Fiber();
            int random = new Random().Next();

            Task<int> result = fiber.EnqueueAsync(async () => 
            {
                await Task.Delay(0);
                return random;
            });

            result.Wait();

            Assert.AreEqual(result.Result, random);
        }

        [TestMethod]
        public void ScheduleFinishesOnTime()
        {
            bool? state = false;

            Fiber fiber = new Fiber();
            TimeSpan waitTime = TimeSpan.FromMilliseconds(250);
            DateTime startTime = DateTime.Now;

            Task t = fiber.Schedule(() =>
            {
                state = true;
            }, waitTime);

            t.Wait();

            Assert.IsTrue(DateTime.Now - startTime > waitTime);
            Assert.IsTrue(state.Value);
        }
    }
}
