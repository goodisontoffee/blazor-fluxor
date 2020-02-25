using Blazor.Fluxor.UnitTests.StoreTests.ThreadingTests.CounterStore;
using Blazor.Fluxor.UnitTests.SupportFiles;
using System.Collections.Generic;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests.ThreadingTests
{
    using System.Threading.Tasks;

    public class Dispatch : IAsyncLifetime
	{
		const int NumberOfTasks = 10;
		const int NumberOfIncrementsPerTask = 1000;

		IStore Store;
		IFeature<CounterState> Feature;

		[Fact]
		public async Task DoesNotLoseState()
		{
			var tasks = new List<Task>();
			for (int i = 0; i < NumberOfTasks; i++)
            {
                tasks.Add(IncrementCounterInTask());
			}

            await Task.WhenAll(tasks);

			Assert.Equal(NumberOfTasks * NumberOfIncrementsPerTask, Feature.State.Counter);
		}

		private async Task IncrementCounterInTask()
		{
			for (int i = 0; i < NumberOfIncrementsPerTask; i++)
			{
				await Store.Dispatch(new IncrementCounterAction());
			}
		}

        public async Task InitializeAsync()
        {
            var storeInitializer = new TestStoreInitializer();
            Store = await Fluxor.Store.Initialize(storeInitializer);
            Store.Initialize();

            Feature = new CounterFeature();
            await Store.AddFeature(Feature);

            Feature.AddReducer(new IncrementCounterReducer());
            await storeInitializer.Complete();
		}

		public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
