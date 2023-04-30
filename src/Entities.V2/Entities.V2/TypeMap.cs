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
using System;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public class TypeMap<TValue>
    {
        public static class TypeIndex<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            public static readonly int Value = Interlocked.Increment(ref _lastIndex);
        }

        internal struct Entry
        {
            public TValue? Value;
            public bool HasValue;

            public Entry(TValue value, bool hasValue = true)
            {
                Value = value;
                HasValue = hasValue;
            }
        }
        
        public ref struct Enumerator
        {
            internal Entry[] Entries;
            internal int CurrentIndex;

            internal Enumerator(Entry[] entries)
            {
                Entries = entries;
                CurrentIndex = -1;
            }

            public TValue? Current => Entries[CurrentIndex].Value;

            public bool MoveNext()
            {
                do
                {
                    if (++CurrentIndex >= Entries.Length)
                    {
                        return false;
                    }
                } while (!Entries[CurrentIndex].HasValue);

                return true;
            }
        }
        
        // ReSharper disable once StaticMemberInGenericType
        private static volatile int _lastIndex = -1;
        internal Entry[] Entries = new Entry[DefaultCapacity];
        
        public const int DefaultCapacity = 16;

        public TypeMap()
        {
            EnsureCapacity();
        }

        public int Capacity => Entries.Length;
        public int Count => Entries.Count(e => e.HasValue);

        [PublicAPI]
        public void Add<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            if (Entries[index].HasValue)
            {
                throw new Exception("Key already exists.");
            }

            Entries[index].HasValue = true;
            Entries[index].Value = value;
        }

        [PublicAPI]
        public bool TryAdd<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            if (Entries[index].HasValue)
            {
                return false;
            }
            
            Entries[index].HasValue = true;
            Entries[index].Value = value;
            return true;
        }

        [PublicAPI]
        public void Set<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            Entries[index].HasValue = true;
            Entries[index].Value = value;
        }

        [PublicAPI]
        public bool Remove<TKey>()
        {
            var index = TypeIndex<TKey>.Value;
            if (index < 0 || index >= Entries.Length || !Entries[index].HasValue)
            {
                return false;
            }

            Entries[index].HasValue = false;
            Entries[index].Value = default;
            return true;
        }

        [PublicAPI]
        public bool ContainsKey<TKey>()
        {
            var index = TypeIndex<TKey>.Value;
            return index >= 0 && index < Entries.Length && Entries[index].HasValue;
        }

        [PublicAPI]
        public bool TryGetValue<TKey>(out TValue? value)
        {
            var index = TypeIndex<TKey>.Value;
            if (index < 0 || index >= Entries.Length || !Entries[index].HasValue)
            {
                value = default;
                return false;
            }

            value = Entries[index].Value;
            return true;
        }

        [PublicAPI]
        public void Clear()
        {
            Array.Clear(Entries, 0, Entries.Length);
        }

        public Enumerator GetEnumerator() => new(Entries);

        private void EnsureCapacity()
        {
            var capacity = _lastIndex + 1;

            if (capacity <= Entries.Length)
            {
                return;
            }

            if (capacity < DefaultCapacity)
            {
                capacity = DefaultCapacity;
            }
            else
            {
                capacity <<= 1;
            }
            
            Array.Resize(ref Entries, capacity);
        }
    }
}