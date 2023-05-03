using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace kv.Entities.V2.SourceGen
{
    [Generator]
    public class ComponentArrayPool : ISourceGenerator
    {
        public const string TypeName = "ComponentArrayPool";
        public const string Namespace = "kv.Entities.V2.Generated";
        
        public const string ConstIComponentArrayPool = "global::kv.Entities.V2.IComponentArrayPool";
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
            sb.AppendLine(indentation, "using System.Buffers;");
            sb.AppendLine(indentation, "using System.Runtime.CompilerServices;");
            sb.AppendLine(indentation, $"namespace {Namespace}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"public class {TypeName} : {ConstIComponentArrayPool}");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, "private readonly Func<int, Array>[] _rentFunctions;");
                    sb.AppendLine(indentation, "private readonly Action<Array>[] _returnFunctions;");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"public {TypeName}({ConstTypeMap}<{ConstTypeInfo}> typeMap)");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, $"{ConstTypeInfo} typeInfo;");
                        sb.AppendLine();
                        sb.AppendLine(indentation, "_rentFunctions = new Func<int, Array>[typeMap.Capacity];");
                        sb.AppendLine(indentation, "_returnFunctions = new Action<Array>[typeMap.Capacity];");
                        sb.AppendLine();
                        
                        foreach (var entityComponentType in entityComponentTypes)
                        {
                            var displayName = entityComponentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            sb.AppendLine(indentation, $"typeMap.TryGetValue<{displayName}>(out typeInfo);");
                            sb.AppendLine(indentation, $"_rentFunctions[typeInfo.Id] = (capacity) => ArrayPool<{displayName}>.Shared.Rent(capacity);");
                            sb.AppendLine(indentation, $"_returnFunctions[typeInfo.Id] = (array) => ArrayPool<{displayName}>.Shared.Return(Unsafe.As<{displayName}[]>(array));");
                        }
                    }
                    indentation--;
                    sb.AppendLine(indentation, "}");
                    
                    sb.AppendLine(indentation, $"public Array Rent({ConstTypeInfo} typeInfo, int minimumLength) => _rentFunctions[typeInfo.Id].Invoke(minimumLength);");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"public void Return({ConstTypeInfo} typeInfo, Array array) => _returnFunctions[typeInfo.Id].Invoke(array);");
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