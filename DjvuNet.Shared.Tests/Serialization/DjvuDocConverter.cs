using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DjvuNet.Serialization
{
    public class DjvuDocConverter : JsonCreationConverter<DjvuDoc>
    {
        protected override DjvuDoc Create(Type objectType, JObject jsonObject)
        {
            return new DjvuDoc();
        }
    }
}
