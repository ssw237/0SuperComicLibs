﻿using SuperComicLib.Reflection.Patch;
using System.Reflection;
using System.Reflection.Emit;

namespace SuperComicLib.Reflection
{
    public delegate void PrepareMethodExample();
    public delegate TargetMethodInfo RegisterPatchMethodExample();
    public delegate void ILOnlyPatcherExample(ILGenerator il, MethodBase original, int argumentFixupOffset, bool hasReturnValue);

    // ILMethodAttribute 전용
    // public delegate IEnumerable<ILBuffer> TargetMethodBodyEditorExample_1(ILBuffer[] original_ILStream);
}
