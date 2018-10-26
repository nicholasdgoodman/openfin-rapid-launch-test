namespace RapidLaunch.Common
{
    /// <summary>
    /// Interface for Interapplication bus wrapper
    /// </summary>
    public interface IMessageBus
    {
        void Publish<T>(string topic, T message);
    }
}
