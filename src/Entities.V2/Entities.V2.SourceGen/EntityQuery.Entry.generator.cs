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
        public static void CreateEntry(StringBuilder sb,
            ref int indentation,
            HashSet<INamedTypeSymbol> allReadWrite,
            HashSet<INamedTypeSymbol> allReadOnly,
            HashSet<INamedTypeSymbol> anyReadWrite,
            HashSet<INamedTypeSymbol> anyReadOnly)
        {
            sb.AppendLine(indentation, $"public ref struct {ConstEntry}");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"private {ConstChunk}? Chunk;");
                sb.AppendLine(indentation, $"private int Index;");
                sb.AppendLine();
                sb.AppendLine(indentation, $"private Span<{ConstEntity}> Entities;");
                sb.AppendLine(indentation, $"public {ConstEntity} Entity => Entities[Index];");
                sb.AppendLine();
                sb.AppendLine(indentation, $"public bool HasComponent<T>() where T : {ConstIEntityComponent} => Chunk?.HasComponent<T>() ?? false;");
                sb.AppendLine();
                sb.AppendLine(indentation, $"public ref T GetComponent<T>() where T : unmanaged, {ConstIEntityComponent} => ref Chunk.GetComponents<T>()[Index];");
                sb.AppendLine();
                sb.AppendLine(indentation, $"public ref T GetManagedComponent<T>() where T : {ConstIEntityComponent} => ref Chunk.GetManagedComponents<T>()[Index];");
                sb.AppendLine();
                
                foreach (var componentTypeSymbol in allReadWrite)
                {
                    sb.AppendLine(indentation, $"private Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                    sb.AppendLine(indentation, $"public ref {componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentTypeSymbol.Name} => ref {componentTypeSymbol.Name}Span[Index];");
                }
            
                foreach (var componentTypeSymbol in allReadOnly)
                {
                    sb.AppendLine(indentation, $"private Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                    sb.AppendLine(indentation, $"public {componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentTypeSymbol.Name} => {componentTypeSymbol.Name}Span[Index];");
                }
            
                foreach (var componentTypeSymbol in anyReadWrite)
                {
                    sb.AppendLine(indentation, $"private Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                    sb.AppendLine(indentation, $"public ref {componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentTypeSymbol.Name} => ref {componentTypeSymbol.Name}Span[Index];");
                }
            
                foreach (var componentTypeSymbol in anyReadOnly)
                {
                    sb.AppendLine(indentation, $"private Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span;");
                    sb.AppendLine(indentation, $"public {componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentTypeSymbol.Name} => {componentTypeSymbol.Name}Span[Index];");
                }
                
                sb.AppendLine();
                
                List<string> componentArgs = new();
                var indentationString = new string('\t', indentation + 1);
                foreach (var componentTypeSymbol in allReadWrite)
                {
                    componentArgs.Add($"{indentationString}Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span");
                }
                
                foreach (var componentTypeSymbol in allReadOnly)
                {
                    componentArgs.Add($"{indentationString}Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span");
                }
                
                foreach (var componentTypeSymbol in anyReadWrite)
                {
                    componentArgs.Add($"{indentationString}Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span");
                }
                
                foreach (var componentTypeSymbol in anyReadOnly)
                {
                    componentArgs.Add($"{indentationString}Span<{componentTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentTypeSymbol.Name}Span");
                }

                sb.Append(indentation, $"public {ConstEntry}({ConstChunk}? chunk, Span<{ConstEntity}> entities, int index");
                if (componentArgs.Count > 0)
                {
                    sb.AppendLine(",");
                    sb.Append(string.Join(",\n", componentArgs));
                }
                sb.AppendLine(")");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"Chunk = chunk;");
                    sb.AppendLine(indentation, $"Index = index;");
                    sb.AppendLine(indentation, $"Entities = entities;");
                    
                    foreach (var componentTypeSymbol in allReadWrite)
                    {
                        sb.AppendLine(indentation, $"this.{componentTypeSymbol.Name}Span = {componentTypeSymbol.Name}Span;");
                    }
                
                    foreach (var componentTypeSymbol in allReadOnly)
                    {
                        sb.AppendLine(indentation, $"this.{componentTypeSymbol.Name}Span = {componentTypeSymbol.Name}Span;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadWrite)
                    {
                        sb.AppendLine(indentation, $"this.{componentTypeSymbol.Name}Span = {componentTypeSymbol.Name}Span;");
                    }
                
                    foreach (var componentTypeSymbol in anyReadOnly)
                    {
                        sb.AppendLine(indentation, $"this.{componentTypeSymbol.Name}Span = {componentTypeSymbol.Name}Span;");
                    }
                }
                indentation--;
                sb.AppendLine(indentation, "}");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
        }
    }
}