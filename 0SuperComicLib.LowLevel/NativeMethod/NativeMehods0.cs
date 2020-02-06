﻿using System;
using System.Runtime.InteropServices;

namespace SuperComicLib.LowLevel
{
    internal static class NativeMehods0
    {
        private const string k = "kernel32.dll";

        [DllImport(k, SetLastError = true)]
        internal unsafe static extern void* VirtualAlloc(void* lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);


        [DllImport(k, SetLastError = true)]
        internal unsafe static extern bool VirtualFree(void* lpAddress, IntPtr dwSize, uint dwFreeType);


        [DllImport(k, SetLastError = true)]
        internal unsafe static extern void RtlZeroMemory(void* dest, IntPtr dwSize);


        [DllImport(k, SetLastError = true)]
        internal unsafe static extern bool VirtualLock(void* lpAddress, IntPtr dwSize);


        [DllImport(k, SetLastError = true)]
        internal unsafe static extern bool VirtualUnlock(void* lpAddress, IntPtr dwSize);


        [DllImport(k, SetLastError = true)]
        internal unsafe static extern bool VirtualProtect(void* lpAddress, IntPtr dwSize, uint flAllocationType, out uint lpflOldProtect);
    }
}
