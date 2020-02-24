using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Threading.Tasks;

namespace Blazor.Fluxor.Routing
{
	/// <summary>
	/// Adds support for routing <see cref="Microsoft.AspNetCore.Components.NavigationManager"/>
	/// via a Fluxor store.
	/// </summary>
	public class RoutingMiddleware : Middleware
	{
		private readonly NavigationManager NavigationManager;
		private readonly IFeature<RoutingState> Feature;

		/// <summary>
		/// Creates a new instance of the routing middleware
		/// </summary>
		/// <param name="navigationManager">Uri helper</param>
		/// <param name="feature">The routing feature</param>
		public RoutingMiddleware(NavigationManager navigationManager, IFeature<RoutingState> feature)
		{
			NavigationManager = navigationManager;
			Feature = feature;
			NavigationManager.LocationChanged += LocationChanged;
		}


		/// <see cref="IMiddleware.InitializeAsync(IStore)"/>
		public override async Task InitializeAsync(IStore store)
		{
			await base.InitializeAsync(store);
			// If the URL changed before we initialized then dispatch an action
			await Store.Dispatch(new Go(NavigationManager.Uri));
		}

		/// <see cref="Middleware.OnInternalMiddlewareChangeEnding"/>
		protected override void OnInternalMiddlewareChangeEnding()
		{
			if (Feature.State.Uri != NavigationManager.Uri)
				NavigationManager.NavigateTo(Feature.State.Uri);
		}

		private void LocationChanged(object sender, LocationChangedEventArgs e)
		{
			string fullUri = NavigationManager.ToAbsoluteUri(e.Location).ToString();
			if (Store != null && !IsInsideMiddlewareChange && fullUri != Feature.State.Uri)
				Store.Dispatch(new Go(e.Location));
		}
	}
}
