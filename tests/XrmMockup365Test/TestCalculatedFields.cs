using System;
using System.Collections.Generic;
using WorkflowExecuter;
using Xunit;

namespace DG.XrmMockupTest
{
    // The classic calculated-field engine concatenates strings via the WorkflowExecuter.Concat node.
    // Dataverse permits a calculated field that references a non-string column inside Concat (the
    // platform stringifies the value before joining), so the node must accept any operand type, not
    // only strings. Before the fix, the (string) cast threw InvalidCastException, which the Retrieve
    // / RetrieveMultiple handlers re-raised because they execute every calculated field on each row
    // regardless of whether it was selected.
    public class TestCalculatedFields
    {
        [Fact]
        public void Concat_MixesStringAndInt()
        {
            var node = new Concat(new[] { new[] { "a", "b", "c" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Order-",
                ["b"] = 42,
                ["c"] = " (priority)",
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("Order-42 (priority)", variables["result"]);
        }

        [Fact]
        public void Concat_MixesStringAndDecimal()
        {
            var node = new Concat(new[] { new[] { "a", "b" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Total: ",
                ["b"] = 12.5m,
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("Total: 12.5", variables["result"]);
        }

        [Fact]
        public void Concat_TreatsNullAsEmpty()
        {
            var node = new Concat(new[] { new[] { "a", "b", "c" } }, "result");
            var variables = new Dictionary<string, object>
            {
                ["a"] = "Start",
                ["b"] = null,
                ["c"] = "End",
            };

            node.Execute(ref variables, TimeSpan.Zero, null, null, null);

            Assert.Equal("StartEnd", variables["result"]);
        }
    }
}
