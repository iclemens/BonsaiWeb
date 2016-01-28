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

        private JSONTypeBuilder jtb;
        private int type_id;

        public FromJSON()
        {
            jtb = new JSONTypeBuilder();

            Schema = @"{
                {""type"":""object"",""properties"":{""type"": {""type"": ""string""},""status"":{""type"": ""string""},""message"":{""type"": ""string""}}}
            }";
        }        


        protected override Expression BuildSelector(Expression argument)
        {
            //var builder = Expression.Constant(JsonConvert);
            type_id += 1;

            try {
                Debug.WriteLine("Generating type...");
                var schema = JSchema.Parse(Schema);
                Type outputType = jtb.SchemaToType("root" + type_id, schema);
                return Expression.Call(
                    typeof(JsonConvert),
                    "DeserializeObject", 
                    new[] { outputType }, 
                    new[] { argument });
            } catch(JSchemaReaderException readerException) {
                Debug.WriteLine("JSchemaReaderException: " + readerException.Message);
                //return Expression.Call(builder, "DeserializeObject", new[] { typeof(object) });
                throw readerException;
            } catch(ArgumentException argumentException) {
                Debug.WriteLine("ArgumentException: duplicate type name?");
                //return Expression.Call(builder, "DeserializeObject", new[] { typeof(object) });
                throw argumentException;
            } catch (Exception exception) {
                Debug.WriteLine("Exception: " + exception.Message);
                // Something went wrong, how do we handle it?
                //return Expression.Call(builder, "DeserializeObject", new[] { typeof(object) });
                throw exception;
            }
        }        
    }
}
