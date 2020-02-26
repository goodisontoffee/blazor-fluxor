﻿using System;

namespace Blazor.Fluxor
{
	using System.Threading.Tasks;

	/// <summary>
	/// A strategy pattern for initialising a store
	/// </summary>
	public interface IStoreInitializationStrategy
	{
		/// <summary>
		/// Initialises the store
		/// </summary>
		/// <param name="completed">The function to call back once the store is ready to be initialised</param>
		void Initialize(Func<Task> completed);
	}
}
