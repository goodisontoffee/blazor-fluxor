using Blazor.Fluxor.UnitTests.StoreTests.ThreadingTests.CounterStore;
using Blazor.Fluxor.UnitTests.SupportFiles;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests.ThreadingTests
{
    using System.Threading.Tasks;

    public class Dispatch : IAsyncLifetime
	{
		const int NumberOfThreads = 10;
		const int NumberOfIncrementsPerThread = 1000;
		volatile int NumberOfThreadsWaitingToStart = NumberOfThreads;

		IStore Store;
		IFeature<CounterState> Feature;
		ManualResetEvent StartEvent;

		[Fact]
		public void DoesNotLoseState()
		{
			var threads = new List<Thread>();
			for (int i = 0; i < NumberOfThreads; i++)
			{
				var thread = new Thread(IncrementCounterInThread);
				thread.Start();
				threads.Add(thread);
			}
			while (NumberOfThreadsWaitingToStart > 0)
				Thread.Sleep(50);

			StartEvent.Set();
			foreach (Thread thread in threads)
				thread.Join();

			Assert.Equal(NumberOfThreads * NumberOfIncrementsPerThread, Feature.State.Counter);
		}

		private void IncrementCounterInThread()
		{
			Interlocked.Decrement(ref NumberOfThreadsWaitingToStart);
			StartEvent.WaitOne();
			var action = new IncrementCounterAction();
			for (int i = 0; i < NumberOfIncrementsPerThread; i++)
			{
				Store.Dispatch(action);
			}
		}

        public async Task InitializeAsync()
        {
			StartEvent = new ManualResetEvent(false);
            var storeInitializer = new TestStoreInitializer();
            Store = new Store(storeInitializer);
            Store.Initialize();

            Feature = new CounterFeature();
            Store.AddFeature(Feature);

            Feature.AddReducer(new IncrementCounterReducer());
            await storeInitializer.Complete();
		}

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
