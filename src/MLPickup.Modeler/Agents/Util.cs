
namespace MLPickup.Modeler.Agents
{
    using System;
    using System.Threading.Tasks;
    using MLPickup.Common.Concurrency;
    using MLPickup.Common.Internal.Logging;
    using TaskCompletionSource = MLPickup.Common.Concurrency.TaskCompletionSource;

    static class Util
    {
        static readonly IInternalLogger Log = InternalLoggerFactory.GetInstance<IAgent>();

        /// <summary>
        /// Marks the specified <see cref="TaskCompletionSource"/> as success. If the
        /// <see cref="TaskCompletionSource"/> is done already, logs a message.
        /// </summary>
        /// <param name="promise">The <see cref="TaskCompletionSource"/> to complete.</param>
        /// <param name="logger">The <see cref="IInternalLogger"/> to use to log a failure message.</param>
        public static void SafeSetSuccess(TaskCompletionSource promise, IInternalLogger logger)
        {
            if (promise != TaskCompletionSource.Void && !promise.TryComplete())
            {
                logger.Warn($"Failed to mark a promise as success because it is done already: {promise}");
            }
        }

        /// <summary>
        /// Marks the specified <see cref="TaskCompletionSource"/> as failure. If the
        /// <see cref="TaskCompletionSource"/> is done already, log a message.
        /// </summary>
        /// <param name="promise">The <see cref="TaskCompletionSource"/> to complete.</param>
        /// <param name="cause">The <see cref="Exception"/> to fail the <see cref="TaskCompletionSource"/> with.</param>
        /// <param name="logger">The <see cref="IInternalLogger"/> to use to log a failure message.</param>
        public static void SafeSetFailure(TaskCompletionSource promise, Exception cause, IInternalLogger logger)
        {
            if (promise != TaskCompletionSource.Void && !promise.TrySetException(cause))
            {
                logger.Warn($"Failed to mark a promise as failure because it's done already: {promise}", cause);
            }
        }

        public static void CloseSafe(this IAgent Agent)
        {
            CompleteAgentCloseTaskSafely(Agent, Agent.CloseAsync());
        }

        public static void CloseSafe(this IAgentUnsafe u)
        {
            CompleteAgentCloseTaskSafely(u, u.CloseAsync());
        }

        internal static async void CompleteAgentCloseTaskSafely(object AgentObject, Task closeTask)
        {
            try
            {
                await closeTask;
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (Log.DebugEnabled)
                {
                    Log.Debug("Failed to close Agent " + AgentObject + " cleanly.", ex);
                }
            }
        }
    }
}
