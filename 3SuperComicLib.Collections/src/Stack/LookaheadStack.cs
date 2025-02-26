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

namespace SuperComicLib.Collections
{
    [System.Diagnostics.DebuggerDisplay("Count = {size}")]
    public class LookaheadStack<T> : IDisposable, ICollection<T>, IStack<T>
    {
        private const int defaultCapacity = 8;

        protected T[] arr;
        protected int size;

        public LookaheadStack() => IncreaseCapacity(defaultCapacity);

        public LookaheadStack(int initalCapacity)
        {
            if (initalCapacity <= 0)
                IncreaseCapacity(defaultCapacity);
            else
                arr = new T[initalCapacity];
        }

        public LookaheadStack(T[] source)
        {
            int x;
            if (source == null || (size = x = source.Length) <= 0)
            {
                IncreaseCapacity(defaultCapacity);
                return;
            }

            T[] vs = new T[x];

            while (--x >= 0)
                vs[x] = source[x];

            arr = vs;
        }

        public int Count => size;
        public int Capacity => arr.Length;
        public bool IsReadOnly => true;

        public void Push(T item)
        {
            if (size == arr.Length)
                IncreaseCapacity(Arrays.GetNextSize(size));

            arr[size++] = item;
        }

        public T Pop()
        {
            T result = arr[--size];
            arr[size] = default;
            return result;
        }

        public void ReplaceLast(T newItem) => arr[size - 1] = newItem;

        public T PopOrDefault()
        {
            int idx = size - 1;
            if (idx < 0)
                return default;

            T result = arr[idx];
            arr[size = idx] = default;
            return result;
        }

        public void Remove(int count)
        {
            if (count >= size)
                Clear();
            else
            {
                int n = size;
                int x = n - count;
                while (--n >= x)
                    arr[n] = default;

                size = x;
            }
        }

        public void RemoveLast() => arr[--size] = default;

        public T Peek() => arr[size - 1];

        public T Peek(int lookahead) => arr[size - lookahead - 1];

        public T PeekOrDefault()
        {
            int idx;
            return
                (idx = size - 1) < 0
                ? default
                : arr[idx];
        }

        public T PeekOrDefault(int lookahead)
        {
            int idx;
            return
                (idx = size - lookahead - 1) < 0
                ? default
                : arr[idx];
        }

        public void Clear()
        {
            Array.Clear(arr, 0, size);
            size = 0;
        }

        protected void IncreaseCapacity(int nextsize)
        {
            ref T[] now = ref arr;
            T[] newarr = new T[nextsize];

            int x = size;
            while (--x >= 0)
                newarr[x] = now[x];

            now = newarr;
        }

        public virtual T[] ToArray()
        {
            int sz;
            if ((sz = size) == 0)
                return Array.Empty<T>();

            T[] src = arr;
            T[] res = new T[sz];

            int n = sz;
            for (int x = 0; x < sz;)
                res[x++] = src[--n];

            return res;
        }

        #region eunmerable
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)arr).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region interface
        void ICollection<T>.Add(T item) => Push(item);

        bool ICollection<T>.Contains(T item) => false;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) { }

        bool ICollection<T>.Remove(T item) => false;
        #endregion
        #endregion

        #region dispose
        protected virtual void Dispose(bool disposing)
        {
            arr = null;
            size = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}