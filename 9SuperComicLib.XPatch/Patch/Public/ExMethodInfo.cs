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

using SuperComicLib.Core;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SuperComicLib.Runtime;

namespace SuperComicLib.XPatch
{
    public class ExMethodInfo : IDisposable
    {
        public MethodInfo patching;
        public string[] toNames;
        public Type[] toTypes;

        public ExMethodInfo(MethodInfo info)
        {
            patching = info;

            ParameterInfo[] temp = info.GetParameters();
            int x = temp.Length;

            toNames = new string[x];
            toTypes = new Type[x];

            for (x--; x >= 0; x--)
            {
                ref ParameterInfo i = ref temp[x];
                toNames[x] = i.Name;
                toTypes[x] = i.ParameterType;
                i = null;
            }
        }

        public virtual bool GenerateCode(
            ILGenerator il,
            MethodBase original,
            ParameterInfo[] parameters,
            int offset,
            bool hasReturn,
            bool hasArgBuffer,
            bool isValuetype)
        {
            int idx = 0;
            int len = toNames.Length;
            if (idx < len)
            {
                if (toNames[0] == "out" && hasReturn)
                {
                    il.Emit(OpCodes.Ldloca_S, (byte)0);
                    idx++;
                }

                if (idx < len && toNames[idx] == "this")
                {
                    if (original.IsStatic)
                        // static 메소드의 첫번째 인수는 @this 일 수 없습니다
                        throw new InvalidOperationException("The first parameter of the static method cannot be @this");

                    // bool refinst = original.DeclaringType.IsValueType;
                    if (isValuetype == false && toTypes[idx].IsByRef)
                        il.Emit(OpCodes.Ldarga_S, (byte)0);
                    else
                        il.Emit(OpCodes.Ldarg_0);
                    idx++;
                }

                for (; idx < len; idx++)
                {
                    ParameterInfo param = parameters.FirstOrDefault(t =>
                    {
                        string now = toNames[idx];
                        return 
                            now.StartsWith("param_") 
                            ? now.Remove(0, 5) == t.Name 
                            : now == t.Name;
                    });

                    if (param == null)
                        continue;

                    if (toTypes[idx].IsByRef)
                        il.Emit_Ldarga(param.Position + offset);
                    else
                        il.Emit_Ldarg(param.Position + offset);
                }
            }
            il.Emit(OpCodes.Call, patching);

            if (patching.ReturnType != CTypes.VOID_T && patching.ReturnType != CTypes.BOOL_T)
                // 패치 메소드의 반환은 void 또는 bool이어야 합니다
                throw new InvalidOperationException("Return type of the patch method must be void or bool");

            return patching.ReturnType == CTypes.BOOL_T;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (patching != null)
            {
                ClsArray.DeleteAll(ref toNames);
                ClsArray.DeleteAll(ref toTypes);

                patching = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
