using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazor.Fluxor
{
	/// <see cref="IStore"/>
	public class Store : IStore
	{
		/// <see cref="IStore.Features"/>
		public IReadOnlyDictionary<string, IFeature> Features => FeaturesByName;
		/// <see cref="IStore.Initialized"/>
		public Task Initialized => InitializedCompletionSource.Task;

		private readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
		private readonly IStoreInitializationStrategy StoreInitializationStrategy;
		private readonly Dictionary<string, IFeature> FeaturesByName = new Dictionary<string, IFeature>(StringComparer.InvariantCultureIgnoreCase);
		private readonly List<IEffect> Effects = new List<IEffect>();
		private readonly List<IMiddleware> Middlewares = new List<IMiddleware>();
		private readonly List<IMiddleware> ReversedMiddlewares = new List<IMiddleware>();
		private readonly Queue<object> QueuedActions = new Queue<object>();
		private readonly TaskCompletionSource<bool> InitializedCompletionSource = new TaskCompletionSource<bool>();

		private volatile bool IsDispatching;
		private volatile int BeginMiddlewareChangeCount;
		private volatile bool HasActivatedStore;
		private bool IsInsideMiddlewareChange => BeginMiddlewareChangeCount > 0;
		private Action<IFeature, object> IFeatureReceiveDispatchNotificationFromStore;

		/// <summary>
		/// Applies the supplied store initialization strategy to initialize the store then dispatches the StoreInitializedAction before returning the store.
		/// </summary>
		/// <param name="storeInitializationStrategy">The instance of <see cref="IStoreInitializationStrategy"/> to apply.</param>
		/// <returns>The initialized store</returns>
		public static async Task<Store> Initialize(IStoreInitializationStrategy storeInitializationStrategy)
		{
			var store = new Store(storeInitializationStrategy);
			await store.Dispatch(new StoreInitializedAction());
			return store;
		}

		/// <summary>
		/// Creates an instance of the store
		/// </summary>
		/// <param name="storeInitializationStrategy">The strategy used to initialise the store</param>
		private Store(IStoreInitializationStrategy storeInitializationStrategy)
		{
			StoreInitializationStrategy = storeInitializationStrategy;

			MethodInfo dispatchNotifictionFromStoreMethodInfo =
				typeof(IFeature)
				.GetMethod(nameof(IFeature.ReceiveDispatchNotificationFromStore));
			IFeatureReceiveDispatchNotificationFromStore = (Action<IFeature, object>)
				Delegate.CreateDelegate(typeof(Action<IFeature, object>), dispatchNotifictionFromStoreMethodInfo);
		}

		/// <see cref="IStore.AddFeature(IFeature)"/>
		public async Task AddFeature(IFeature feature)
		{
			if (feature == null)
				throw new ArgumentNullException(nameof(feature));

			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				FeaturesByName.Add(feature.GetName(), feature);
			}
			finally
			{
				mutex.Release();
			}
		}

		/// <see cref="IDispatcher.Dispatch(object)"/>
		public async Task Dispatch(object action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				// Do not allow task dispatching inside a middleware-change.
				// These change cycles are for things like "jump to state" in Redux Dev Tools
				// and should be short lived.
				// We avoid dispatching inside a middleware change because we don't want UI events (like component Init)
				// that trigger actions (such as fetching data from a server) to execute
				if (IsInsideMiddlewareChange)
					return;

				// If a dequeue is already in progress, we will just
				// let this new action be added to the queue and then exit
				// Note: This is to cater for the following scenario
				//	1: An action is dispatched
				//	2: An effect is triggered
				//	3: The effect immediately dispatches a new action
				// The Queue ensures it is processed after its triggering action has completed rather than immediately
				QueuedActions.Enqueue(action);

				// HasActivatedStore is set to true when the page finishes loading
				// At which point DequeueActions will be called
				if (!HasActivatedStore)
					return;

				DequeueActions();
			}
			finally
			{
				mutex.Release();
			}
		}

		/// <see cref="IStore.AddEffect(IEffect)"/>
		public async Task AddEffect(IEffect effect)
		{
			if (effect == null)
				throw new ArgumentNullException(nameof(effect));

			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				Effects.Add(effect);
			}
			finally
			{
				mutex.Release();
			}
		}

		/// <see cref="IStore.AddMiddleware(IMiddleware)"/>
		public async Task AddMiddleware(IMiddleware middleware)
		{
			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				Middlewares.Add(middleware);
				ReversedMiddlewares.Insert(0, middleware);
				// Initialize the middleware immediately if the store has already been initialized, otherwise this will be
				// done the first time Dispatch is called
				if (HasActivatedStore)
				{
					middleware.InitializeAsync(this).Wait();
					middleware.AfterInitializeAllMiddlewares();
				}
			}
			finally
			{
				mutex.Release();
			}
		}

		/// <see cref="IStore.BeginInternalMiddlewareChange"/>
		public async Task<IAsyncDisposable> BeginInternalMiddlewareChange()
		{
			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				BeginMiddlewareChangeCount++;
				IDisposable[] disposables = Middlewares
					.Select(x => x.BeginInternalMiddlewareChange())
					.ToArray();

				return new AsyncDisposableCallback(async () => await EndMiddlewareChange(disposables));
			}
			finally
			{
				mutex.Release();
			}
		}

		/// <see cref="IStore.Initialize"/>
		public RenderFragment Initialize()
		{
			if (HasActivatedStore)
				return builder => { };

			StoreInitializationStrategy.Initialize(ActivateStore);
			return (RenderTreeBuilder renderer) =>
			{
				var scriptBuilder = new StringBuilder();
				scriptBuilder.AppendLine("if (window.canInitializeFluxor) {");
				{
					scriptBuilder.AppendLine("delete window.canInitializeFluxor;");
					foreach (IMiddleware middleware in Middlewares)
					{
						string middlewareScript = middleware.GetClientScripts();
						if (middlewareScript != null)
						{
							scriptBuilder.AppendLine($"// Middleware scripts: {middleware.GetType().FullName}");
							scriptBuilder.AppendLine($"{middlewareScript}");
						}
					}
				}
				scriptBuilder.AppendLine("}");

				string script = scriptBuilder.ToString();
				renderer.OpenElement(1, "script");
				renderer.AddAttribute(2, "id", "initializeFluxor");
				renderer.AddMarkupContent(3, script);
				renderer.CloseElement();
			};
		}

		private async Task EndMiddlewareChange(IDisposable[] disposables)
		{
			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				BeginMiddlewareChangeCount--;
				if (BeginMiddlewareChangeCount == 0)
					disposables.ToList().ForEach(x => x.Dispose());
			}
			finally
			{
				mutex.Release();
			}
		}

		private void TriggerEffects(object action)
		{
			var effectsToTrigger = Effects.Where(x => x.ShouldReactToAction(action));
			foreach (var effect in effectsToTrigger)
			{
				effect.HandleAsync(action, this);
			}
		}

		private async Task InitializeMiddlewares()
		{
			foreach (IMiddleware middleware in Middlewares)
			{
				await middleware.InitializeAsync(this);
			}
			Middlewares.ForEach(x => x.AfterInitializeAllMiddlewares());
		}

		private void ExecuteMiddlewareBeforeDispatch(object actionAboutToBeDispatched)
		{
			foreach (IMiddleware middleWare in Middlewares)
				middleWare.BeforeDispatch(actionAboutToBeDispatched);
		}

		private void ExecuteMiddlewareAfterDispatch(object actionJustDispatched)
		{
			Middlewares.ForEach(x => x.AfterDispatch(actionJustDispatched));
		}

		private async Task ActivateStore()
		{
			if (HasActivatedStore)
				return;

			await mutex.WaitAsync().ConfigureAwait(false);

			try
			{
				HasActivatedStore = true;
				await InitializeMiddlewares();
				DequeueActions();
				InitializedCompletionSource.SetResult(true);
			}
			finally
			{
				mutex.Release();
			}
		}

		private void DequeueActions()
		{
			if (IsDispatching)
				return;

			IsDispatching = true;
			try
			{
				while (QueuedActions.TryDequeue(out object nextActionToProcess))
				{
					// Only process the action if no middleware vetos it
					if (Middlewares.All(x => x.MayDispatchAction(nextActionToProcess)))
					{
						ExecuteMiddlewareBeforeDispatch(nextActionToProcess);

						// Notify all features of this action
						foreach (var featureInstance in FeaturesByName.Values)
							IFeatureReceiveDispatchNotificationFromStore(featureInstance, nextActionToProcess);

						ExecuteMiddlewareAfterDispatch(nextActionToProcess);

						TriggerEffects(nextActionToProcess);
					}
				}
			}
			finally
			{
				IsDispatching = false;
			}
		}
	}
}
