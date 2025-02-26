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

#if X64
#pragma warning disable IDE1006 // 명명 스타일
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SuperComicLib.Collections
{
    unsafe partial struct _index_linked_vector<T>
    {        
        // long next
        // long prev
        // T value
        private byte* _ptr; // first 4 byte = length
        private byte* _head;
        private long _free;
        private long _size;

#region constructor
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public _index_linked_vector(long initCapacity)
        {
            _ptr = null;
            _head = null;
            _free = NULL_PTR;
            _size = 0;

            increaseCapacity(initCapacity);
        }
#endregion

#region size & capacity
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long capacity() => *(long*)(_ptr - sizeof(long));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long size() => _size;
#endregion

#region indexer
        public _index_node<T> this[long raw_index] => new _index_node<T>(_get(_ptr, raw_index));

        /// <exception cref="ArgumentOutOfRangeException">out of range</exception>
        /// <exception cref="NullReferenceException">tried to access a deleted element</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public _index_node<T> at(long raw_index)
        {
            if ((ulong)raw_index >= (ulong)capacity())
                throw new ArgumentOutOfRangeException(nameof(raw_index));

            var v = this[raw_index];
            if (v.next < 0 || // deleted node
                (v.next | v.prev) == 0 && (ulong)raw_index >= (ulong)_size)
                throw new NullReferenceException(nameof(raw_index));

            return v;
        }
#endregion

#region insert
        private byte* p_insert_before(byte* baseNode, in T value)
        {
            long tidx_;
            if (_free >= 0)
            {
                tidx_ = _free;
                _free = *(long*)_get_vp(_ptr, tidx_);
            }
            else if ((tidx_ = _size) >= capacity()) // no space
            {
                var baseIndex = _index(baseNode, _ptr);

                increaseCapacity(_size + 1);
                baseNode = _get(_ptr, baseIndex);
            }

            var list_ = _ptr;

            var newNode_ = _get(list_, tidx_);
            *v_value(newNode_) = value;

            *v_next(newNode_) = _index(baseNode, list_);
            *v_prev(newNode_) = *v_prev(baseNode);

            *(long*)_get(list_, *v_prev(baseNode)) = tidx_;
            *v_prev(baseNode) = tidx_;

            _size++;

            return newNode_;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long index_of(_index_node<T> node) => _index(node._ptr, _ptr);
#endregion

#region erase
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void erase_unsafe(_index_node<T> node)
        {
            var list_ = _ptr;

            var pnode = node._ptr;

            *(long*)_get_vp(list_, *v_next(pnode)) = *v_prev(pnode);
            *(long*)_get(list_, *v_prev(pnode)) = *v_next(pnode);

            if (_head == pnode)
                _head = _get(list_, *v_next(pnode));

            if (--_size == 0) // empty list
            {
                _head = _ptr;
                _free = NULL_PTR;
            }
            else
            {
                *v_prev(pnode) = _free;
                _free = _index(pnode, list_);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void erase(_index_node<T> node)
        {
            validate_node(node);
            erase_unsafe(node);
        }
#endregion

#region private method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void validate_node(_index_node<T> node)
        {
            long index = _index(node._ptr, _ptr);
            if ((ulong)index >= (ulong)capacity())
                throw new ArgumentOutOfRangeException(nameof(node));

            if (node.next < 0 ||
                (node.next | node.prev) == 0 && (ulong)index >= (ulong)_size)
                throw new InvalidOperationException($"Dereference to {nameof(NULL_PTR)}");
        }

        private void increaseCapacity(long req_size)
        {
            long sz = (long)CMath.Max((ulong)req_size, 4u);

            long new_sizeInBytes = _sizeInBytes(Arrays.GetNextSize(sz));
            var np = (byte*)Marshal.AllocHGlobal((IntPtr)(new_sizeInBytes + sizeof(long)));

            *(long*)np = sz;

            var vs_np = np + sizeof(long);

            ulong copysize = (ulong)_sizeInBytes(_size);
            Buffer.MemoryCopy(_ptr, vs_np, copysize, copysize);

            MemoryBlock.Clear64(_get(vs_np, _size), (ulong)new_sizeInBytes - copysize);

            if (_ptr != null)
                Marshal.FreeHGlobal((IntPtr)(_ptr - sizeof(long)));

            _head = vs_np + _index(_head, _ptr);
            _ptr = vs_np;
        }
#endregion

#region dispose
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_ptr != null)
                Marshal.FreeHGlobal((IntPtr)(_ptr - sizeof(long)));
        }
#endregion

#region helper methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long _sizeInBytes(long size) => (16 + sizeof(T)) * size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long _index(byte* p, byte* b) => (p - b) / (16 + sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* _get(byte* p, long idx) => p + (16 + sizeof(T)) * idx;
        // get, value prev (pivot)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* _get_vp(byte* p, long idx) => _get(p, idx) + sizeof(long);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long* v_next(byte* p) => (long*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long* v_prev(byte* p) => (long*)(p + sizeof(long));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T* v_value(byte* p) => (T*)(p + sizeof(long) + sizeof(long));
#endregion
    }
}
#endif