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
        public static void CreateMethods(StringBuilder sb,
            ref int indentation,
            INamedTypeSymbol queryTypeSymbol,
            HashSet<INamedTypeSymbol> allReadWrite,
            HashSet<INamedTypeSymbol> allReadOnly,
            HashSet<INamedTypeSymbol> anyReadWrite,
            HashSet<INamedTypeSymbol> anyReadOnly)
        {
            var queryTypeString = queryTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            sb.AppendLine(indentation, $"public {queryTypeString} Where({ConstQueryPredicate} predicate)");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"return new {queryTypeString}(Chunks, Predicate + predicate);");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine();
            
            sb.AppendLine(indentation, $"public bool TryGetFirst(out {ConstEntry} entry)");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var enumerator = {ConstQueryGetEnumerator}();");
                sb.AppendLine(indentation, $"var moveNext = enumerator.MoveNext();");
                sb.AppendLine(indentation, $"entry = moveNext ? enumerator.Current : default;");
                sb.AppendLine(indentation, $"return moveNext;");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine(indentation, $"public bool TryGetFirst({ConstQueryPredicate} predicate, out {ConstEntry} entry)");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var newQuery = this.Where(predicate);");
                sb.AppendLine(indentation, $"var enumerator = newQuery.{ConstQueryGetEnumerator}();");
                sb.AppendLine(indentation, $"var moveNext = enumerator.MoveNext();");
                sb.AppendLine(indentation, $"entry = moveNext ? enumerator.Current : default;");
                sb.AppendLine(indentation, $"return moveNext;");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine(indentation, $"public bool Any()");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var enumerator = {ConstQueryGetEnumerator}();");
                sb.AppendLine(indentation, $"return enumerator.MoveNext();");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine(indentation, $"public bool Any({ConstQueryPredicate} predicate)");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var newQuery = this.Where(predicate);");
                sb.AppendLine(indentation, $"var enumerator = newQuery.{ConstQueryGetEnumerator}();");
                sb.AppendLine(indentation, $"return enumerator.MoveNext();");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine(indentation, $"public int Count()");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var count = 0;");
                sb.AppendLine(indentation, $"foreach (var entry in this)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"count++;");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                sb.AppendLine(indentation, $"return count;");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
            
            sb.AppendLine(indentation, $"public int Count({ConstQueryPredicate} predicate)");
            sb.AppendLine(indentation, "{");
            indentation++;
            {
                sb.AppendLine(indentation, $"var newQuery = this.Where(predicate);");
                
                sb.AppendLine(indentation, $"var count = 0;");
                sb.AppendLine(indentation, $"foreach (var entry in newQuery)");
                sb.AppendLine(indentation, "{");
                indentation++;
                {
                    sb.AppendLine(indentation, $"count++;");
                }
                indentation--;
                sb.AppendLine(indentation, "}");
                
                sb.AppendLine(indentation, $"return count;");
            }
            indentation--;
            sb.AppendLine(indentation, "}");
        }
    }
}