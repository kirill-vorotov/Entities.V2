﻿/*
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
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public static class Debug
    {
        [System.Diagnostics.Conditional("DEBUG"), AssertionMethod, ContractAnnotation("condition:false=>halt")]
        public static void Assert([DoesNotReturnIf(false)] bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }

        [System.Diagnostics.Conditional("DEBUG"), AssertionMethod, ContractAnnotation("condition:false=>halt")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        [System.Diagnostics.Conditional("DEBUG"), AssertionMethod, ContractAnnotation("condition:false=>halt")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string message, string detailMessage)
        {
            System.Diagnostics.Debug.Assert(condition, message, detailMessage);
        }

        [System.Diagnostics.Conditional("DEBUG"), AssertionMethod, ContractAnnotation("condition:false=>halt")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string message, string detailMessageFormat, params object[] args)
        {
            System.Diagnostics.Debug.Assert(condition, message, detailMessageFormat, args);
        }
    }
}