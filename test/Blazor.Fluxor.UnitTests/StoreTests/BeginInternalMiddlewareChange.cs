using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
	public partial class StoreTests
	{
		public class BeginInternalMiddlewareChange
		{
			TestStoreInitializer StoreInitializer;

			[Fact]
			public async Task ExecutesOnAllRegisteredMiddlewares()
			{
				int disposeCount = 0;
				var mockMiddleware = new Mock<IMiddleware>();
				mockMiddleware
					.Setup(x => x.BeginInternalMiddlewareChange())
					.Returns(new DisposableCallback(() => disposeCount++));

				var subject = await Store.Initialize(StoreInitializer);
				await subject.AddMiddleware(mockMiddleware.Object);

				var disposable1 = await subject.BeginInternalMiddlewareChange();
				var disposable2 = await subject.BeginInternalMiddlewareChange();

				await disposable1.DisposeAsync();
				Assert.Equal(0, disposeCount);

				await disposable2.DisposeAsync();
				Assert.Equal(1, disposeCount);
			}

			public BeginInternalMiddlewareChange()
			{
				StoreInitializer = new TestStoreInitializer();
			}
		}
	}
}
