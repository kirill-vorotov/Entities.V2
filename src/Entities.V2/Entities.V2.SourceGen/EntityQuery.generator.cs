/*
 * MIT License
 *
 * Copyright (c) 2023 Kirill Vorotov
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace kv.Entities.V2.SourceGen
{
    [Generator]
    public partial class EntityQuery : ISourceGenerator
    {
        public struct Entry
        {
            public INamedTypeSymbol QueryNamedTypeSymbol;
            public ImmutableArray<AttributeData> Attributes;
        }
        
        public const string ConstDelegate = "delegate";
        public const string ConstSystem = "System";
        public const string ConstSystemCollections = "System.Collections";
        public const string ConstSystemCollectionsGeneric = "System.Collections.Generic";
        public const string ConstEntitiesWorld = "global::kv.Entities.V2.World";
        public const string ConstIEntityComponent = "global::kv.Entities.V2.IEntityComponent";
        public const string ConstTypeInfo = "global::kv.Entities.V2.TypeInfo";
        public const string ConstList = "global::System.Collections.Generic.List";
        public const string ConstIReadOnlyList = "global::System.Collections.Generic.IReadOnlyList";
        
        public const string ConstAllAttribute = "global::kv.Entities.V2.AllAttribute";
        public const string ConstAnyAttribute = "global::kv.Entities.V2.AnyAttribute";
        public const string ConstNoneAttribute = "global::kv.Entities.V2.NoneAttribute";

        public const string ConstChunk = "global::kv.Entities.V2.Chunk";
        public const string ConstEntity = "global::kv.Entities.V2.Entity";
        public const string ConstChunkQuery = "global::kv.Entities.V2.ChunkQuery";
        
        public const string ConstQueryPredicate = "QueryPredicate";
        public const string ConstQueryGetEnumerator = "GetEnumerator";
        public const string ConstQueryCreate = "Create";
        
        public const string ConstEntry = "Entry";
        
        public const string ConstEnumerator = "Enumerator";
        public const string ConstEnumeratorMoveNext = "MoveNext";
        public const string ConstEnumeratorCurrent = "Current";
        public const string ConstEnumeratorReset = "Reset";
        public const string ConstEnumeratorUpdate = "Update";
        public const string ConstEnumeratorCheckPredicate = "CheckPredicate";
        
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            StringBuilder sb = new();
            
            List<INamedTypeSymbol> namedTypeSymbols = new();
            Utils.GetNamedTypes(context.Compilation.GlobalNamespace.GetNamespaceMembers(), ref namedTypeSymbols);

            var queryNamedTypeSymbols = namedTypeSymbols.Where(t => t.GetAttributes().Any());

            var entries = namedTypeSymbols
                .Where(t => t.GetAttributes()
                    .Any(a => a.AttributeClass is not null &&
                              (a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                               ConstAllAttribute ||
                               a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                               ConstAnyAttribute ||
                               a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                               ConstNoneAttribute)))
                .Select(t => new Entry
                {
                    QueryNamedTypeSymbol = t,
                    Attributes = t.GetAttributes()
                });

            foreach (var entry in entries)
            {
                CreateQuery(context, entry.QueryNamedTypeSymbol, entry.Attributes, sb);
            }
        }

        public static void CreateQuery(GeneratorExecutionContext context, INamedTypeSymbol queryNamedTypeSymbol, ImmutableArray<AttributeData> attributes, StringBuilder sb)
        {
            var queryTypeName = queryNamedTypeSymbol.Name;
            
            List<INamedTypeSymbol> containingTypes = new();
            FindContainingTypes(queryNamedTypeSymbol, ref containingTypes);
            containingTypes.Reverse();
            
            sb.Clear();
            var indentation = 0;

            sb.AppendLine(indentation, "#nullable enable");
            sb.AppendLine(indentation, $"using {ConstSystem};");
            sb.AppendLine(indentation, $"using {ConstSystemCollections};");
            sb.AppendLine(indentation, $"using {ConstSystemCollectionsGeneric};");
            sb.AppendLine();
            sb.AppendLine(indentation, $"namespace {queryNamedTypeSymbol.ContainingNamespace}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                foreach (var containingType in containingTypes)
                {
                    var containingTypeKind = containingType.TypeKind switch
                    {
                        TypeKind.Unknown => string.Empty,
                        TypeKind.Array => string.Empty,
                        TypeKind.Class => "class",
                        TypeKind.Delegate => "delegate",
                        TypeKind.Dynamic => string.Empty,
                        TypeKind.Enum => string.Empty,
                        TypeKind.Error => string.Empty,
                        TypeKind.Interface => "interface",
                        TypeKind.Module => string.Empty,
                        TypeKind.Pointer => string.Empty,
                        TypeKind.Struct => "struct",
                        TypeKind.TypeParameter => string.Empty,
                        TypeKind.Submission => string.Empty,
                        TypeKind.FunctionPointer => string.Empty,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    
                    var isStatic = containingType.IsStatic ? " static" : string.Empty;
                    var isRefLikeType = containingType.IsRefLikeType ? " ref" : string.Empty;
                    var genericsString = containingType.IsGenericType && containingType.TypeParameters.Any() ? $"<{string.Join(", ", containingType.TypeParameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>" : string.Empty;

                    sb.AppendLine(indentation, $"public{isRefLikeType}{isStatic} partial {containingTypeKind} {containingType.Name}{genericsString}");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    sb.AppendLine();
                }

                CreateQueryDefinition(queryNamedTypeSymbol, attributes, sb, ref indentation);

                for (var i = 0; i < containingTypes.Count; i++)
                {
                    indentation--;
                    sb.AppendLine(indentation, "}");
                }
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            var hintName = string.Join('.', containingTypes.Select(t => t.Name));
            if (string.IsNullOrEmpty(hintName))
            {
                context.AddSource($"{context.Compilation.AssemblyName}.{queryTypeName}.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            else
            {
                context.AddSource($"{context.Compilation.AssemblyName}.{hintName}.{queryTypeName}.generated", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        public static void CreateQueryDefinition(INamedTypeSymbol queryNamedTypeSymbol, ImmutableArray<AttributeData> attributes, StringBuilder sb, ref int indentation)
        {
            var allAttributes = attributes.Where(a =>
                a.AttributeClass is not null &&
                a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ConstAllAttribute);
            
            var anyAttributes = attributes.Where(a =>
                a.AttributeClass is not null &&
                a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ConstAnyAttribute);
            
            var noneAttributes = attributes.Where(a =>
                a.AttributeClass is not null &&
                a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ConstNoneAttribute);

            HashSet<INamedTypeSymbol> allReadWrite = new(SymbolEqualityComparer.Default);
            HashSet<INamedTypeSymbol> allReadOnly = new(SymbolEqualityComparer.Default);
            
            HashSet<INamedTypeSymbol> anyReadWrite = new(SymbolEqualityComparer.Default);
            HashSet<INamedTypeSymbol> anyReadOnly = new(SymbolEqualityComparer.Default);
            
            HashSet<INamedTypeSymbol> none = new(SymbolEqualityComparer.Default);

            foreach (var attributeData in allAttributes)
            {
                if (attributeData.ConstructorArguments.Length != 2)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol componentType)
                {
                    continue;
                }
                
                var enumIntValue = (int)(attributeData.ConstructorArguments[1].Value ?? 0);
                var isReadOnly = enumIntValue == 0;

                if (isReadOnly)
                {
                    allReadOnly.Add(componentType);
                }
                else
                {
                    allReadWrite.Add(componentType);
                }
            }
            
            foreach (var attributeData in anyAttributes)
            {
                if (attributeData.ConstructorArguments.Length != 2)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol componentType)
                {
                    continue;
                }
                
                var enumIntValue = (int)(attributeData.ConstructorArguments[1].Value ?? 0);
                var isReadOnly = enumIntValue == 0;

                if (isReadOnly)
                {
                    anyReadOnly.Add(componentType);
                }
                else
                {
                    anyReadWrite.Add(componentType);
                }
            }

            foreach (var attributeData in noneAttributes)
            {
                if (attributeData.ConstructorArguments.Length != 1)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol componentType)
                {
                    continue;
                }

                none.Add(componentType);
            }

            var typeKind = queryNamedTypeSymbol.TypeKind switch
            {
                TypeKind.Unknown => string.Empty,
                TypeKind.Array => string.Empty,
                TypeKind.Class => "class",
                TypeKind.Delegate => "delegate",
                TypeKind.Dynamic => string.Empty,
                TypeKind.Enum => string.Empty,
                TypeKind.Error => string.Empty,
                TypeKind.Interface => "interface",
                TypeKind.Module => string.Empty,
                TypeKind.Pointer => string.Empty,
                TypeKind.Struct => "struct",
                TypeKind.TypeParameter => string.Empty,
                TypeKind.Submission => string.Empty,
                TypeKind.FunctionPointer => string.Empty,
                _ => throw new ArgumentOutOfRangeException()
            };
                    
            var isStatic = queryNamedTypeSymbol.IsStatic ? " static" : string.Empty;
            var isRefLikeType = queryNamedTypeSymbol.IsRefLikeType ? " ref" : string.Empty;
            var genericsString = queryNamedTypeSymbol.IsGenericType && queryNamedTypeSymbol.TypeParameters.Any() ? $"<{string.Join(", ", queryNamedTypeSymbol.TypeParameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>" : string.Empty;

            sb.AppendLine(indentation, $"public{isRefLikeType}{isStatic} partial {typeKind} {queryNamedTypeSymbol.Name}{genericsString}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                #region Predicate

                sb.AppendLine(indentation, $"public {ConstDelegate} bool {ConstQueryPredicate}({ConstEntry} entry);");

                #endregion

                sb.AppendLine();
                
                #region Entry

                CreateEntry(sb, ref indentation, allReadWrite, allReadOnly, anyReadWrite, anyReadOnly);
                
                #endregion

                sb.AppendLine();
                
                #region Enumerator

                CreateEnumerator(sb, ref indentation, allReadWrite, allReadOnly, anyReadWrite, anyReadOnly);

                #endregion

                sb.AppendLine();
                
                #region Fields

                sb.AppendLine(indentation, $"public {ConstIReadOnlyList}<{ConstChunk}> Chunks;");
                sb.AppendLine(indentation, $"public {ConstQueryPredicate}? Predicate;");
                
                #endregion
                
                sb.AppendLine();
                
                #region Ctor
                
                sb.AppendLine(indentation, $"private {queryNamedTypeSymbol.Name}({ConstIReadOnlyList}<{ConstChunk}> chunks, {ConstQueryPredicate}? predicate = null)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"Chunks = chunks;");
                    sb.AppendLine(indentation, $"Predicate = predicate;");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();

                #region GetEnumerator()
                
                sb.AppendLine(indentation, $"public {ConstEnumerator} {ConstQueryGetEnumerator}()");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"return new(Chunks, Predicate);");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();
                
                #region Create()
                
                sb.AppendLine(indentation, $"public static {queryNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {ConstQueryCreate}({ConstEntitiesWorld} world)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"if (!world.ChunkQueries.TryGetValue(typeof({queryNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), out var chunkQuery))");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, $"chunkQuery = world.CreateChunkQuery()");
                        indentation++;
                        {

                            foreach (var componentTypeSymbol in allReadWrite)
                            {
                                sb.AppendLine(indentation, $".WithAll<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
                            }

                            foreach (var componentTypeSymbol in allReadOnly)
                            {
                                sb.AppendLine(indentation, $".WithAll<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
                            }

                            foreach (var componentTypeSymbol in anyReadWrite)
                            {
                                sb.AppendLine(indentation, $".WithAny<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
                            }

                            foreach (var componentTypeSymbol in anyReadOnly)
                            {
                                sb.AppendLine(indentation, $".WithAny<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
                            }

                            foreach (var componentTypeSymbol in none)
                            {
                                sb.AppendLine(indentation, $".WithNone<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
                            }

                            sb.AppendLine(indentation, $".Update();");
                        }
                        indentation--;
                        sb.AppendLine(indentation, $"world.ChunkQueries[typeof({queryNamedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})] = chunkQuery;");
                    }
                    indentation--;
                    sb.AppendLine(indentation, "}");
                    
                    sb.AppendLine(indentation, $"return new(chunkQuery.Chunks);");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();
                
                #region Linq

                CreateMethods(sb, ref indentation, queryNamedTypeSymbol, allReadWrite, allReadOnly, anyReadWrite, anyReadOnly);

                #endregion
            }
            indentation--;
            sb.AppendLine(indentation, "}");
        }
        
        public static void FindContainingTypes(INamedTypeSymbol namedTypeSymbol, ref List<INamedTypeSymbol> containingTypes)
        {
            while (true)
            {
                if (namedTypeSymbol.ContainingType is null)
                {
                    return;
                }

                containingTypes.Add(namedTypeSymbol.ContainingType);
                namedTypeSymbol = namedTypeSymbol.ContainingType;
            }
        }
    }
}