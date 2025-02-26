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
using SuperComicLib.Core;

namespace SuperComicLib.Arithmetic
{
    public readonly struct UInt256 : IEquatable<UInt256>, IComparable<UInt256>, ICustomInteger
    {
        #region constant | static field
        public const int Size32 = 8;
        public const int Size64 = Size32 >> 1;
        public const int Bits = Size32 << 5;
        public const bool Signed = true;

        public static readonly UInt256 MinValue = new UInt256(long.MinValue, 0, 0, 0);
        public static readonly UInt256 MaxValue = new UInt256(long.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);
        #endregion

        public readonly ulong low;
        public readonly ulong mid;
        public readonly ulong high;
        public readonly long flag;

        #region constructor
        public UInt256(long head, long high, long mid, long low) :
            this(head, (ulong)high, (ulong)mid, (ulong)low)
        {
        }

        public UInt256(long head, ulong high, ulong mid, ulong low)
        {
            this.low = low;
            this.mid = mid;
            this.high = high;
            this.flag = head;
        }
        #endregion

        #region method
        int ICustomInteger.Length32 => Size32;

        int ICustomInteger.Length64 => Size64;

        bool ICustomInteger.IsUnsigned => !Signed;

        public bool Equals(UInt256 other) => this == other;

        public unsafe int CompareTo(UInt256 other)
        {
            fixed (ulong* ptr = &low)
                return BigIntArithmetic.CompareTo(ptr, &other.low, Size64);
        }
        #endregion

        #region override
        public override bool Equals(object obj) =>
            obj is UInt256 v && Equals(v);

        public unsafe override int GetHashCode()
        {
            int result = low.GetHashCode();

            fixed (ulong* ptr = &mid)
                for (int x = Size64 - 2; --x >= 0;)
                    result = IntHash.Combine(result, ptr[x].GetHashCode());

            return IntHash.Combine(result, flag.GetHashCode());
        }

        public unsafe override string ToString() => ToString(null);

        public unsafe string ToString(string format)
        {
            fixed (ulong* ptr = &low)
                return BigIntArithmetic.FormatString((uint*)ptr, Size32, Signed, format);
        }
        #endregion

        #region + - * / %
        public unsafe static UInt256 operator +(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.Add((uint*)&left.low, (uint*)&right.low, Size32);
            return left;
        }
        public unsafe static UInt256 operator -(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.Sub((uint*)&left.low, (uint*)&right.low, Size32);
            return left;
        }
        public unsafe static UInt256 operator *(UInt256 left, UInt256 right)
        {
            UInt256 result = default;
            BigIntArithmetic.Mul((uint*)&left.low, (uint*)&right.low, (uint*)&result.low, Size32);
            return result;
        }
        public unsafe static UInt256 operator /(UInt256 left, UInt256 right)
        {
            UInt256 result = default;
            BigIntArithmetic.Div(&left.low, &right.low, &result.low, Size64);
            return result;
        }
        public unsafe static UInt256 operator %(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.Mod(&left.low, &right.low, Size64);
            return left;
        }
        #endregion

        #region neg or pos
        public unsafe static UInt256 operator -(UInt256 value)
        {
            BigIntArithmetic.NEG((uint*)&value.low, (uint*)&value.low, Size32);
            return value;
        }
        public unsafe static UInt256 operator +(UInt256 value) => value;
        #endregion

        #region bit
        public unsafe static UInt256 operator |(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.OR(&left.low, &right.low, Size64);
            return left;
        }
        public unsafe static UInt256 operator ^(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.XOR(&left.low, &right.low, Size64);
            return left;
        }
        public unsafe static UInt256 operator &(UInt256 left, UInt256 right)
        {
            BigIntArithmetic.AND(&left.low, &right.low, Size64);
            return left;
        }
        public unsafe static UInt256 operator <<(UInt256 left, int shift)
        {
            if (shift <= 0)
                return left;
            else if (shift >= Bits)
                return default;

            BigIntArithmetic.LSHIFT(&left.low, shift, Size64);
            return left;
        }
        public unsafe static UInt256 operator >>(UInt256 left, int shift)
        {
            if (shift <= 0)
                return left;
            else if (shift >= Bits)
                return default;

            BigIntArithmetic.RSHIFT(&left.low, shift, Size64);
            return left;
        }
        public unsafe static UInt256 operator ~(UInt256 value)
        {
            BigIntArithmetic.NOT(&value.low, Size64);
            return value;
        }
        #endregion

        #region x -> current
        public static unsafe implicit operator UInt256(float v)
        {
            UInt256 result = default;
            if (BigIntArithmetic.FormatIEEE754(v, &result.low, Size64))
                BigIntArithmetic.NEG((uint*)&result.low, (uint*)&result.low, Size32);

            return result;
        }

        public static implicit operator UInt256(decimal v)
        {
            int[] vs = decimal.GetBits(v);
            return new UInt256(
                (long)(vs[3] & int.MinValue) << 32,
                0,
                vs[2],
                ((long)vs[1] << 32) | (uint)vs[0]);
        }

        public static implicit operator UInt256(uint v) => new UInt256(0, 0, 0, v);
        public static implicit operator UInt256(ulong v) => new UInt256(0, 0, 0, v);
        public static implicit operator UInt256(int v) =>
            v < 0
            ? new UInt256(-1, -1, -1, v)
            : new UInt256(0, 0, 0, v);
        public static implicit operator UInt256(long v) =>
            v < 0
            ? new UInt256(-1, -1, -1, v)
            : new UInt256(0, 0, 0, (ulong)v);

        // from low bits
        public static implicit operator UInt256(in Int128 value) =>
            value.high < 0
            ? new UInt256(-1, -1, value.high, (long)value.low)
            : new UInt256(0, 0, (ulong)value.high, value.low);

        public static implicit operator UInt256(in UInt128 value) =>
            new UInt256(0, 0, value.high, value.low);
        #endregion

        #region current -> x
        public static unsafe explicit operator int(UInt256 v) => (int)v.low;
        public static unsafe explicit operator uint(UInt256 v) => (uint)v.low;
        public static unsafe explicit operator long(UInt256 v) => (long)v.low;
        public static unsafe explicit operator ulong(UInt256 v) => v.low;
        public static unsafe explicit operator decimal(UInt256 v) => new decimal((int)v.low, (int)(v.low >> 32), (int)v.flag, v.flag < 0, 0);

        public static unsafe explicit operator float(UInt256 v)
        {
            bool neg;
            if (v.flag < 0) // neg
            {
                neg = true;
                BigIntArithmetic.NEG((uint*)&v.low, (uint*)&v.low, Size32);
            }
            else
                neg = false;

            float result = BigIntArithmetic.ToIEEE754(&v.low);
            return
                neg
                ? -result
                : result;
        }
        #endregion

        #region compare
        public static unsafe bool operator ==(UInt256 left, UInt256 right) =>
            BigIntArithmetic.Equals(&left.low, &right.low, Size64);
        public static unsafe bool operator !=(UInt256 left, UInt256 right) =>
            BigIntArithmetic.Inequals(&left.low, &right.low, Size64);
        public static bool operator <(UInt256 left, UInt256 right) => left.CompareTo(right) < 0;
        public static bool operator >(UInt256 left, UInt256 right) => left.CompareTo(right) > 0;
        public static bool operator <=(UInt256 left, UInt256 right) => left.CompareTo(right) <= 0;
        public static bool operator >=(UInt256 left, UInt256 right) => left.CompareTo(right) >= 0;
        #endregion
    }
}