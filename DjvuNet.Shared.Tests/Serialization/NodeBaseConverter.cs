using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DjvuNet.Serialization
{
    public class NodeBaseConverter : JsonCreationConverter<NodeBase>
    {
        protected override NodeBase Create(Type objectType, JObject jsonObject)
        {
            string type = (string)jsonObject["$type"];
            var t = Type.GetType(type);
            return (NodeBase) Activator.CreateInstance(t);
        }
    }
}
