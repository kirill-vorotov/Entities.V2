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

namespace kv.Entities.V2
{
    public readonly struct TypeInfo
    {
        public readonly int Id;
        public readonly Type Type;
        public readonly int Size;
        public readonly int Alignment;
        public readonly bool HasFields;
        public readonly bool IsUnmanaged;
        public readonly bool IsGroupComponent;

        public bool IsZeroSized => IsUnmanaged && !HasFields;

        public TypeInfo(int id, Type type, int size, int alignment, bool hasFields, bool isUnmanaged, bool isGroupComponent)
        {
            Id = id;
            Type = type;
            Size = size;
            Alignment = alignment;
            HasFields = hasFields;
            IsUnmanaged = isUnmanaged;
            IsGroupComponent = isGroupComponent;
        }
    }
}