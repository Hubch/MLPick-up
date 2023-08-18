
namespace MLPickup.Modeler.Agents
{
    using MLPickup.Common.Concurrency;

    /// <summary>
    /// <see cref="IEventExecutor"/> specialized to handle I/O operations of assigned <see cref="IChannel"/>s.
    /// </summary>
    public interface IEventLoop : IEventLoopGroup, IEventExecutor
    {
        /// <summary>
        /// Parent <see cref="IEventLoopGroup"/>.
        /// </summary>
        new IEventLoopGroup Parent { get; }
    }
}
