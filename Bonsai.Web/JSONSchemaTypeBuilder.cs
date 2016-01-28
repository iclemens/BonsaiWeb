using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using System.Reflection;
using System.Reflection.Emit;

using Newtonsoft.Json.Schema;

namespace BonsaiWeb
{
    public sealed class JSONSchemaTypeBuilder
    {
        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;
        private Dictionary<JSchema, Type> typeCache;

        private int count;


        /**
         * Singleton class
         */
        private static readonly JSONSchemaTypeBuilder instance = new JSONSchemaTypeBuilder();

        public static JSONSchemaTypeBuilder Instance { 
            get {
                return instance;
            } 
        }


        public JSONSchemaTypeBuilder()
        {
            AssemblyName assemblyName =
                new AssemblyName("JSONSchemaGeneratedTypes");

            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            typeCache = new Dictionary<JSchema,Type>();
            count = 0;
        }


        /**
         * Returns a name for a new type
         */
        private string GenerateTypeName()
        {
            string name = "DynamicType" + count;
            count++;

            return name;
        }


        /**
         * Compares to JSchemas to see if they are the same.
         */
        private bool CompareSchemas(JSchema one, JSchema two)
        {
            if(one.Type != two.Type)
                return false;

            if(one.Type == JSchemaType.Object) {
                if (one.Properties.Count() != two.Properties.Count())
                    return false;

                foreach (var entry in one.Properties) {
                    if(!two.Properties.ContainsKey(entry.Key))
                        return false;

                    if (!CompareSchemas(entry.Value, two.Properties[entry.Key]))
                        return false;
                }
            }

            if(one.Type == JSchemaType.Array) {
                if(one.Items.Count() != two.Items.Count())
                    return false;

                for (var i = 0; i < one.Items.Count(); i++) {
                    if (!CompareSchemas(one.Items[i], two.Items[i]))
                        return false;
                }
            }

            // Types and subproperties are not different, and thus the same
            return true;
        }


        /**
         * Check if we've created a type for this schema previously
         */
        private Tuple<bool, Type> FindCachedType(JSchema schema)
        {
            foreach(var entry in typeCache) {
                if (CompareSchemas(schema, entry.Key)) {                    
                    return new Tuple<bool, Type>(true, entry.Value);
                }
            }

            return new Tuple<bool, Type>(false, typeof(void));
        }


        /**
         * Create a type given an object
         * 
         * Note that this does not handle additionalProperties, which behave like dictionaries.
         */
        private Type ObjectToType(JSchema schema)
        {
            Tuple<bool, Type> result = FindCachedType(schema);

            if(result.Item1) {
                return result.Item2;
            }

            string typeName = GenerateTypeName();

            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName,
                TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                null);

            ConstructorBuilder constructor = typeBuilder.DefineDefaultConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // Add all properties
            foreach (KeyValuePair<string, JSchema> property in schema.Properties)
            {
                // Convert property to type
                Type propertyType = SchemaToType(property.Value);

                // Add property to object
                CreateProperty(
                    typeBuilder,
                    property.Key,
                    propertyType);
            }

            Type objectType = typeBuilder.CreateType();

            typeCache[schema] = objectType;
            return objectType;
        }


        private Type ArrayToType(JSchema schema)
        {
            if (schema.Items.Count() == 1) {
                Type type = SchemaToType(schema.Items.First());
                return type.MakeArrayType();
            }
            
            if (schema.Items.Count() > 1) {
                // Return tuple instead
            }

            return typeof(string);
        }


        public Type SchemaToType(JSchema schema)
        {
            switch (schema.Type)
            {
                case (JSchemaType.None):
                case (JSchemaType.Null):
                    return typeof(void);
                case (JSchemaType.Boolean):
                    return typeof(System.Boolean);
                case (JSchemaType.Integer):
                    return typeof(System.UInt64);
                case (JSchemaType.Number):
                    return typeof(System.Double);
                case (JSchemaType.String):
                    return typeof(System.String);

                case (JSchemaType.Array):
                    return ArrayToType(schema);
                case (JSchemaType.Object):
                    return ObjectToType(schema);
            }

            return typeof(System.String);
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
    }
}
