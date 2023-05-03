using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace kv.Entities.V2.SourceGen
{
    [Generator]
    public class ComponentTypeMap : ISourceGenerator
    {
        public const string TypeName = "ComponentTypeMap";
        public const string Namespace = "kv.Entities.V2.Generated";
        
        public const string ConstTypeMap = "global::kv.Entities.V2.TypeMap";
        public const string ConstTypeInfo = "global::kv.Entities.V2.TypeInfo";
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
            
            var namedTypeSymbols = new List<INamedTypeSymbol>();
            Utils.GetNamedTypes(context.Compilation.GlobalNamespace.GetNamespaceMembers(), ref namedTypeSymbols);
            
            var entityComponentTypes = namedTypeSymbols
                .Where(t => Utils.ImplementsInterface(t, ConstIEntityComponent) && t.IsType && !t.IsAbstract && !t.IsStatic)
                .ToList();
            
            StringBuilder sb = new();
            var indentation = 0;
            
            sb.AppendLine(indentation, "#nullable enable");
            sb.AppendLine(indentation, "using System.Runtime.CompilerServices;");
            sb.AppendLine(indentation, $"namespace {Namespace}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"public static class {TypeName}");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"public static {ConstTypeMap}<{ConstTypeInfo}> Create()");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, $"{ConstTypeMap}<{ConstTypeInfo}> typeMap = new();");
                        var index = 0;
                        foreach (var entityComponentType in entityComponentTypes)
                        {
                            var displayName = entityComponentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            var isUnmanaged = entityComponentType.IsUnmanagedType ? "true" : "false";
                            var fields = entityComponentType.GetMembers().OfType<IFieldSymbol>();
                            var hasFields = !entityComponentType.IsUnmanagedType || fields.Any() ? "true" : "false";
                            sb.AppendLine(indentation, $"typeMap.Set<{displayName}>(new {ConstTypeInfo}({index.ToString()}, typeof({displayName}), Unsafe.SizeOf<{displayName}>(), Unsafe.SizeOf<{displayName}>(), {hasFields}, {isUnmanaged}, false));");
                            index++;
                        }
                        sb.AppendLine(indentation, "return typeMap;");
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