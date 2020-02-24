using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
	public partial class StoreTests
	{
		public class Initialize
		{
			TestStoreInitializer StoreInitializer;

			[Fact]
			public async Task ActivatesMiddleware_WhenStoreInitializerCompletes()
			{
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				var mockMiddleware = new Mock<IMiddleware>();
				await subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();

				mockMiddleware
					.Verify(x => x.InitializeAsync(subject));
			}

			[Fact]
			public async Task CallsAfterInitializeAllMiddlewares_WhenStoreInitializerCompletes()
			{
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				var mockMiddleware = new Mock<IMiddleware>();
				await subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();

				mockMiddleware
					.Verify(x => x.AfterInitializeAllMiddlewares());
			}

			public Initialize()
			{
				StoreInitializer = new TestStoreInitializer();
			}
		}
	}
}