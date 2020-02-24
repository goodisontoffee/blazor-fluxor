using Blazor.Fluxor.UnitTests.MockFactories;
using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
	public partial class StoreTests
	{
		public class Dispatch
		{
			TestStoreInitializer StoreInitializer;

			[Fact]
			public async Task ThrowsArgumentNullException_WhenActionIsNull()
			{
				var subject = await Store.Initialize(StoreInitializer);
				await Assert.ThrowsAsync<ArgumentNullException>(async () => await subject.Dispatch(null));
			}

			[Fact]
			public async Task DoesNotDispatchActions_WhenIsInsideMiddlewareChange()
			{
				var mockMiddleware = MockMiddlewareFactory.Create();

				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();

				var testAction = new TestAction();
                await using (await subject.BeginInternalMiddlewareChange())
				{
					await subject.Dispatch(testAction);
				}

				mockMiddleware.Verify(x => x.MayDispatchAction(testAction), Times.Never);
			}

			[Fact]
			public async Task DoesNotSendActionToFeatures_WhenMiddlewareForbidsIt()
			{
				var testAction = new TestAction();
				var mockFeature = MockFeatureFactory.Create();
				var mockMiddleware = MockMiddlewareFactory.Create();
				mockMiddleware
					.Setup(x => x.MayDispatchAction(testAction))
					.Returns(false);
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();

				await StoreInitializer.Complete();
				await subject.Dispatch(testAction);

				mockFeature
					.Verify(x => x.ReceiveDispatchNotificationFromStore(testAction), Times.Never);
			}

			[Fact]
			public async Task ExecutesBeforeDispatchActionOnMiddlewares()
			{
				var testAction = new TestAction();
				var mockMiddleware = MockMiddlewareFactory.Create();
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();
				await subject.Dispatch(testAction);

				mockMiddleware
					.Verify(x => x.BeforeDispatch(testAction), Times.Once);
			}

			[Fact]
			public async Task NotifiesFeatures()
			{
				var mockFeature = MockFeatureFactory.Create();
				var subject = await Store.Initialize(StoreInitializer);
				await subject.AddFeature(mockFeature.Object);
				subject.Initialize();

				var testAction = new TestAction();
				await StoreInitializer.Complete();
				await subject.Dispatch(testAction);

				mockFeature
					.Verify(x => x.ReceiveDispatchNotificationFromStore(testAction));
			}

			[Fact]
			public async Task DispatchesTasksFromEffect()
			{
				var mockFeature = MockFeatureFactory.Create();
				var actionToEmit1 = new TestActionFromEffect1();
				var actionToEmit2 = new TestActionFromEffect2();
				var actionsToEmit = new object[] { actionToEmit1, actionToEmit2 };
				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await subject.AddFeature(mockFeature.Object);
				await subject.AddEffect(new EffectThatEmitsActions<TestAction>(actionsToEmit));

				await StoreInitializer.Complete();
				await subject.Dispatch(new TestAction());

				mockFeature
					.Verify(x => x.ReceiveDispatchNotificationFromStore(actionToEmit1), Times.Once);
				mockFeature
					.Verify(x => x.ReceiveDispatchNotificationFromStore(actionToEmit2), Times.Once);
			}

			[Fact]
			public async Task TriggersOnlyEffectsThatHandleTheDispatchedAction()
			{
				var mockIncompatibleEffect = new Mock<IEffect>();
				mockIncompatibleEffect
					.Setup(x => x.ShouldReactToAction(It.IsAny<object>()))
					.Returns(false);
				var mockCompatibleEffect = new Mock<IEffect>();
				mockCompatibleEffect
					.Setup(x => x.ShouldReactToAction(It.IsAny<object>()))
					.Returns(true);

				var subject = await Store.Initialize(StoreInitializer);
				subject.Initialize();
				await subject.AddEffect(mockIncompatibleEffect.Object);
				await subject.AddEffect(mockCompatibleEffect.Object);
				await StoreInitializer.Complete();

				var action = new TestAction();
                await subject.Dispatch(action);

				mockIncompatibleEffect.Verify(x => x.HandleAsync(action, It.IsAny<IDispatcher>()), Times.Never);
				mockCompatibleEffect.Verify(x => x.HandleAsync(action, It.IsAny<IDispatcher>()), Times.Once);
			}

			public Dispatch()
			{
				StoreInitializer = new TestStoreInitializer();
			}
		}
    }
}
