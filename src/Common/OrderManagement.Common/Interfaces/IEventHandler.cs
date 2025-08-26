using OrderManagement.Common.Events;

namespace OrderManagement.Common.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}