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
        static TypeBuilder GetTypeBuilder()
        {
            var typeSignature = "MyDynamicType";
            var assemblyName = new AssemblyName(typeSignature);

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                null);

            return typeBuilder;
        }

        static Type BuildType()
        {
            TypeBuilder typeBuilder = GetTypeBuilder();
            ConstructorBuilder constructor = typeBuilder.DefineDefaultConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            CreateProperty(
                typeBuilder, 
                "firstName", 
                typeof(System.String));

            Type objectType = typeBuilder.CreateType();
            return objectType;
        }

        static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            
            /**
             * Create getter method
             */
            MethodBuilder getPropMethodBuilder = typeBuilder.DefineMethod("get_" + propertyName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType, Type.EmptyTypes);

            ILGenerator ilGetGenerator = getPropMethodBuilder.GetILGenerator();
            ilGetGenerator.Emit(OpCodes.Ldarg_0);
            ilGetGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGetGenerator.Emit(OpCodes.Ret);

            /**
             * Create setter method
             */
            MethodBuilder setPropMethodBuilder = typeBuilder.DefineMethod("set_" + propertyName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, new[] { propertyType });

            ILGenerator ilSetGenerator = setPropMethodBuilder.GetILGenerator();
            Label modifyProperty = ilSetGenerator.DefineLabel();
            Label exitSet = ilSetGenerator.DefineLabel();

            ilSetGenerator.MarkLabel(modifyProperty);
            ilSetGenerator.Emit(OpCodes.Ldarg_0);
            ilSetGenerator.Emit(OpCodes.Ldarg_1);
            ilSetGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            ilSetGenerator.Emit(OpCodes.Nop);
            ilSetGenerator.MarkLabel(exitSet);
            ilSetGenerator.Emit(OpCodes.Ret);

            /**
             * Set getter and setters
             */
            propertyBuilder.SetGetMethod(getPropMethodBuilder);
            propertyBuilder.SetSetMethod(setPropMethodBuilder);
        }

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

            var schema = JSchema.Parse(exampleSchema);

            // Build type
            Type type = BuildType();

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
