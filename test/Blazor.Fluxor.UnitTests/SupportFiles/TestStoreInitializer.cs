using System;
using System.Threading.Tasks;

namespace Blazor.Fluxor.UnitTests.SupportFiles
{
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
