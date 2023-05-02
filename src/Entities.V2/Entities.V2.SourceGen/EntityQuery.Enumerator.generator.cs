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

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace kv.Entities.V2.SourceGen
{
    public partial class EntityQuery
    {
        public static void CreateEnumerator(StringBuilder sb,
            ref int indentation,
            HashSet<INamedTypeSymbol> allReadWrite,
            HashSet<INamedTypeSymbol> allReadOnly,
            HashSet<INamedTypeSymbol> anyReadWrite,
            HashSet<INamedTypeSymbol> anyReadOnly)
        {
            sb.AppendLine(indentation, $"public ref partial struct {ConstEnumerator}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"public {ConstIReadOnlyList}<{ConstChunk}> Chunks;");
                sb.AppendLine(indentation, $"public {ConstQueryPredicate}? Predicate;");
                sb.AppendLine();
                sb.AppendLine(indentation, $"public int CurrentChunkIndex;");
                sb.AppendLine(indentation, $"public int CurrentIndexInChunk;");
                sb.AppendLine();
                sb.AppendLine(indentation, $"public Span<{ConstEntity}> Entities;");
                
                foreach (var componentTypeSymbol in allReadWrite)
                {
                    sb.AppendLine(indentation, $"public Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                }
                
                foreach (var componentTypeSymbol in allReadOnly)
                {
                    sb.AppendLine(indentation, $"public Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                }
                
                foreach (var componentTypeSymbol in anyReadWrite)
                {
                    sb.AppendLine(indentation, $"public Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                }
                
                foreach (var componentTypeSymbol in anyReadOnly)
                {
                    sb.AppendLine(indentation, $"public Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                }
                
                sb.AppendLine();
                
                #region Ctor
                
                sb.AppendLine(indentation, $"public {ConstEnumerator}({ConstIReadOnlyList}<{ConstChunk}> chunks, {ConstQueryPredicate}? predicate = default)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"Chunks = chunks;");
                    sb.AppendLine(indentation, $"Predicate = predicate;");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"CurrentChunkIndex = -1;");
                    sb.AppendLine(indentation, $"CurrentIndexInChunk = -1;");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"Entities = Span<{ConstEntity}>.Empty;");
                    
                    foreach (var componentTypeSymbol in allReadWrite)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in allReadOnly)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadWrite)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadOnly)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();
                
                #region MoveNext()
                
                sb.AppendLine(indentation, $"public bool {ConstEnumeratorMoveNext}()");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, "if (Chunks is not { Count: > 0 } || CurrentChunkIndex >= Chunks.Count)");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, "return false;");
                    }
                    indentation--;
                    sb.AppendLine(indentation, "}");
                    sb.AppendLine();
                    sb.AppendLine(indentation, "bool foundEntity;");
                    sb.AppendLine();
                    sb.AppendLine(indentation, "do");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, "var updateChunk = false;");
                        sb.AppendLine();
                        sb.AppendLine(indentation, "++CurrentIndexInChunk;");
                        sb.AppendLine(indentation, "if (CurrentChunkIndex < 0 || Chunks[CurrentChunkIndex] is not { Count: > 0 } || CurrentIndexInChunk >= Chunks[CurrentChunkIndex].Count)");
                        sb.AppendLine(indentation, "{");
                        indentation++;
                        {
                            sb.AppendLine(indentation, "++CurrentChunkIndex;");
                            sb.AppendLine(indentation, "CurrentIndexInChunk = 0;");
                            sb.AppendLine(indentation, "updateChunk = true;");
                        }
                        indentation--;
                        sb.AppendLine(indentation, "}");
                        sb.AppendLine();
                        sb.AppendLine(indentation, "if (CurrentChunkIndex >= Chunks.Count)");
                        sb.AppendLine(indentation, "{");
                        indentation++;
                        {
                            sb.AppendLine(indentation, "return false;");
                        }
                        indentation--;
                        sb.AppendLine(indentation, "}");
                        sb.AppendLine();
                        sb.AppendLine(indentation, "updateChunk &= Chunks[CurrentChunkIndex] is { Count: > 0 };");
                        sb.AppendLine(indentation, "if (updateChunk)");
                        sb.AppendLine(indentation, "{");
                        indentation++;
                        {
                            sb.AppendLine(indentation, "Update(Chunks[CurrentChunkIndex]);");
                        }
                        indentation--;
                        sb.AppendLine(indentation, "}");
                        sb.AppendLine();
                        sb.AppendLine(indentation, "foundEntity = Chunks[CurrentChunkIndex] is { Count: > 0 } &&");
                        indentation += 3;
                        sb.AppendLine(indentation, "CurrentIndexInChunk < Chunks[CurrentChunkIndex].Count &&");
                        sb.AppendLine(indentation, "CheckPredicate(Current);");
                        indentation -= 3;
                    }
                    indentation--;
                    sb.AppendLine(indentation, "} while (!foundEntity);");
                    sb.AppendLine();
                    sb.AppendLine(indentation, "return true;");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();

                #region Current
                
                sb.AppendLine(indentation, $"public {ConstEntry} {ConstEnumeratorCurrent} =>");
                indentation++;
                {
                    sb.Append(indentation, $"new(Chunks[CurrentChunkIndex], Entities, CurrentIndexInChunk");
                    indentation++;
                    {
                        List<string> componentArgs = new();
                        var indentationString = new string('\t', indentation);
                        foreach (var componentTypeSymbol in allReadWrite)
                        {
                            componentArgs.Add($"{indentationString}{componentTypeSymbol.Name}Span");
                        }
                
                        foreach (var componentTypeSymbol in allReadOnly)
                        {
                            componentArgs.Add($"{indentationString}{componentTypeSymbol.Name}Span");
                        }
                
                        foreach (var componentTypeSymbol in anyReadWrite)
                        {
                            componentArgs.Add($"{indentationString}{componentTypeSymbol.Name}Span");
                        }
                
                        foreach (var componentTypeSymbol in anyReadOnly)
                        {
                            componentArgs.Add($"{indentationString}{componentTypeSymbol.Name}Span");
                        }
                        
                        if (componentArgs.Count > 0)
                        {
                            sb.AppendLine(",");
                            sb.Append(string.Join(",\n", componentArgs));
                        }
                        
                        sb.AppendLine(");");
                    }
                    indentation--;
                }
                indentation--;
                
                #endregion

                sb.AppendLine();

                #region Reset()
                
                sb.AppendLine(indentation, $"public void {ConstEnumeratorReset}()");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"CurrentChunkIndex = -1;");
                    sb.AppendLine(indentation, $"CurrentIndexInChunk = -1;");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"Entities = Span<{ConstEntity}>.Empty;");
                    
                    foreach (var componentTypeSymbol in allReadWrite)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in allReadOnly)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadWrite)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadOnly)
                    {
                        sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;");
                    }
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
                
                sb.AppendLine();

                #region Update()
                
                sb.AppendLine(indentation, $"public void {ConstEnumeratorUpdate}({ConstChunk}? chunk)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"if (chunk is null)");
                    sb.AppendLine(indentation, "{");
                    indentation++;
                    {
                        sb.AppendLine(indentation, $"return;");
                    }
                    indentation--;
                    sb.AppendLine(indentation, "}");
                    sb.AppendLine();
                    sb.AppendLine(indentation, $"Entities = Chunks[CurrentChunkIndex].Entities.AsSpan();");
                    
                    foreach (var componentTypeSymbol in allReadWrite)
                    {
                        if (componentTypeSymbol.IsUnmanagedType)
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                        else
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetManagedComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                    }
                
                    foreach (var componentTypeSymbol in allReadOnly)
                    {
                        if (componentTypeSymbol.IsUnmanagedType)
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                        else
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetManagedComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                    }
                
                    foreach (var componentTypeSymbol in anyReadWrite)
                    {
                        if (componentTypeSymbol.IsUnmanagedType)
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                        else
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetManagedComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                    }
                
                    foreach (var componentTypeSymbol in anyReadOnly)
                    {
                        if (componentTypeSymbol.IsUnmanagedType)
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                        else
                        {
                            sb.AppendLine(indentation, $"{componentTypeSymbol.Name}Span = chunk.GetManagedComponents<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
                        }
                    }
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion

                sb.AppendLine();

                #region CheckPredicate()
                
                sb.AppendLine(indentation, $"public bool {ConstEnumeratorCheckPredicate}({ConstEntry} entry)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"return Predicate?.Invoke(entry) ?? true;");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                #endregion
            }
            indentation--;
            sb.AppendLine(indentation, "}");
        }
    }
}