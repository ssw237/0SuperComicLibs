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

using SuperComicLib.Collections;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperComicLib.IO
{
    public sealed unsafe class FormatStreamReader<TResult, TLocalBuf> : IDisposable
        where TResult : unmanaged
        where TLocalBuf : unmanaged
    {
        public readonly Encoding baseEncoding;
        public readonly Stream baseStream;
        private IFormatParseResolver<TResult, TLocalBuf> _resolver;
        private readonly byte[] _ub_arr;
        private readonly NativeArray<char> _char_arr;

        #region constructors
        public FormatStreamReader(Stream stream, Encoding encoding, IFormatParseResolver<TResult, TLocalBuf> resolver)
        {
            const int TSIZE_LIMIT = 0xFEFF;
            if (sizeof(TResult) + sizeof(TLocalBuf) >= TSIZE_LIMIT)
                throw new StackOverflowException($"{nameof(TResult)} + {nameof(TLocalBuf)} size overflow. size limit: {TSIZE_LIMIT} bytes");

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            int bufsize = (int)CMath.Max((uint)resolver.EncodingBufferSize, 128u);

            int size = encoding.GetMaxCharCount(bufsize);
            _char_arr = new NativeArray<char>(size);

            resolver.SetNativeBuffer(_char_arr.Ptr);

            baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            baseEncoding = encoding;

            _resolver = resolver;

            _ub_arr = new byte[bufsize];
        }

        internal FormatStreamReader(Stream stream, Encoding encoding, IFormatParseResolver<TResult, TLocalBuf> resolver, int buf_size)
        {
            baseStream = stream;
            baseEncoding = encoding;

            _resolver = resolver;

            _char_arr = new NativeArray<char>(encoding.GetMaxByteCount(buf_size));

            resolver.SetNativeBuffer(_char_arr.Ptr);

            _ub_arr = new byte[buf_size];
        }
        #endregion

        #region method
        public TResult? Read()
        {
            // max 65279 bytes
            TLocalBuf local_buf;

            TResult local_res;

            var inst = _resolver;

            var buf = _char_arr;

            var encarr = _ub_arr;

            var stream_ = baseStream;
            var encoding_ = baseEncoding;

            // x64-> max 65279 + 8 + 24 + 32(home space) = 65343
            for (; ; )
            {
                int readcnt = stream_.Read(encarr, 0, encarr.Length);

                // end of stream
                if (readcnt <= 0)
                {
                    readcnt = (int)inst.OnEndOfStream(&local_buf);
                    if (readcnt == (int)StreamStateControl.Restart)
                    {
                        stream_.Seek(0, SeekOrigin.Begin);
                        continue;
                    }
                    else if (readcnt == (int)StreamStateControl.PushAndReturn)
                    {
                        return
                            Calli(inst, &local_buf, &local_res, 0)
                            ? local_res
                            : throw new InvalidOperationException($"Parse fail (last push)");
                    }
                    else
                        return null;
                }

                int pos;
                fixed (byte* ptr = &encarr[0])
#if AnyCPU || X86
                    pos = encoding_.GetChars(ptr, readcnt, buf.Ptr, buf.Length);
#else
                    pos = encoding_.GetChars(ptr, readcnt, buf.Ptr, (int)buf.Length);
#endif

                if (Calli(inst, &local_buf, &local_res, pos))
                    return local_res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Calli(IFormatParseResolver<TResult, TLocalBuf> inst, TLocalBuf* local_buf, TResult* local_res, int pos)
        {
            var state = inst.Push(pos, local_buf, local_res);

            if (state == ResolverState.Fail)
                throw new InvalidOperationException($"Parse fail");

            return state == ResolverState.Success;
        }
#endregion

#region dispose
        ~FormatStreamReader()
        {
            if (_resolver != null)
            {
                baseStream.Dispose();
                _resolver.Dispose();
                _char_arr.Dispose();
            }
        }

        public void Dispose()
        {
            if (_resolver != null)
            {
                baseStream.Dispose();
                _resolver.Dispose();
                _char_arr.Dispose();

                _resolver = null;
            }
            GC.SuppressFinalize(this);
        }
#endregion
    }
}
