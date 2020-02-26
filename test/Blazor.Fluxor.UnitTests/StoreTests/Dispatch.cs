using Blazor.Fluxor.UnitTests.MockFactories;
using Blazor.Fluxor.UnitTests.SupportFiles;
using Moq;
using System;
using Xunit;

namespace Blazor.Fluxor.UnitTests.StoreTests
{
    using System.Threading.Tasks;

    public partial class StoreTests
	{
		public class Dispatch
		{
			TestStoreInitializer StoreInitializer;

			[Fact]
			public void ThrowsArgumentNullException_WhenActionIsNull()
			{
				var subject = new Store(StoreInitializer);
				Assert.Throws<ArgumentNullException>(() => subject.Dispatch(null));
			}

			[Fact]
			public async Task DoesNotDispatchActions_WhenIsInsideMiddlewareChange()
			{
				var mockMiddleware = MockMiddlewareFactory.Create();

				var subject = new Store(StoreInitializer);
				subject.Initialize();
				subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();

				var testAction = new TestAction();
				using (subject.BeginInternalMiddlewareChange())
				{
					subject.Dispatch(testAction);
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
				var subject = new Store(StoreInitializer);
				subject.Initialize();

				await StoreInitializer.Complete();
				subject.Dispatch(testAction);

				mockFeature
					.Verify(x => x.ReceiveDispatchNotificationFromStore(testAction), Times.Never);
			}

			[Fact]
			public async Task ExecutesBeforeDispatchActionOnMiddlewares()
			{
				var testAction = new TestAction();
				var mockMiddleware = MockMiddlewareFactory.Create();
				var subject = new Store(StoreInitializer);
				subject.Initialize();
				subject.AddMiddleware(mockMiddleware.Object);

				await StoreInitializer.Complete();
				subject.Dispatch(testAction);

				mockMiddleware
					.Verify(x => x.BeforeDispatch(testAction), Times.Once);
			}

			[Fact]
			public async Task NotifiesFeatures()
			{
				var mockFeature = MockFeatureFactory.Create();
				var subject = new Store(StoreInitializer);
				subject.AddFeature(mockFeature.Object);
				subject.Initialize();

				var testAction = new TestAction();
				await StoreInitializer.Complete();
				subject.Dispatch(testAction);

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
				var subject = new Store(StoreInitializer);
				subject.Initialize();
				subject.AddFeature(mockFeature.Object);
				subject.AddEffect(new EffectThatEmitsActions<TestAction>(actionsToEmit));

				await StoreInitializer.Complete();
				subject.Dispatch(new TestAction());

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

				var subject = new Store(StoreInitializer);
				subject.Initialize();
				subject.AddEffect(mockIncompatibleEffect.Object);
				subject.AddEffect(mockCompatibleEffect.Object);
				await StoreInitializer.Complete();

				var action = new TestAction();
				subject.Dispatch(action);

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
