namespace Astrolabed.Events;

public interface IEventConsumer
{
    void Consume(EventRecord evt);
}
