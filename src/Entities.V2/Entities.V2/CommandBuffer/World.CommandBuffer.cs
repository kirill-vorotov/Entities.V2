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
using System.Collections.Concurrent;
using System.Threading;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public sealed partial class World
    {
        internal ConcurrentDictionary<int, CommandBuffer> CommandBuffers = new();

        [PublicAPI]
        public CommandBuffer GetCommandBuffer()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            if (!CommandBuffers.TryGetValue(currentThreadId, out var commandBuffer))
            {
                commandBuffer = new CommandBuffer(currentThreadId, ComponentTypes);
                CommandBuffers[currentThreadId] = commandBuffer;
            }

            return commandBuffer;
        }

        [PublicAPI]
        public void ExecuteCommandBuffers()
        {
            foreach (var commandBuffer in CommandBuffers.Values)
            {
                // TODO
                commandBuffer.Clear();
            }
            
            UpdateCachedQueries();
        }
    }
}