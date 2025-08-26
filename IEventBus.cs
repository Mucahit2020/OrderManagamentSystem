using OrderManagement.Common.Events;

namespace OrderManagement.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        where T : BaseEvent;
}