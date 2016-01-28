using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;

using Newtonsoft.Json.Schema;

namespace SchemaToType
{
    class JSONTypeBuilder
    {
        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;


        public JSONTypeBuilder()
        {
            AssemblyName assemblyName = 
                new AssemblyName("JSONSchemaGeneratedTypes");

            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }


        /**
         * Create a type given an object
         * 
         * Note that this does not handle additionalProperties, which behave like dictionaries.
         */
        protected Type ObjectToType(string typeSignature, JSchema schema)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public | TypeAttributes.Class |
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                null);

            ConstructorBuilder constructor = typeBuilder.DefineDefaultConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // Add all properties
            foreach(KeyValuePair<string, JSchema> property in schema.Properties)
            {
                string propertyTypeSignature = typeSignature;

                if (typeSignature != "")
                    propertyTypeSignature += ".";

                propertyTypeSignature += property.Key;

                // Convert property to type
                Type propertyType = SchemaToType(propertyTypeSignature, property.Value);

                // Add property to object
                CreateProperty(
                    typeBuilder,
                    property.Key,
                    propertyType);
            }

            Type objectType = typeBuilder.CreateType();
            return objectType;

        }


        protected Type ArrayToType(string typeSignature, JSchema schema)
        {
            if(schema.Items.Count() == 1) {
                Type type = SchemaToType(typeSignature + "[]", schema.Items.First());
                return type.MakeArrayType();
            } else {
                // Handle tuples
            }

            return typeof(void);
        }


        public Type SchemaToType(string typeSignature, JSchema schema) 
        {
            switch (schema.Type)
            {
                case(JSchemaType.None):
                case(JSchemaType.Null):
                    return typeof(void);
                case(JSchemaType.Boolean):
                    return typeof(System.Boolean);
                case(JSchemaType.Integer):
                    return typeof(System.UInt64);
                case(JSchemaType.Number):
                    return typeof(System.Double);
                case(JSchemaType.String):
                    return typeof(System.String);

                case(JSchemaType.Array):
                    return ArrayToType(typeSignature, schema);
                case(JSchemaType.Object):
                    return ObjectToType(typeSignature, schema);
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
