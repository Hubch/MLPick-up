
namespace MLPickup.Modeler.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using MLPickup.Common.Concurrency;


    /// <summary>
    /// A list of <see cref="IAgentHandler"/>s which handles or intercepts inbound events and outbound operations of
    /// a <see cref="IAgent"/>. <see cref="IAgentPipeline"/> implements an advanced form of the
    /// <a href="http://www.oracle.com/technetwork/java/interceptingfilter-142169.html">Intercepting Filter</a> pattern
    /// to give a user full control over how an event is handled and how the <see cref="IAgentHandler"/>s in a
    /// pipeline interact with each other.
    /// <para>Creation of a pipeline</para>
    /// <para>Each Agent has its own pipeline and it is created automatically when a new Agent is created.</para>
    /// <para>How an event flows in a pipeline</para>
    /// <para>
    /// The following diagram describes how I/O events are processed by <see cref="IAgentHandler"/>s in a
    /// <see cref="IAgentPipeline"/> typically. An I/O event is handled by a <see cref="IAgentHandler"/> and is
    /// forwarded by the <see cref="IAgentHandler"/> which handled the event to the <see cref="IAgentHandler"/>
    /// which is placed right next to it. A <see cref="IAgentHandler"/> can also trigger an arbitrary I/O event if
    /// necessary. To forward or trigger an event, a <see cref="IAgentHandler"/> calls the event propagation methods
    /// defined in <see cref="IAgentHandlerContext"/>, such as <see cref="IAgentHandlerContext.FireAgentRead"/>
    /// and <see cref="IAgentHandlerContext.WriteAsync"/>.
    /// </para>
    /// <para>
    ///     <pre>
    ///         I/O Request
    ///         via <see cref="IAgent"/> or
    ///         {@link AgentHandlerContext} 
    ///         |
    ///         +---------------------------------------------------+---------------+
    ///         |                           AgentPipeline         |               |
    ///         |                                                  \|/              |
    ///         |    +----------------------------------------------+----------+    |
    ///         |    |                   AgentHandler  N                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         |               |                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   AgentHandler N-1                    |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  .               |
    ///         |               .                                   .               |
    ///         | AgentHandlerContext.fireIN_EVT() AgentHandlerContext.OUT_EVT()|
    ///         |          [method call]                      [method call]         |
    ///         |               .                                   .               |
    ///         |               .                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   AgentHandler  2                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         |               |                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   AgentHandler  1                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         +---------------+-----------------------------------+---------------+
    ///         |                                  \|/
    ///         +---------------+-----------------------------------+---------------+
    ///         |               |                                   |               |
    ///         |       [ Socket.read() ]                    [ Socket.write() ]     |
    ///         |                                                                   |
    ///         |  Netty Internal I/O Threads (Transport Implementation)            |
    ///         +-------------------------------------------------------------------+
    ///     </pre>
    /// </para>
    /// <para>
    /// An inbound event is handled by the <see cref="IAgentHandler"/>s in the bottom-up direction as shown on the
    /// left side of the diagram. An inbound event is usually triggered by the I/O thread on the bottom of the diagram
    /// so that the <see cref="IAgentHandler"/>s are notified when the state of a <see cref="IAgent"/> changes
    /// (e.g. newly established connections and closed connections) or the inbound data was read from a remote peer. If
    /// an inbound event goes beyond the <see cref="IAgentHandler"/> at the top of the diagram, it is discarded and
    /// logged, depending on your loglevel.
    /// </para>
    /// <para>
    /// An outbound event is handled by the <see cref="IAgentHandler"/>s in the top-down direction as shown on the
    /// right side of the diagram. An outbound event is usually triggered by your code that requests an outbound I/O
    /// operation, such as a write request and a connection attempt.  If an outbound event goes beyond the
    /// <see cref="IAgentHandler"/> at the bottom of the diagram, it is handled by an I/O thread associated with the
    /// <see cref="IAgent"/>. The I/O thread often performs the actual output operation such as
    /// <see cref="AbstractAgent.WriteAsync"/>.
    /// </para>
    /// <para>Forwarding an event to the next handler</para>
    /// <para>
    /// As explained briefly above, a <see cref="IAgentHandler"/> has to invoke the event propagation methods in
    /// <see cref="IAgentHandlerContext"/> to forward an event to its next handler. Those methods include:
    ///     <ul>
    ///         <li>
    ///             Inbound event propagation methods:
    ///             <ul>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentRegistered"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentActive"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentRead"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentReadComplete"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireExceptionCaught"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireUserEventTriggered"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentWritabilityChanged"/></li>
    ///                 <li><see cref="IAgentHandlerContext.FireAgentInactive"/></li>
    ///             </ul>
    ///         </li>
    ///         <li>
    ///             Outbound event propagation methods:
    ///             <ul>
    ///                 <li><see cref="IAgentHandlerContext.BindAsync"/></li>
    ///                 <li><see cref="IAgentHandlerContext.ConnectAsync(EndPoint)"/></li>
    ///                 <li><see cref="IAgentHandlerContext.ConnectAsync(EndPoint, EndPoint)"/></li>
    ///                 <li><see cref="IAgentHandlerContext.WriteAsync"/></li>
    ///                 <li><see cref="IAgentHandlerContext.Flush"/></li>
    ///                 <li><see cref="IAgentHandlerContext.Read"/></li>
    ///                 <li><see cref="IAgentHandlerContext.DisconnectAsync"/></li>
    ///                 <li><see cref="IAgentHandlerContext.CloseAsync"/></li>
    ///             </ul>
    ///         </li>
    ///     </ul>
    /// </para>
    /// <para>
    ///     and the following example shows how the event propagation is usually done:
    ///     <code>
    ///         public class MyInboundHandler : <see cref="AgentHandlerAdapter"/>
    ///         {
    ///             public override void AgentActive(<see cref="IAgentHandlerContext"/> ctx)
    ///             {
    ///                 Console.WriteLine("Connected!");
    ///                 ctx.FireAgentActive();
    ///             }
    ///         }
    /// 
    ///         public class MyOutboundHandler : <see cref="AgentHandlerAdapter"/>
    ///         {
    ///             public override async Task CloseAsync(<see cref="IAgentHandlerContext"/> ctx)
    ///             {
    ///                 Console.WriteLine("Closing...");
    ///                 await ctx.CloseAsync();
    ///             }
    ///         }
    ///     </code>
    /// </para>
    /// <para>Building a pipeline</para>
    /// <para>
    /// A user is supposed to have one or more <see cref="IAgentHandler"/>s in a pipeline to receive I/O events
    /// (e.g. read) and to request I/O operations (e.g. write and close).  For example, a typical server will have the
    /// following handlers in each Agent's pipeline, but your mileage may vary depending on the complexity and
    /// characteristics of the protocol and business logic:
    ///     <ol>
    ///         <li>Protocol Decoder - translates binary data (e.g. <see cref="IByteBuffer"/>) into a Java object.</li>
    ///         <li>Protocol Encoder - translates a Java object into binary data.</li>
    ///         <li>Business Logic Handler - performs the actual business logic (e.g. database access).</li>
    ///     </ol>
    /// </para>
    /// <para>
    ///     and it could be represented as shown in the following example:
    ///     <code>
    ///         static readonly <see cref="IEventExecutorGroup"/> group = new <see cref="MultithreadEventLoopGroup"/>();
    ///         ...
    ///         <see cref="IAgentPipeline"/> pipeline = ch.Pipeline;
    ///         pipeline.AddLast("decoder", new MyProtocolDecoder());
    ///         pipeline.AddLast("encoder", new MyProtocolEncoder());
    /// 
    ///         // Tell the pipeline to run MyBusinessLogicHandler's event handler methods
    ///         // in a different thread than an I/O thread so that the I/O thread is not blocked by
    ///         // a time-consuming task.
    ///         // If your business logic is fully asynchronous or finished very quickly, you don't
    ///         // need to specify a group.
    ///         pipeline.AddLast(group, "handler", new MyBusinessLogicHandler());
    ///     </code>
    /// </para>
    /// <para>Thread safety</para>
    /// <para>
    /// An <see cref="IAgentHandler"/> can be added or removed at any time because an <see cref="IAgentPipeline"/>
    /// is thread safe. For example, you can insert an encryption handler when sensitive information is about to be
    /// exchanged, and remove it after the exchange.
    /// </para>
    /// </summary>
    public interface IAgentPipeline : IEnumerable<IAgentHandler>
    {
        /// <summary>
        /// Inserts an <see cref="IAgentHandler"/> at the first position of this pipeline.
        /// </summary>
        /// <param name="name">
        /// The name of the handler to insert first. Pass <c>null</c> to let the name auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to insert first.</param>
        /// <returns>The <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddFirst(string name, IAgentHandler handler);

        /// <summary>
        /// Inserts a <see cref="IAgentHandler"/> at the first position of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handler"/>'s event handler methods.
        /// </param>
        /// <param name="name">
        /// The name of the handler to insert first. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to insert first.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddFirst(IEventExecutorGroup group, string name, IAgentHandler handler);

        /// <summary>
        /// Appends an <see cref="IAgentHandler"/> at the last position of this pipeline.
        /// </summary>
        /// <param name="name">
        /// The name of the handler to append. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to append.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddLast(string name, IAgentHandler handler);

        /// <summary>
        /// Appends a <see cref="IAgentHandler"/> at the last position of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handler"/>'s event handler methods.
        /// </param>
        /// <param name="name">
        /// The name of the handler to append. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to append.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddLast(IEventExecutorGroup group, string name, IAgentHandler handler);

        /// <summary>
        /// Inserts a <see cref="IAgentHandler"/> before an existing handler of this pipeline.
        /// </summary>
        /// <param name="baseName">The name of the existing handler.</param>
        /// <param name="name">
        /// The name of the new handler being appended. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to append.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists, or if no match was found for the
        /// given <paramref name="baseName"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddBefore(string baseName, string name, IAgentHandler handler);

        /// <summary>
        /// Inserts a <see cref="IAgentHandler"/> before an existing handler of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handler"/>'s event handler methods.
        /// </param>
        /// <param name="baseName">The name of the existing handler.</param>
        /// <param name="name">
        /// The name of the new handler being appended. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The <see cref="IAgentHandler"/> to append.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists, or if no match was found for the
        /// given <paramref name="baseName"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddBefore(IEventExecutorGroup group, string baseName, string name, IAgentHandler handler);

        /// <summary>
        /// Inserts a <see cref="IAgentHandler"/> after an existing handler of this pipeline.
        /// </summary>
        /// <param name="baseName">The name of the existing handler.</param>
        /// <param name="name">
        /// The name of the new handler being appended. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The handler to insert after.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists, or if no match was found for the
        /// given <paramref name="baseName"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddAfter(string baseName, string name, IAgentHandler handler);

        /// <summary>
        /// Inserts a <see cref="IAgentHandler"/> after an existing handler of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handler"/>'s event handler methods.
        /// </param>
        /// <param name="baseName">The name of the existing handler.</param>
        /// <param name="name">
        /// The name of the new handler being appended. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="handler">The handler to insert after.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="name"/> already exists, or if no match was found for the
        /// given <paramref name="baseName"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if the specified handler is <c>null</c>.</exception>
        IAgentPipeline AddAfter(IEventExecutorGroup group, string baseName, string name, IAgentHandler handler);

        /// <summary>
        /// Inserts multiple <see cref="IAgentHandler"/>s at the first position of this pipeline.
        /// </summary>
        /// <param name="handlers">The <see cref="IAgentHandler"/>s to insert.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline AddFirst(params IAgentHandler[] handlers);

        /// <summary>
        /// Inserts multiple <see cref="IAgentHandler"/>s at the first position of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handlers"/>' event handler methods.
        /// </param>
        /// <param name="handlers">The <see cref="IAgentHandler"/>s to insert.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline AddFirst(IEventExecutorGroup group, params IAgentHandler[] handlers);

        /// <summary>
        /// Inserts multiple <see cref="IAgentHandler"/>s at the last position of this pipeline.
        /// </summary>
        /// <param name="handlers">The <see cref="IAgentHandler"/>s to insert.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline AddLast(params IAgentHandler[] handlers);

        /// <summary>
        /// Inserts multiple <see cref="IAgentHandler"/>s at the last position of this pipeline.
        /// </summary>
        /// <param name="group">
        /// The <see cref="IEventExecutorGroup"/> which invokes the <paramref name="handlers"/>' event handler methods.
        /// </param>
        /// <param name="handlers">The <see cref="IAgentHandler"/>s to insert.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline AddLast(IEventExecutorGroup group, params IAgentHandler[] handlers);

        /// <summary>
        /// Removes the specified <see cref="IAgentHandler"/> from this pipeline.
        /// </summary>
        /// <param name="handler">The <see cref="IAgentHandler"/> to remove.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified handler was not found.</exception>
        IAgentPipeline Remove(IAgentHandler handler);

        /// <summary>
        /// Removes the <see cref="IAgentHandler"/> with the specified name from this pipeline.
        /// </summary>
        /// <param name="name">The name under which the <see cref="IAgentHandler"/> was stored.</param>
        /// <returns>The removed <see cref="IAgentHandler"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if there's no such handler with the specified name in this pipeline.
        /// </exception>
        IAgentHandler Remove(string name);

        /// <summary>
        /// Removes the <see cref="IAgentHandler"/> of the specified type from this pipeline.
        /// </summary>
        /// <typeparam name="T">The type of handler to remove.</typeparam>
        /// <returns>The removed <see cref="IAgentHandler"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if there's no handler of the specified type in this pipeline.</exception>
        T Remove<T>() where T : class, IAgentHandler;

        /// <summary>
        /// Removes the first <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>The removed <see cref="IAgentHandler"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this pipeline is empty.</exception>
        IAgentHandler RemoveFirst();

        /// <summary>
        /// Removes the last <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>The removed <see cref="IAgentHandler"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this pipeline is empty.</exception>
        IAgentHandler RemoveLast();

        /// <summary>
        /// Replaces the specified <see cref="IAgentHandler"/> with a new handler in this pipeline.
        /// </summary>
        /// <param name="oldHandler">The <see cref="IAgentHandler"/> to be replaced.</param>
        /// <param name="newName">
        /// The name of the new handler being inserted. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="newHandler">The new <see cref="IAgentHandler"/> to be inserted.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="newName"/> already exists, or if the
        /// <paramref name="oldHandler"/> was not found.
        /// </exception>
        IAgentPipeline Replace(IAgentHandler oldHandler, string newName, IAgentHandler newHandler);

        /// <summary>
        /// Replaces the <see cref="IAgentHandler"/> of the specified name with a new handler in this pipeline.
        /// </summary>
        /// <param name="oldName">The name of the <see cref="IAgentHandler"/> to be replaced.</param>
        /// <param name="newName">
        /// The name of the new handler being inserted. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="newHandler">The new <see cref="IAgentHandler"/> to be inserted.</param>
        /// <returns>The <see cref="IAgentHandler"/> that was replaced.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="newName"/> already exists, or if no match was found for
        /// the given <paramref name="oldName"/>.
        /// </exception>
        IAgentHandler Replace(string oldName, string newName, IAgentHandler newHandler);

        /// <summary>
        /// Replaces the <see cref="IAgentHandler"/> of the specified type with a new handler in this pipeline.
        /// </summary>
        /// <typeparam name="T">The type of the handler to be removed.</typeparam>
        /// <param name="newName">
        /// The name of the new handler being inserted. Pass <c>null</c> to let the name be auto-generated.
        /// </param>
        /// <param name="newHandler">The new <see cref="IAgentHandler"/> to be inserted.</param>
        /// <returns>The <see cref="IAgentHandler"/> that was replaced.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if an entry with the same <paramref name="newName"/> already exists, or if no match was found for
        /// the given type.
        /// </exception>
        T Replace<T>(string newName, IAgentHandler newHandler) where T : class, IAgentHandler;

        /// <summary>
        /// Returns the first <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>The first handler in the pipeline, or <c>null</c> if the pipeline is empty.</returns>
        IAgentHandler First();

        /// <summary>
        /// Returns the context of the first <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>
        /// The context of the first handler in the pipeline, or <c>null</c> if the pipeline is empty.
        /// </returns>
        IAgentHandlerContext FirstContext();

        /// <summary>
        /// Returns the last <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>The last handler in the pipeline, or <c>null</c> if the pipeline is empty.</returns>
        IAgentHandler Last();

        /// <summary>
        /// Returns the context of the last <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <returns>
        /// The context of the last handler in the pipeline, or <c>null</c> if the pipeline is empty.
        /// </returns>
        IAgentHandlerContext LastContext();

        /// <summary>
        /// Returns the <see cref="IAgentHandler"/> with the specified name in this pipeline.
        /// </summary>
        /// <param name="name">The name of the desired <see cref="IAgentHandler"/>.</param>
        /// <returns>
        /// The handler with the specified name, or <c>null</c> if there's no such handler in this pipeline.
        /// </returns>
        IAgentHandler Get(string name);

        /// <summary>
        /// Returns the <see cref="IAgentHandler"/> of the specified type in this pipeline.
        /// </summary>
        /// <typeparam name="T">The type of handler to retrieve.</typeparam>
        /// <returns>
        /// The handler with the specified type, or <c>null</c> if there's no such handler in this pipeline.
        /// </returns>
        T Get<T>() where T : class, IAgentHandler;

        /// <summary>
        /// Returns the context object of the specified <see cref="IAgentHandler"/> in this pipeline.
        /// </summary>
        /// <param name="handler">The <see cref="IAgentHandler"/> whose context should be retrieved.</param>
        /// <returns>
        /// The context object of the specified handler, or <c>null</c> if there's no such handler in this pipeline.
        /// </returns>
        IAgentHandlerContext Context(IAgentHandler handler);

        /// <summary>
        /// Returns the context object of the <see cref="IAgentHandler"/> with the specified name in this pipeline.
        /// </summary>
        /// <param name="name">The name of the <see cref="IAgentHandler"/> whose context should be retrieved.</param>
        /// <returns>
        /// The context object of the handler with the specified name, or <c>null</c> if there's no such handler in
        /// this pipeline.
        /// </returns>
        IAgentHandlerContext Context(string name);

        /// <summary>
        /// Returns the context object of the <see cref="IAgentHandler"/> of the specified type in this pipeline.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IAgentHandler"/> whose context should be retrieved.</typeparam>
        /// <returns>
        /// The context object of the handler with the specified type, or <c>null</c> if there's no such handler in
        /// this pipeline.
        /// </returns>
        IAgentHandlerContext Context<T>() where T : class, IAgentHandler;

        /// <summary>
        /// Returns the <see cref="IAgent" /> that this pipeline is attached to.
        /// Returns <c>null</c> if this pipeline is not attached to any Agent yet.
        /// </summary>
        IAgent Agent { get; }

        /// <summary>
        /// An <see cref="IAgent"/> was registered to its <see cref="IEventLoop"/>.
        /// This will result in having the <see cref="IAgentHandler.AgentRegistered"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentRegistered();

        /// <summary>
        /// An <see cref="IAgent"/> was unregistered from its <see cref="IEventLoop"/>.
        /// This will result in having the <see cref="IAgentHandler.AgentUnregistered"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentUnregistered();

        /// <summary>
        /// An <see cref="IAgent"/> is active now, which means it is connected.
        /// This will result in having the <see cref="IAgentHandler.AgentActive"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentActive();

        /// <summary>
        /// An <see cref="IAgent"/> is inactive now, which means it is closed.
        /// This will result in having the <see cref="IAgentHandler.AgentInactive"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentInactive();

        /// <summary>
        /// An <see cref="IAgent"/> received an <see cref="Exception"/> in one of its inbound operations.
        /// This will result in having the <see cref="IAgentHandler.ExceptionCaught"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <param name="cause">The <see cref="Exception"/> that was caught.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireExceptionCaught(Exception cause);

        /// <summary>
        /// An <see cref="IAgent"/> received an user defined event.
        /// This will result in having the <see cref="IAgentHandler.UserEventTriggered"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <param name="evt">The user-defined event that was triggered.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireUserEventTriggered(object evt);

        /// <summary>
        /// An <see cref="IAgent"/> received a message.
        /// This will result in having the <see cref="IAgentHandler.AgentRead"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <param name="msg">The message that was received.</param>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentRead(object msg);

        /// <summary>
        /// An <see cref="IAgent"/> completed a message after reading it.
        /// This will result in having the <see cref="IAgentHandler.AgentReadComplete"/> method
        /// called of the next <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentReadComplete();

        /// <summary>
        /// Triggers an <see cref="IAgentHandler.AgentWritabilityChanged"/> event to the next
        /// <see cref="IAgentHandler"/> in the <see cref="IAgentPipeline"/>.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline FireAgentWritabilityChanged();

        /// <summary>
        /// Request to bind to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.BindAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        Task BindAsync(EndPoint localAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.ConnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.ConnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <param name="localAddress">The local <see cref="EndPoint"/> to bind.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        /// <summary>
        /// Request to disconnect from the remote peer.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.DisconnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Request to close the <see cref="IAgent"/>. After it is closed it is not possible to reuse it again.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.CloseAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task CloseAsync();

        /// <summary>
        /// Request to deregister the <see cref="IAgent"/> bound this <see cref="IAgentPipeline"/> from the
        /// previous assigned <see cref="IEventExecutor"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.DeregisterAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DeregisterAsync();

        /// <summary>
        /// Request to Read data from the <see cref="IAgent"/> into the first inbound buffer, triggers an
        /// <see cref="IAgentHandler.AgentRead"/> event if data was read, and triggers a
        /// <see cref="IAgentHandler.AgentReadComplete"/> event so the handler can decide whether to continue
        /// reading. If there's a pending read operation already, this method does nothing.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.Read"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the  <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline Read();

        /// <summary>
        /// Request to write a message via this <see cref="IAgentPipeline"/>.
        /// This method will not request to actual flush, so be sure to call <see cref="Flush"/>
        /// once you want to request to flush all pending data to the actual transport.
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task WriteAsync(object msg);

        /// <summary>
        /// Request to flush all pending messages.
        /// </summary>
        /// <returns>This <see cref="IAgentPipeline"/>.</returns>
        IAgentPipeline Flush();

        /// <summary>
        /// Shortcut for calling both <see cref="WriteAsync"/> and <see cref="Flush"/>.
        /// </summary>
        Task WriteAndFlushAsync(object msg);
    }
}
