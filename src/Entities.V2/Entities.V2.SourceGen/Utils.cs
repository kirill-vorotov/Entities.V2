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
using Microsoft.CodeAnalysis;

namespace kv.Entities.V2.SourceGen
{
    public static class Utils
    {
        public static void GetNamedTypes(IEnumerable<INamespaceSymbol> namespaceSymbols, ref List<INamedTypeSymbol> namedTypeSymbols)
        {
            foreach (var namespaceOrTypeSymbol in namespaceSymbols)
            {
                if (!namespaceOrTypeSymbol.IsNamespace)
                {
                    continue;
                }

                foreach (var typeMember in namespaceOrTypeSymbol.GetTypeMembers())
                {
                    AddNamedType(typeMember, ref namedTypeSymbols);
                }
                GetNamedTypes(namespaceOrTypeSymbol.GetNamespaceMembers(), ref namedTypeSymbols);
            }
        }

        public static void AddNamedType(INamedTypeSymbol namedTypeSymbol, ref List<INamedTypeSymbol> namedTypeSymbols)
        {
            namedTypeSymbols.Add(namedTypeSymbol);
            foreach (var typeMember in namedTypeSymbol.GetTypeMembers())
            {
                AddNamedType(typeMember, ref namedTypeSymbols);
            }
        }
        
        public static bool ImplementsInterface(INamedTypeSymbol namedTypeSymbol, string targetName)
        {
            if (namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetName)
            {
                return false;
            }

            foreach (var @interface in namedTypeSymbol.AllInterfaces)
            {
                if (@interface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}