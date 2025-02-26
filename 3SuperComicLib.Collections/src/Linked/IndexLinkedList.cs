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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SuperComicLib.Collections
{
    // [DebuggerTypeProxy(typeof(IIterableView<>))]
    [DebuggerDisplay("Count = {m_size}")]
    public class IndexLinkedList<T> : ICollection<T>, IReadOnlyCollection<T>, IEnumerable<T>, ILinkedListSlim_Internal<T>
    {
        private const int NULL_PTR = -1;

        private Node[] m_list;
        private int m_head_idx;
        private int m_free_idx; // default -1
        private int m_size;
        private uint m_version;

        #region constructors
        public IndexLinkedList(int capacity)
        {
            m_list = new Node[(int)CMath.Max((uint)capacity, 4u)];
            m_free_idx = -1;
        }

        public IndexLinkedList(IEnumerable<T> collection) : this(4)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var e1 = collection.GetEnumerator();
            while (e1.MoveNext())
                AddLast(e1.Current);
        }

        public IndexLinkedList() : this(4)
        {
        }
        #endregion

        #region properties & indexing
        public int Capacity => m_list.Length;

        public int Count => m_size;

        public NodeIndex<T> First => new NodeIndex<T>(this, m_head_idx);

        public NodeIndex<T> Last => new NodeIndex<T>(this, LastIndex);

        private int LastIndex =>
            m_size != 0
            ? m_list[m_head_idx].prev
            : NULL_PTR;

        public NodeIndex<T> this[int rawIndex]
        {
            get
            {
                ref Node n = ref m_list[rawIndex];
                if (n.next < 0 || // deleted slot (NULL_PTR)
                    (n.next | n.prev) == 0 && (rawIndex != 0 || m_size != 1)) // invalid reference
                    throw new NullReferenceException(nameof(rawIndex));

                return new NodeIndex<T>(this, rawIndex);
            }
        }
        #endregion

        #region internal interface impl
        ref T IByReferenceIndexer_Internal<T>.ByRefValue(int index) => ref m_list[index].value;

        int ILinkedListSlim_Internal<T>.GetNextNode(int node)
        {
            int next = m_list[node].next;
            return
                next < 0 // invalid reference
                ? node
                : next;
        }

        int ILinkedListSlim_Internal<T>.GetPrevNode(int node)
        {
            ref Node n = ref m_list[node];
            return
                n.next < 0 // invalid reference
                ? node
                : n.prev;
        }
        #endregion

        #region add item method
        private int P_InsertBefore(int __node_idx, ref T __value)
        {
            int tidx_;
            if (m_free_idx >= 0)
            {
                tidx_ = m_free_idx;
                m_free_idx = m_list[tidx_].prev;
            }
            else if ((tidx_ = m_size) >= m_list.Length) // no space
                IncreaseCapacity();

            Node[] list_ = m_list;

            ref Node newNode_ = ref list_[tidx_];
            newNode_.value = __value;

            ref Node baseNode_ = ref list_[__node_idx];

            newNode_.next = __node_idx;
            newNode_.prev = baseNode_.prev;

            list_[baseNode_.prev].next = tidx_;
            baseNode_.prev = tidx_;

            m_size++; // added
            m_version++;

            return tidx_;
        }

        public NodeIndex<T> AddLast(T value) => new NodeIndex<T>(this, P_InsertBefore(m_head_idx, ref value));

        public NodeIndex<T> AddFirst(T value) => new NodeIndex<T>(this, m_head_idx = P_InsertBefore(m_head_idx, ref value));

        /// <exception cref="ArgumentException">not match owner of node</exception>
        /// <exception cref="InvalidOperationException">referenced empty slot</exception>
        public NodeIndex<T> AddAfter(NodeIndex<T> node, T value)
        {
            ValidateNodeIndex(ref node);
            return new NodeIndex<T>(this, P_InsertBefore(m_list[node.m_index].next, ref value));
        }

        /// <exception cref="ArgumentException">not match owner of node</exception>
        /// <exception cref="InvalidOperationException">referenced empty slot</exception>
        public NodeIndex<T> AddBefore(NodeIndex<T> node, T value)
        {
            ValidateNodeIndex(ref node);
            int result = P_InsertBefore(node.m_index, ref value);

            if (node.m_index == m_head_idx) // head 위치 이전에 삽입 = AddFirst와 같음
                m_head_idx = result;

            return new NodeIndex<T>(this, result);
        }
        #endregion

        #region remove item & clear method
        private void Internal_RemoveAt(int node_idx)
        {
            Node[] list_ = m_list;

            ref Node target_ = ref list_[node_idx];

            list_[target_.next].prev = target_.prev;
            list_[target_.prev].next = target_.next;
            
            // clear
            target_.value = default;

            if (m_head_idx == node_idx)
                m_head_idx = target_.next;

            m_version++;
            if (--m_size == 0) // empty list
            {
                // reset variables
                m_head_idx = 0;
                m_free_idx = NULL_PTR;
            }
            else
            {
                target_.prev = m_free_idx;
                m_free_idx = node_idx;
            }

            target_.next = NULL_PTR; // flag empty slot
        }

        /// <exception cref="ArgumentException">not match owner of node</exception>
        /// <exception cref="InvalidOperationException">referenced empty slot</exception>
        public void RemoveAt(NodeIndex<T> node)
        {
            ValidateNodeIndex(ref node);
            Internal_RemoveAt(node.m_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T value) => Remove(value, EqualityComparer<T>.Default);

        public bool Remove(T value, IEqualityComparer<T> comparer)
        {
            var node = FindFirst(value, comparer);
            if (node.m_owner != null)
            {
                RemoveAt(node);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T RemoveFirst()
        {
            int head_idx = m_head_idx;
            T result = m_list[head_idx].value;
            Internal_RemoveAt(head_idx);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T RemoveLast()
        {
            int idx = m_list[m_head_idx].prev;
            T result = m_list[idx].value;
            Internal_RemoveAt(idx);

            return result;
        }

        public bool TryRemoveFirst(out T result)
        {
            if (m_size <= 0)
            {
                result = default;
                return false;
            }

            result = RemoveFirst();
            return true;
        }

        public bool TryRemoveLast(out T result)
        {
            if (m_size <= 0)
            {
                result = default;
                return false;
            }

            result = RemoveLast();
            return true;
        }

        public bool TryRemove(NodeIndex<T> node)
        {
            if (node.m_owner != this || node.m_index < 0)
                return false;

            Internal_RemoveAt(node.m_index);
            return true;
        }

        public void Clear()
        {
            Node[] list_ = m_list;

            // reset values
            for (int i = m_head_idx, sz = m_size; --sz >= 0;)
            {
                ref Node n = ref list_[i];
                i = n.next;

                n = default; // reset
            }

            // reset variables
            m_size = 0;
            m_head_idx = 0;
            m_free_idx = NULL_PTR;
            m_version++;
        }

        public void ClearResize(int new_capacity)
        {
            if (new_capacity > m_list.Length)
            {
                m_list = new Node[Math.Max(new_capacity, 4)];

                m_size = 0;
                m_head_idx = 0;
                m_free_idx = NULL_PTR;
                m_version++;
            }
            else
                Clear();
        }
        #endregion

        #region find & contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NodeIndex<T> FindFirst(T value) => FindFirst(value, EqualityComparer<T>.Default);

        public NodeIndex<T> FindFirst(T value, IEqualityComparer<T> comparer)
        {
            Node[] list = m_list;
            for (int i = m_head_idx, sz = m_size; --sz >= 0; i = list[i].next)
                if (comparer.Equals(list[i].value, value))
                    return new NodeIndex<T>(this, i);

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NodeIndex<T> FindLast(T value) => FindLast(value, EqualityComparer<T>.Default);

        public NodeIndex<T> FindLast(T value, IEqualityComparer<T> comparer)
        {
            Node[] list = m_list;
            for (int i = m_head_idx, sz = m_size; --sz >= 0;)
            {
                i = list[i].prev;
                if (comparer.Equals(list[i].value, value))
                    return new NodeIndex<T>(this, i);
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value) => FindFirst(value).m_owner != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value, EqualityComparer<T> comparer) => FindFirst(value, comparer).m_owner != null;

        public NodeIndex<T> FirstAt(int position)
        {
            if (m_size == 0)
                return default;

            var list = m_list;

            int idx;
            ref Node n = ref list[idx = m_head_idx];

            while (--position >= 0)
                n = ref list[idx = n.next];

            return new NodeIndex<T>(this, idx);
        }

        public NodeIndex<T> LastAt(int position)
        {
            if (m_size == 0)
                return default;

            var list = m_list;

            int idx;
            ref Node n = ref list[idx = list[m_head_idx].prev];

            while (--position >= 0)
                n = ref list[idx = n.prev];

            return new NodeIndex<T>(this, idx);
        }
        #endregion

        #region util methods & interface impl
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public T[] ToArray()
        {
            int sz;
            if ((sz = m_size) == 0)
                return Array.Empty<T>();

            T[] result = new T[sz];

            Node[] list = m_list;
            for (int idx = list[m_head_idx].prev; --sz >= 0;)
            {
                ref Node n = ref list[idx];

                result[sz] = n.value;

                idx = n.prev;
            }

            return result;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if ((uint)arrayIndex >= (uint)array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            int sz;
            if (array.Length - arrayIndex < (sz = m_size))
                throw new ArgumentException("insufficient space");

            Node[] list = m_list;
            for (int idx = m_head_idx; --sz >= 0;)
            {
                ref Node n = ref list[idx];

                array[arrayIndex++] = n.value;

                idx = n.next;
            }
        }

        /// <summary>
        /// 데이터를 대상 배열로 복사합니다
        /// </summary>
        /// <param name="array">데이터를 수신 받을 버퍼입니다</param>
        /// <param name="arrayIndex">버퍼에서 수신 받기 시작할 위치입니다</param>
        /// <param name="startPosition">원본에서 읽기 시작할 위치입니다</param>
        /// <returns>실제로 복사한 데이터의 개수입니다</returns>
        public int TryCopyTo(T[] array, int arrayIndex, int startPosition)
        {
            int size = Math.Min(array.Length - arrayIndex, m_size);

            Node[] list = m_list;

            int idx = m_head_idx;
            ref Node n = ref list[idx];

            // 원형 연결 리스트라서 안전함
            while (--startPosition >= 0)
                n = ref list[n.next];

            for (int i = size; --i >= 0; n = ref list[n.next])
                array[arrayIndex++] = n.value;

            return size;
        }
        #endregion

        #region collection impl
        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item) => AddLast(item);
        #endregion

        #region helper method
        private void IncreaseCapacity()
        {
            Node[] old = m_list;

            int x;
            Node[] arr = new Node[Arrays.GetNextSize(x = m_size)];

            while (--x >= 0)
                arr[x] = old[x];

            m_list = arr;
        }

        private void ValidateNodeIndex(ref NodeIndex<T> node)
        {
            if (node.m_owner != this)
                throw new ArgumentException($"This '{nameof(IndexLinkedList<T>)}' is not the owner of '{nameof(NodeIndex<T>)}'");

            if ((uint)node.m_index >= (uint)m_list.Length)
                throw new InvalidOperationException("Attempting to access a deleted slot");

            ref Node n = ref m_list[node.m_index];
            if (n.next < 0 ||
                (n.next | n.prev) == 0 && node.m_index != 0)
                throw new InvalidOperationException($"Dereference to {nameof(NULL_PTR)}");
        }
        #endregion

        #region struct & nested class
        private struct Node
        {
            public int next;
            public int prev;
            public T value;
        }

        private struct Enumerator : IEnumerator<T>
        {
            private readonly IndexLinkedList<T> _list;
            private readonly uint _version;
            private int _curnode;
            private T _current;

            public Enumerator(IndexLinkedList<T> inst)
            {
                _list = inst;
                _version = inst.m_version;
                _curnode = inst.m_head_idx;
                _current = default;
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            private void ValidateVersion()
            {
                if (_list.m_version != _version)
                    throw new InvalidOperationException("modified collection");
            }

            public bool MoveNext()
            {
                ValidateVersion();

                if (_list.m_size == 0 || _curnode < 0) // empty list OR indexOutOfRng(end enumerator)
                    return false;

                ref Node node = ref _list.m_list[_curnode];

                _current = node.value;

                if ((_curnode = node.next) == _list.m_head_idx)
                    _curnode = NULL_PTR;

                return true;
            }

            public void Reset()
            {
                ValidateVersion();

                _current = default;
                _curnode = _list.m_head_idx;
            }

            public void Dispose() { }
        }
        #endregion
    }
}
