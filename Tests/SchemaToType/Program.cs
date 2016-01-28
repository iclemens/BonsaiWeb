using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;

using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace SchemaToType
{
    class Program
    {
        static void Main(string[] args)
        {
            string exampleSchema = @"{
                'title': 'Example Schema',
                'type': 'object',
                'properties': {
                    'firstName': { 'type': 'string' },
                    'lastName': { 'type': 'string' },
                    'age': { 'type': 'integer' },
                    'nestedObject': { 
                        'type': 'object',
                        'properties': { 'nestedProperty': {'type': 'string' } }
                    },
                    'tags': { 
                        'type': 'array',
                        'items': { 'type': 'string' }
                    }
                }
            }";

            string exampleJson = @"
                { 'firstName': 'Ivar', 'lastName': 'Clemens', 'age': 31, 'nestedObject': { 'nestedProperty': 'value' }, 'tags': ['one', 'two', 'three'] }
            ";

            JSONTypeBuilder jtb = new JSONTypeBuilder();
            Type type = jtb.SchemaToType("root", schema);

            /**
             * Create deserialization function
             * 
             * Note that the JSON is NOT validated against the generated type...
             */
            MethodInfo deserializeMethod = typeof(JsonConvert)
                .GetMethods()
                .Where(method => method.Name == "DeserializeObject")
                .Where(method => method.IsGenericMethod)
                .Where(method => method.GetParameters().Count() == 1)
                .First()
                .MakeGenericMethod(type);

            try {
                var obj = deserializeMethod.Invoke(null, new[] { exampleJson });

                var propertyInfo = obj.GetType().GetProperty("firstName");
                Console.WriteLine("Property value: " + propertyInfo.GetValue(obj, null));
            } catch(TargetInvocationException e) {
                throw e.InnerException;
            }

            Console.ReadLine();
        }
    }
}
