using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using DjvuNet.Errors;
using Xunit.Sdk;

namespace DjvuNet.Tests
{
    public class JsonXunitSerializer : IXunitSerializer
    {
        private static volatile JsonSerializerOptions _jsonOptions;
        private static readonly Lock _jsonOptionsLock = new Lock();

        public JsonXunitSerializer() { }

        public bool IsSerializable(Type type, object value, [NotNullWhen(false)] out string failureReason)
        {
            failureReason = null;
            return true;
        }

        public string Serialize(object value)
        {
            if (value == null)
                return "null";

            return JsonSerializer.Serialize(value, value.GetType(), GetJsonOptions());
        }

        public object Deserialize(Type type, string serializedValue)
        {
            if (serializedValue == "null")
            {
                return type.IsValueType ? Activator.CreateInstance(type)! : null!;
            }

            object retVal = JsonSerializer.Deserialize(serializedValue, type, GetJsonOptions());
            if (retVal != null)
            {
                return retVal;
            }
            else
            {
                DjvuExceptionUtil.ThrowInvalidOperation($"Failed to deserialize JSON payload to type {type.FullName}");
                return null;
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            if (_jsonOptions != null)
                return _jsonOptions;

            lock (_jsonOptionsLock)
            {
                return _jsonOptions ??= new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = false,
                    PropertyNameCaseInsensitive = false
                };
            }
        }
    }
}
