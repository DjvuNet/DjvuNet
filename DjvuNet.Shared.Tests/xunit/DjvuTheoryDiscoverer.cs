using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DjvuNet.Tests.Xunit
{
    public class DjvuTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        protected readonly IMessageSink _MessageSink;

        public DjvuTheoryDiscoverer(IMessageSink messageSink)
        {
            _MessageSink = messageSink;
        }

        public IMessageSink MessageSink { get { return _MessageSink; } }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, 
            IAttributeInfo factAttribute)
        {
            var skipReason = factAttribute.GetNamedArgument<string>("Skip");
            if (skipReason != null)
                return new[] 
                {
                    CreateTestCaseForSkip(discoveryOptions, testMethod, factAttribute, skipReason)
                };

            if (discoveryOptions.PreEnumerateTheoriesOrDefault())
            {
                try
                {
                    var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute)).ToList();
                    var results = new List<IXunitTestCase>();

                    for (int index = 0; index < dataAttributes.Count; index++)
                    {
                        var discovererAttribute = 
                            dataAttributes[index]
                            .GetCustomAttributes(typeof(DataDiscovererAttribute))
                            .First();

                        var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(MessageSink, discovererAttribute);
                        skipReason = dataAttributes[index].GetNamedArgument<string>("Skip");

                        if (!discoverer.SupportsDiscoveryEnumeration(dataAttributes[index], testMethod.Method))
                            return new[] { CreateTestCaseForTheory(discoveryOptions, testMethod, factAttribute) };

                        IEnumerable<object[]> data = 
                            discoverer.GetData(dataAttributes[index], testMethod.Method).ToList();

                        if (data is IDictionary<string, object[]>)
                        {
                            foreach (KeyValuePair<string, object[]> dataRow in (IDictionary<string, object[]>)data)
                            {
                                var testCase =
                                    skipReason != null
                                        ? CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, factAttribute, dataRow.Value, skipReason)
                                        : CreateTestCaseForNamedDataRow(discoveryOptions, testMethod, factAttribute, index, dataRow.Key);

                                results.Add(testCase);
                            }
                        }
                        else if (data.GroupBy(x => (x[0]?.ToString()) ?? string.Empty).All(x => x.Count() == 1))
                        {
                            foreach (object[] dataRow in data)
                            {
                                var testCase =
                                    skipReason != null
                                        ? CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, factAttribute, dataRow, skipReason)
                                        : CreateTestCaseForNamedDataRow(discoveryOptions, testMethod, factAttribute, index, dataRow[0].ToString() ?? string.Empty);

                                results.Add(testCase);
                            }
                        }
                        else
                        {
                            results.AddRange(
                                data.Select((x, i) =>
                                    skipReason != null ?
                                        CreateTestCaseForSkippedDataRow(discoveryOptions, testMethod, factAttribute, x, skipReason) :
                                        CreateTestCaseForDataRow(discoveryOptions, testMethod, factAttribute, index, i)));

                        }
                    }

                    if (results.Count == 0)
                        results.Add(
                            new ExecutionErrorTestCase(
                                MessageSink,
                                discoveryOptions.MethodDisplayOrDefault(),
                                testMethod,
                                $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));

                    return results;
                }
                catch (Exception ex)    // If something goes wrong, fall through to return just the XunitTestCase
                {
                    MessageSink.OnMessage(new DiagnosticMessage($"Exception thrown during theory discovery on '{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}'; falling back to single test case.{Environment.NewLine}{ex}"));
                }
            }

            return new[] { CreateTestCaseForTheory(discoveryOptions, testMethod, factAttribute) };
        }

        protected virtual IXunitTestCase CreateTestCaseForSkip(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, IAttributeInfo factAttribute, string skipReason)
        {
            return new XunitTestCase(_MessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, null);
        }

        protected virtual IXunitTestCase CreateTestCaseForTheory(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod, IAttributeInfo factAttribute, string skipReason = null)
        {
            return new XunitTestCase(_MessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, null);
        }

        protected virtual IXunitTestCase CreateTestCaseForSkippedDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, IAttributeInfo factAttribute, 
            object[] dataRow, string skipReason)
        {
             return new XunitSkippedDataRowTestCase(
                 MessageSink, 
                 discoveryOptions.MethodDisplayOrDefault(), 
                 testMethod, 
                 skipReason, 
                 dataRow);
        }

        protected virtual IXunitTestCase CreateTestCaseForDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, IAttributeInfo theoryAttribute, 
            int dataAttributeNumber, int dataRowNumber)
        {
            return new DjvuDataRowTestCase(
                MessageSink, 
                discoveryOptions.MethodDisplayOrDefault(), 
                testMethod, 
                dataAttributeNumber, 
                dataRowNumber);
        }

        protected virtual IXunitTestCase CreateTestCaseForNamedDataRow(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, 
            IAttributeInfo theoryAttribute, 
            int dataAttributeNumber, 
            string dataRowName)
        {
            return new DjvuNamedDataRowTestCase(
                MessageSink, 
                discoveryOptions.MethodDisplayOrDefault(), 
                testMethod, 
                dataAttributeNumber, 
                dataRowName);
        }

    }
}
