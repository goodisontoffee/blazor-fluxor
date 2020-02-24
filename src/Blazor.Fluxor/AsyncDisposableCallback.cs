using System;
using System.Threading.Tasks;

namespace Blazor.Fluxor
{
	/// <summary>
	/// This class can be used to execute a custom piece of code when IDisposable is
	/// called.
	/// </summary>
	/// <seealso cref="IStore.BeginInternalMiddlewareChange()"/>
	public sealed class AsyncDisposableCallback : IAsyncDisposable
	{
		private readonly Func<Task> Function;
		private bool IsDisposed;

		/// <summary>
		/// Creates an instance of the class
		/// </summary>
		/// <param name="funtion">The function to execute when the instance is disposed</param>
		public AsyncDisposableCallback(Func<Task> function)
		{
			Function = function ?? throw new ArgumentNullException(nameof(function));
		}

		/// <summary>
		/// Executes the action when disposed
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(AsyncDisposableCallback));

			IsDisposed = true;
			GC.SuppressFinalize(this);
			await Function();
		}

		/// <summary>
		/// Throws an exception if this object is collected without being disposed
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if the object is collected without being disposed</exception>
		~AsyncDisposableCallback()
		{
			if (!IsDisposed)
				throw new InvalidOperationException("AsyncDisposableCallback was not disposed");
		}
	}
}