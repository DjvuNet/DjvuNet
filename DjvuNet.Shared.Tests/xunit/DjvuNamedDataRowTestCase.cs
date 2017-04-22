using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DjvuNet.Tests.Xunit
{
    public class DjvuNamedDataRowTestCase : TestMethodTestCase, IXunitTestCase
    {
        private readonly IMessageSink _MessageSink;
        private int _AttributeNumber;
        private string _RowName;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes")]
        public DjvuNamedDataRowTestCase()
        {
            _MessageSink = new NullMessageSink();
        }

        public DjvuNamedDataRowTestCase(
            IMessageSink messageSink,
            TestMethodDisplay defaultMethodDisplay,
            ITestMethod testMethod,
            int attributeNumber,
            string rowName)
            : base(
                  defaultMethodDisplay, 
                  testMethod, 
                  GetTestMethodArguments(testMethod, attributeNumber, rowName, messageSink))
        {
            _AttributeNumber = attributeNumber;
            _RowName = rowName;
            _MessageSink = messageSink;
        }

        protected virtual string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        { 
            return TestMethod.Method
                .GetDisplayNameWithArguments(displayName, TestMethodArguments, MethodGenericTypes);
        }

        protected virtual string GetSkipReason(IAttributeInfo factAttribute)
        { 
            return factAttribute.GetNamedArgument<string>("Skip");
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            var factAttribute = TestMethod.Method.GetCustomAttributes(typeof(FactAttribute)).First();
            var baseDisplayName = factAttribute.GetNamedArgument<string>("DisplayName") ?? BaseDisplayName;

            DisplayName = GetDisplayName(factAttribute, baseDisplayName);
            SkipReason = GetSkipReason(factAttribute);

            foreach (var traitAttribute in GetTraitAttributesData(TestMethod))
            {
                var discovererAttribute = 
                    traitAttribute.GetCustomAttributes(typeof(TraitDiscovererAttribute)).FirstOrDefault();

                if (discovererAttribute != null)
                {
                    var discoverer = ExtensibilityPointFactory.GetTraitDiscoverer(_MessageSink, discovererAttribute);
                    if (discoverer != null)
                        foreach (var keyValuePair in discoverer.GetTraits(traitAttribute))
                            Add(Traits, keyValuePair.Key, keyValuePair.Value);
                }
                else
                    _MessageSink.OnMessage(
                        new DiagnosticMessage(
                            $"Trait attribute on '{DisplayName}' did not have [TraitDiscoverer]"));
            }
        }

        static void Add<TKey, TValue>(IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            (dictionary[key] ?? (dictionary[key] = new List<TValue>())).Add(value);
        }

        static IEnumerable<IAttributeInfo> GetTraitAttributesData(ITestMethod testMethod)
        {
            return testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(ITraitAttribute))
                .Concat(testMethod.Method.GetCustomAttributes(typeof(ITraitAttribute)))
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(ITraitAttribute)));
        }

        static object[] GetTestMethodArguments(
            ITestMethod testMethod, int attributeNumber, string rowName, IMessageSink diagnosticMessageSink)
        {
            try
            {
                IAttributeInfo dataAttribute = 
                    testMethod.Method
                    .GetCustomAttributes(typeof(DataAttribute))
                    .Where((x, i) => i == attributeNumber)
                    .FirstOrDefault();

                if (dataAttribute == null)
                    return null;

                IAttributeInfo discovererAttribute = 
                    dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();

                IDataDiscoverer discoverer = 
                    ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);

                IEnumerable<object[]> data = discoverer.GetData(dataAttribute, testMethod.Method);

                if (data is IDictionary<string, object[]>)
                    return ((IDictionary<string, object[]>)data)[rowName];

                return data.Where(x => x[0].ToString() == rowName).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            data.AddValue("TestMethod", TestMethod);
            data.AddValue("AttributeNumber", _AttributeNumber);
            data.AddValue("RowName", _RowName);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            TestMethod = data.GetValue<ITestMethod>("TestMethod");
            _AttributeNumber = data.GetValue<int>("AttributeNumber");
            _RowName = data.GetValue<string>("RowName");
            TestMethodArguments = GetTestMethodArguments(TestMethod, _AttributeNumber, _RowName, _MessageSink);
        }

        protected override string GetUniqueID()
        {
            return 
                $"{TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name};" + 
                $"{TestMethod.TestClass.Class.Name};" + 
                $"{TestMethod.Method.Name};" + 
                $"{_AttributeNumber}/{_RowName}";
        }

        /// <inheritdoc/>
        public virtual Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                 IMessageBus messageBus,
                                                 object[] constructorArguments,
                                                 ExceptionAggregator aggregator,
                                                 CancellationTokenSource cancellationTokenSource)
        { 
            return 
                new XunitTestCaseRunner(
                    this, 
                    DisplayName, 
                    SkipReason, 
                    constructorArguments, 
                    TestMethodArguments, 
                    messageBus, 
                    aggregator, 
                    cancellationTokenSource).RunAsync();
        }
    }
}
