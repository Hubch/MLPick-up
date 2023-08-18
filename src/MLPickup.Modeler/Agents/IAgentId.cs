
namespace MLPickup.Modeler.Agents
{
    using System;
    public interface IAgentId : IComparable<IAgentId>
    {
        string AsShortText();

        string AsLongText();
    }
}
