﻿// MIT License
//
// Copyright (c) 2019-2022 SuperComic (ekfvoddl3535@naver.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SuperComicLib.Collections
{
    public sealed class LoopArray<T> : IEnumerable<T>
    {
        private const int minimum_size = 1;

        private readonly T[] m_array;
        private int m_readpos;
        private int m_writepos;
        private int m_size;

        public LoopArray() : this(minimum_size)
        {
        }

        public LoopArray(int size)
        {
            m_readpos = 0;
            m_writepos = 0;

            m_array = new T[Math.Max(minimum_size, size)];
        }

        public T this[int idx] => m_array[CMath.Abs(m_readpos + idx) % m_array.Length];

        public int Capacity => m_array.Length;

        public int Count => m_size;

        public int FirstItemIndex => m_readpos;

        public RawBuffer GetBuffer() => new RawBuffer(m_array, m_readpos);

        public void Write(T value)
        {
            int len = m_array.Length;
            if (m_size == len)
                m_readpos = (m_readpos + 1) % len;
            else
                m_size++;

            int curr = m_writepos;
            m_array[curr] = value;

            m_writepos = (curr + 1) % len;
        }

        public T[] ToArray()
        {
            if (m_size == 0)
                return Array.Empty<T>();

            T[] arr = m_array;
            int len = arr.Length;
            int r = m_readpos;

            T[] vs = new T[m_size];
            for (int x = 0; x < len; x++)
                vs[x] = arr[(r + x) % len];

            return vs;
        }

        public void Clear()
        {
            Array.Clear(m_array, 0, m_array.Length);
            m_size = m_readpos = m_writepos = 0;
        }

        #region enumerable
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<T>
        {
            private LoopArray<T> inst;
            private int m_init_pos;
            private int m_idx;
            private int m_size;

            public Enumerator(LoopArray<T> inst)
            {
                this.inst = inst;

                m_init_pos = inst.m_writepos;
                m_size = inst.m_size;

                m_idx = -1;
            }

            public T Current => inst[m_idx];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (inst.m_writepos != m_init_pos)
                    throw new InvalidOperationException();

                return ++m_idx < m_size;
            }

            public void Reset() => m_idx = -1;

            public void Dispose()
            {
                inst = null;
                m_init_pos = m_idx = m_size = 0;
            }
        }
        #endregion

        #region ref enumerate
        public readonly ref struct RawBuffer
        {
            private readonly T[] m_arr;
            private readonly int m_pos;

            internal RawBuffer(T[] arr, int pos)
            {
                m_arr = arr;
                m_pos = pos;
            }

            public ref T this[int idx]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref m_arr[(m_pos + idx) % m_arr.Length];
            }
        }
        #endregion
    }
}
