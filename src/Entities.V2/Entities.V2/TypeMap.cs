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

#nullable enable
using System.Threading;

namespace kv.Entities.V2
{
    public class TypeMap<TValue>
    {
        public ref struct Enumerator
        {
            public TypeInfo Current
            {
                get => throw new System.NotImplementedException();
            }

            public bool MoveNext()
            {
                throw new System.NotImplementedException();
            }
        }
        
        // ReSharper disable once StaticMemberInGenericType
        private static volatile int _lastIndex = -1;
        
        public static class TypeIndex<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly int Index = Interlocked.Increment(ref _lastIndex);
        }
        
        public const int DefaultCapacity = 16;
        
        public int Count => throw new System.NotImplementedException();

        public void Add<TKey>(TValue value)
        {
            throw new System.NotImplementedException();
        }

        public bool TryAdd<TKey>(TValue value)
        {
            throw new System.NotImplementedException();
        }

        public void Set<TKey>(TValue value)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove<TKey>()
        {
            throw new System.NotImplementedException();
        }

        public bool ContainsKey<TKey>()
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValue<TKey>(out TValue value)
        {
            throw new System.NotImplementedException();
        }

        public Enumerator GetEnumerator() => new Enumerator();
    }
}