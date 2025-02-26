﻿// MIT License
//
// Copyright (c) 2019-2022 SuperComic (ekfvoddl3535@naver.com)
// Copyright (c) 2017 Andreas Pardeike
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SuperComicLib.Runtime;

namespace SuperComicLib.XPatch
{
    internal sealed class NativeThisPointer
    {
        // 의미없는 값
        private static readonly IntPtr magic = new IntPtr(0x1345789A);
        private static bool hasThisPtr;

        private NativeThisPointer() { }

        #region 준비용
#pragma warning disable
        /// <summary>
        /// 이 메서드는 replacemethod가 잘 작동하는지 확인하기 위한 수단으로 사용됩니다
        /// 다른 메서드를 침범하지 않도록 하기 위해 최소 (32비트: 6바이트, 64비트: 12바이트) 이상의 코드 크기를 유지해야합니다
        /// (어셈블리 기준)
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Test GetTest(IntPtr t1, IntPtr t2) => default(Test);

        private static void ReplacedTest(NativeThisPointer instance, IntPtr ptr, IntPtr t1, IntPtr t2) =>
            hasThisPtr = t1 == t2 && t1 == magic;
#pragma warning restore
        #endregion

        #region 정적 기본
        internal static bool NeedsPointerFix(Type retType)
        {
            if (retType.IsStruct() == false)
                return false;

            int size = Marshal.SizeOf(retType);
            return
                size >= 3 && 
                size != 4 && 
                size != 8 && 
                hasThisPtr;
        }

        // 한 번만 한다
        static NativeThisPointer()
        {
            NativeThisPointer instance = new NativeThisPointer();
            Type me = instance.GetType();

            RuntimeMethodHandle handle = me.GetMethod(nameof(ReplacedTest), Helper.mflag1).MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            Helper.ReplaceMethod(me.GetMethod(nameof(GetTest), Helper.mflag0), handle.GetFunctionPointer());

            instance.GetTest(magic, magic);
        }
        #endregion

#pragma warning disable
        private struct Test
        {
            readonly byte b1;
            readonly byte b2;
            readonly byte b3;
        }
#pragma warning restore
    }
}
