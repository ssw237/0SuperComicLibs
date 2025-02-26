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

using System.Runtime.CompilerServices;
using System.Security;

namespace SuperComicLib.IO
{
    [SuppressUnmanagedCodeSecurity]
    public static unsafe class GLOBAL_SuperComicLibCore_IO_UnmanagedStreamExtension
    {
        #region read + write
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Read<T>(this UnmanagedStream stream, in NativeSpan<T> buffer) where T : unmanaged =>
#if AnyCPU
            stream.Read((byte*)buffer.Source, (byte*)buffer.end()._ptr);
#else
            stream.Read((byte*)buffer.Source, (byte*)(buffer.Source + buffer.Length));
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this UnmanagedStream stream, in NativeSpan<T> buffer) where T : unmanaged =>
#if AnyCPU
            stream.Write((byte*)buffer.Source, (byte*)buffer.end()._ptr);
#else
            stream.Write((byte*)buffer.Source, (byte*)(buffer.Source + buffer.Length));
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this UnmanagedStream stream, in NativeConstSpan<T> buffer) where T : unmanaged =>
#if AnyCPU
            stream.Write((byte*)buffer._source, (byte*)buffer.end()._ptr);
#else
            stream.Write((byte*)buffer._source, (byte*)(buffer._source + buffer.Length));
#endif
        #endregion

        #region copy-to
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CopyTo(this UnmanagedStream source, UnmanagedStream dest) => CopyTo(source, dest, 16384);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CopyTo(this UnmanagedStream source, UnmanagedStream dest, int tempBuffer_size)
        {
            byte[] temp_buffer = new byte[tempBuffer_size];

            fixed (byte* buffer = &temp_buffer[0])
                return CopyTo(source, dest, new NativeSpan<byte>(buffer, buffer + tempBuffer_size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CopyTo(this UnmanagedStream source, UnmanagedStream dest, in NativeSpan<byte> temp_buffer)
        {
            if (dest.CanWrite == false)
                return false;

            byte* buffer_end = temp_buffer.Source + temp_buffer.Length;

            while (!source.EndOfStream)
            {
                source.Read(temp_buffer.Source, buffer_end);
                dest.Write(temp_buffer.Source, buffer_end);
            }

            return true;
        }
        #endregion

        #region convert
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedStream AsUnmanagedStream<T>(this in NativeSpan<T> span) where T : unmanaged =>
#if AnyCPU
            new UnmanagedStream(span.Source, (byte*)span.end()._ptr - (byte*)span.Source);
#else
            new UnmanagedStream(span.Source, span.Length);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedReadOnlyStream AsUnmanagedStream<T>(this in NativeConstSpan<T> span) where T : unmanaged =>
#if AnyCPU
            new UnmanagedReadOnlyStream(span._source, (byte*)span.end()._ptr - (byte*)span._source);
#else
            new UnmanagedReadOnlyStream(span._source, span.Length);
#endif
        #endregion
    }
}
