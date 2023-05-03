using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace kv.Entities.V2.SourceGen
{
    [Generator]
    public class EntitiesApi : ISourceGenerator
    {
        public const string TypeName = "EntitiesApi";
        public const string Namespace = "kv.Entities.V2.Generated";
        
        public const string ConstWorld = "global::kv.Entities.V2.World";
        public const string ConstIComponentArrayPool = "global::kv.Entities.V2.IComponentArrayPool";
        public const string ConstComponentArrayPool = "global::kv.Entities.V2.Generated.ComponentArrayPool";
        public const string ConstComponentTypeMap = "global::kv.Entities.V2.Generated.ComponentTypeMap";
        public const string ConstIEntityComponent = "global::kv.Entities.V2.IEntityComponent";
        
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if UNITY_ENGINE
            if (context.Compilation.AssemblyName != "Assembly-CSharp")
            {
                return;
            }
#endif
            
            StringBuilder sb = new();
            var indentation = 0;
            
            sb.AppendLine(indentation, "#nullable enable");
            sb.AppendLine(indentation, $"namespace {Namespace}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"public static class {TypeName}");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"public static {ConstWorld} CreateWorld()");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, $"var typeMap = {ConstComponentTypeMap}.Create();");
                        sb.AppendLine(indentation, $"{ConstIComponentArrayPool} componentArrayPool = new {ConstComponentArrayPool}(typeMap);");
                        sb.AppendLine(indentation, "return new(typeMap, componentArrayPool);");
                    }
                    indentation--;
                    sb.AppendLine(indentation, "}");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            context.AddSource($"{context.Compilation.AssemblyName}.{TypeName}.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}