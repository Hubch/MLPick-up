using MLModeling.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLModeling.Modeler.Agents
{
    using System;
    using System.Diagnostics.Contracts;

    public sealed class ActionAgentInitializer<T> : AgentInitializer<T>
        where T : IAgent
    {
        readonly Action<T> initializationAction;

        public ActionAgentInitializer(Action<T> initializationAction)
        {
            Contract.Requires(initializationAction != null);

            this.initializationAction = initializationAction;
        }

        protected override void InitAgent(T channel) => this.initializationAction(channel);

        public override string ToString() => nameof(ActionAgentInitializer<T>) + "[" + StringUtil.SimpleClassName(typeof(T)) + "]";
    }
}
