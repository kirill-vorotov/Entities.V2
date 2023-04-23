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
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public struct BitMask
    {
        public const int BitShiftPerInt64 = 6;
        public Memory<ulong> Buffer;

        public BitMask(Memory<ulong> buffer)
        {
            Buffer = buffer;
        }
        
        [PublicAPI]
        public bool this[int index]
        {
            get => (Buffer.Span[index >> BitShiftPerInt64] & (1UL << index)) != 0;
            set
            {
                var mask = 1UL << index;
                ref var num = ref Buffer.Span[index >> BitShiftPerInt64];
                if (value)
                {
                    num |= mask;
                }
                else
                {
                    num &= ~mask;
                }
            }
        }
        
        [PublicAPI]
        public readonly bool IsZero()
        {
            for (var i = 0; i < Buffer.Length; i++)
            {
                if (Buffer.Span[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }
        
        [PublicAPI]
        public void SetAll(bool value)
        {
            if (value)
            {
                Buffer.Span.Fill(~0UL);
            }
            else
            {
                Buffer.Span.Clear();
            }
        }
        
        [PublicAPI]
        public void SetAll(BitMask other)
        {
            var min = Math.Min(Buffer.Length, other.Buffer.Length);
            var span = Buffer.Span;
            var otherSpan = other.Buffer.Span;
            for (var i = 0; i < min; i++)
            {
                span[i] = otherSpan[i];
            }
        }
        
        [PublicAPI]
        public void And(BitMask other)
        {
            var min = Math.Min(Buffer.Length, other.Buffer.Length);
            var span = Buffer.Span;
            var otherSpan = other.Buffer.Span;
            for (var i = 0; i < min; i++)
            {
                span[i] &= otherSpan[i];
            }
        }
        
        [PublicAPI]
        public void Or(BitMask other)
        {
            var min = Math.Min(Buffer.Length, other.Buffer.Length);
            var span = Buffer.Span;
            var otherSpan = other.Buffer.Span;
            for (var i = 0; i < min; i++)
            {
                span[i] |= otherSpan[i];
            }
        }
        
        [PublicAPI]
        public void Xor(BitMask other)
        {
            var min = Math.Min(Buffer.Length, other.Buffer.Length);
            var span = Buffer.Span;
            var otherSpan = other.Buffer.Span;
            for (var i = 0; i < min; i++)
            {
                span[i] ^= otherSpan[i];
            }
        }
        
        [PublicAPI]
        public void Not()
        {
            var span = Buffer.Span;
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = ~span[i];
            }
        }
        
        [PublicAPI]
        public int PopCount()
        {
            var count = 0;
            var span = Buffer.Span;
            for (var i = 0; i < Buffer.Length; i++)
            {
                var value = span[i];
                int c;
                for (c = 0; value > 0; c++)
                {
                    value &= value - 1;
                }

                count += c;
            }
            
            return count;
        }
        
        [PublicAPI]
        public bool Equals(BitMask other)
        {
            if (Buffer.Length != other.Buffer.Length)
            {
                return false;
            }

            var span = Buffer.Span;
            var otherSpan = other.Buffer.Span;
            for (var i = 0; i < Buffer.Length; i++)
            {
                if (span[i] != otherSpan[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferSize(int itemsCount)
        {
            var tmp = (uint)(itemsCount - 1 + (1 << BitShiftPerInt64));
            var bufferLength = (int)(tmp >> BitShiftPerInt64);
            return bufferLength;
        }
    }
}