﻿using Blazor.Fluxor;

namespace ReduxDevToolsIntegration.Client.Store.FetchData.GetForecastData
{
	public class GetForecastDataFailedActionReducer : Reducer<FetchDataState, GetForecastDataFailedAction>
	{
		public override FetchDataState Reduce(FetchDataState state, GetForecastDataFailedAction action)
		{
			return new FetchDataState(
				isLoading: false,
				errorMessage: action.ErrorMessage,
				forecasts: null);
		}
	}
}
