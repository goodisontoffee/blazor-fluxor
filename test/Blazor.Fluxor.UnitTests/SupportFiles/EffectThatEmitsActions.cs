using System;
using System.Threading.Tasks;

namespace Blazor.Fluxor.UnitTests.SupportFiles
{
    using System.Collections.Generic;

    public class EffectThatEmitsActions<TTriggerAction> : Effect<TTriggerAction>
	{
		public readonly object[] ActionsToEmit;

		public EffectThatEmitsActions(object[] actionsToEmit)
		{
			ActionsToEmit = actionsToEmit ?? Array.Empty<object>();
		}

		protected override async Task HandleAsync(TTriggerAction action, IDispatcher dispatcher)
        {
            ICollection<Task> tasks = new List<Task>();

            foreach (object actionToEmit in ActionsToEmit)
            {
                tasks.Add(dispatcher.Dispatch(actionToEmit));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                await task;
            }
		}
	}
}
