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
        
        // ReSharper disable once StaticMemberInGenericType
        private static volatile int _lastIndex = -1;
        private Entry[] _entries = new Entry[DefaultCapacity];
        
        public const int DefaultCapacity = 16;

        public TypeMap()
        {
            EnsureCapacity();
        }

        public int Capacity => _entries.Length;
        public int Count => _entries.Count(e => e.HasValue);

        [PublicAPI]
        public void Add<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            if (_entries[index].HasValue)
            {
                throw new Exception("Key already exists.");
            }

            _entries[index].HasValue = true;
            _entries[index].Value = value;
        }

        [PublicAPI]
        public bool TryAdd<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            if (_entries[index].HasValue)
            {
                return false;
            }
            
            _entries[index].HasValue = true;
            _entries[index].Value = value;
            return true;
        }

        [PublicAPI]
        public void Set<TKey>(TValue value)
        {
            var index = TypeIndex<TKey>.Value;
            EnsureCapacity();
            _entries[index].HasValue = true;
            _entries[index].Value = value;
        }

        [PublicAPI]
        public bool Remove<TKey>()
        {
            var index = TypeIndex<TKey>.Value;
            if (index < 0 || index >= _entries.Length || !_entries[index].HasValue)
            {
                return false;
            }

            _entries[index].HasValue = false;
            _entries[index].Value = default;
            return true;
        }

        [PublicAPI]
        public bool ContainsKey<TKey>()
        {
            var index = TypeIndex<TKey>.Value;
            return index >= 0 && index < _entries.Length && _entries[index].HasValue;
        }

        [PublicAPI]
        public bool TryGetValue<TKey>(out TValue? value)
        {
            var index = TypeIndex<TKey>.Value;
            if (index < 0 || index >= _entries.Length || !_entries[index].HasValue)
            {
                value = default;
                return false;
            }

            value = _entries[index].Value;
            return true;
        }

        private void EnsureCapacity()
        {
            var capacity = _lastIndex + 1;

            if (capacity <= _entries.Length)
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
            
            Array.Resize(ref _entries, capacity);
        }
    }
}