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

using System.Collections.Generic;

namespace SuperComicLib.Collections
{
    public sealed class TreeNode<T>
    {
        public T Value;
        internal TreeNode<T> root;
        internal TreeNode<T> prev;
        internal TreeNode<T> child_tail; // same root

        public TreeNode(T value) : this(null, value)
        {
        }

        internal TreeNode(TreeNode<T> root, T value)
        {
            this.root = root;
            Value = value;
        }

        public TreeNode<T> RootNode => root;

        public bool IsRootNode => root == null;

        public bool IsLeafNode => child_tail == null;

        public void Expand(T value)
        {
            TreeNode<T> child = new TreeNode<T>(this, value)
            {
                prev = child_tail
            };

            child_tail = child;
        }

        public bool Shrink(T value) =>
            Shrink(value, EqualityComparer<T>.Default);

        public bool Shrink(T value, IEqualityComparer<T> comparer)
        {
            TreeNode<T> _next = null;
            TreeNode<T> _curr = child_tail;
            
            while (_curr != null)
            {
                if (comparer.Equals(value, _curr.Value))
                {
                    _curr.root = null;
                    if (_next != null)
                        _next.prev = _curr.prev;
                    else
                        child_tail = _curr.prev;

                    return true;
                }

                _next = _curr; 
                _curr = _curr.prev;
            }

            return false;
        }

        public TreeNode<T> FindChild(T value) =>
            FindChild(value, EqualityComparer<T>.Default);

        public TreeNode<T> FindChild(T value, IEqualityComparer<T> comparer)
        {
            TreeNode<T> curr = child_tail;
            for (; curr != null; curr = curr.prev)
                if (comparer.Equals(curr.Value, value))
                    return curr;

            return null;
        }
    }
}
