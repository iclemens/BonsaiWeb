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
    public class FromJSON : CombinatorExpressionBuilder
    {
        public string Schema { get; set; }

        private JSONTypeBuilder jtb;
        private int type_id;

        public FromJSON(): base(minArguments: 0, maxArguments: 0)
        {
            jtb = new JSONTypeBuilder();

            Schema = @"{
                'type': 'array',
                'item': {'type':'string'}
            }";
        }        


        protected override Expression BuildCombinator(IEnumerable<Expression> arguments)
        {
            var builder = Expression.Constant(this);
            type_id += 1;

            try {
                Debug.WriteLine("Generating type...");
                var schema = JSchema.Parse(Schema);
                Type outputType = jtb.SchemaToType("root" + type_id, schema);
                return Expression.Call(builder, "Generate", new[] { outputType });
            } catch(JSchemaReaderException readerException) {
                Debug.WriteLine("JSchemaReaderException: " + readerException.Message);
                return Expression.Call(builder, "Generate", new[] { typeof(object) });
            } catch(ArgumentException) {
                Debug.WriteLine("ArgumentException: duplicate type name?");
                return Expression.Call(builder, "Generate", new[] { typeof(object) });
            } catch (Exception exception) {
                Debug.WriteLine("Exception: " + exception.Message);
                // Something went wrong, how do we handle it?
                return Expression.Call(builder, "Generate", new[] { typeof(object) });
            }
        }

        IObservable<TResult> Generate<TResult>()
        {
            return Observable.Return<TResult>(

                JsonConvert.DeserializeObject<TResult>("")
                
                );

            return source.Select(x => JsonConvert.DeserializeObject<TResult>(x));
        }
            
        
    }
}
