using Bonsai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Linq.Expressions;
using System.ComponentModel;
using Bonsai.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System.Diagnostics;

namespace BonsaiWeb
{
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Converts a string to JSON")]
    public class FromJSON : SelectBuilder
    {
        public string Schema { get; set; }


        public FromJSON()
        {
            // The default object has a dictionary with additional properties
            // it is therefore able to contain any possible JSON string.
            Schema = "{\"type\":\"object\"}";
        }        


        protected override Expression BuildSelector(Expression argument)
        {
            // There is no exception handling here, because Bonsai takes
            // care of that for us.

            var schema = JSchema.Parse(Schema);
            Type outputType = JSONSchemaTypeBuilder.Instance.SchemaToType(schema);
            return Expression.Call(
                typeof(JsonConvert),
                "DeserializeObject", 
                new[] { outputType }, 
                new[] { argument });
        }        
    }
}
