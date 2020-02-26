using System;

namespace Blazor.Fluxor.UnitTests.SupportFiles
{
    using System.Threading.Tasks;

    public class TestStoreInitializer : IStoreInitializationStrategy
	{
		Func<Task> Completed;

		public void Initialize(Func<Task> completed)
		{
			Completed = completed;
		}

		public async Task Complete() => await Completed();
	}
}
