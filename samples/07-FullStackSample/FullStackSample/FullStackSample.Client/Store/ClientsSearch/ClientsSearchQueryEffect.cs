﻿using Blazor.Fluxor;
using FullStackSample.Client.Services;
using FullStackSample.Client.Store.EntityStateEvents;
using FullStackSample.Client.Store.Main;
using System.Linq;
using System.Threading.Tasks;

namespace FullStackSample.Client.Store.ClientsSearch
{
	public class ClientsSearchQueryEffect : Effect<Api.Requests.ClientsSearchQuery>
	{
		private readonly IApiService ApiService;

		public ClientsSearchQueryEffect(IApiService apiService)
		{
			ApiService = apiService;
		}

		protected async override Task HandleAsync(Api.Requests.ClientsSearchQuery query, IDispatcher dispatcher)
		{
			try
			{
				var response = await ApiService.Execute<Api.Requests.ClientsSearchQuery, Api.Requests.ClientsSearchResponse>(query);
				dispatcher.Dispatch(response);

				var notifications = response.Clients.Select(x =>
					new ClientStateChanges(
						stateUpdateKind: StateUpdateKind.Exists,
						id: x.Id,
						name: x.Name,
						registrationNumber: PropertyUpdate<int>.NotSet));
				dispatcher.Dispatch(new ClientStatesChangedNotification(notifications));
			}
			catch
			{
				dispatcher.Dispatch(new NotifyUnexpectedServerErrorStatusChanged(true));
				dispatcher.Dispatch(new Api.Requests.ClientsSearchResponse());
			}
		}
	}
}
