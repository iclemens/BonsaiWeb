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
        public FromJSON(): base(minArguments: 0, maxArguments: 0)
        {
            Schema = @"{
                'type': 'array',
                'item': {'type':'string'}
            }";
        }

        public string Schema { get; set; }

        protected override Expression BuildCombinator(IEnumerable<Expression> arguments)
        {
            var builder = Expression.Constant(this);
            var schema = JSchema.Parse(Schema);

            

            //TypeBuilder typeBuilder = 
            Debug.WriteLine(schema.Type.ToString());

            return Expression.Call(builder, "Generate", new[] { typeof(Object) });
        }

        IObservable<TResult> Generate<TResult>()
        {
            return Observable.Return<TResult>(

                JsonConvert.DeserializeObject<TResult>("")
                
                );
            //return source.Select(x => JsonConvert.DeserializeObject<TResult>(x));
        }
            
        
    }
}
