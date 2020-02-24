using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
	public partial class StoreTests
	{
		public class AddMiddleware
		{
			TestStoreInitializer StoreInitializer;

			[Fact]
			public async Task ActivatesMiddleware_WhenPageHasAlreadyLoaded()
			{
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await StoreInitializer.Complete();

				var mockMiddleware = new Mock<IMiddleware>();
				await subject.AddMiddleware(mockMiddleware.Object);

				mockMiddleware
					.Verify(x => x.InitializeAsync(subject));
			}

			[Fact]
			public async Task CallsAfterInitializeAllMiddlewares_WhenPageHasAlreadyLoaded()
			{
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await StoreInitializer.Complete();

				var mockMiddleware = new Mock<IMiddleware>();
				await subject.AddMiddleware(mockMiddleware.Object);

				mockMiddleware
					.Verify(x => x.AfterInitializeAllMiddlewares());
			}

			public AddMiddleware()
			{
				StoreInitializer = new TestStoreInitializer();
			}
		}
	}
}
