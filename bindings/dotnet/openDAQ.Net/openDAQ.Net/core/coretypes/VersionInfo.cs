/*
 * Copyright 2022-2024 Blueberry d.o.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CSharpGenerator v1.0.0) on 29.04.2024 15:46:05.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Daq.Core.Types;


[StructLayout(LayoutKind.Sequential)]
internal unsafe class RawVersionInfo : RawBaseObject
{
    //ErrorCode getMajor(daq.SizeT* major); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out nuint, ErrorCode> GetMajor;
    //ErrorCode getMinor(daq.SizeT* minor); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out nuint, ErrorCode> GetMinor;
    //ErrorCode getPatch(daq.SizeT* patch); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out nuint, ErrorCode> GetPatch;
}

/// <summary>
/// Represents a semantic version
/// composing of:
/// - major version representing breaking changes
/// - minor version representing new features
/// - patch version representing only bug fixes.
/// </summary>
[Guid("5951d4d2-35eb-513c-b67d-89dabc6be3bf")]
public class VersionInfo : BaseObject
{
    //type-casted base._virtualTable
    private readonly RawVersionInfo _rawVersionInfo;

    internal VersionInfo(IntPtr nativePointer, bool incrementReference)
        : base(nativePointer, incrementReference)
    {
        IntPtr objVirtualTable = Marshal.ReadIntPtr(nativePointer, 0); //read the pointer from the given address
        base._virtualTable =
            _rawVersionInfo = Marshal.PtrToStructure<RawVersionInfo>(objVirtualTable);
    }

    /// <summary>The major version incremented at breaking changes.</summary>
    public nuint Major
    {
        get
        {
            //native output argument
            nuint major;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawVersionInfo.GetMajor(base.NativePointer, out major);

                if (Daq.Core.Types.Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            return major;
        }
    }

    /// <summary>The minor version incremented at new features with full backwards compatibility.</summary>
    public nuint Minor
    {
        get
        {
            //native output argument
            nuint minor;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawVersionInfo.GetMinor(base.NativePointer, out minor);

                if (Daq.Core.Types.Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            return minor;
        }
    }

    /// <summary>The patch version incremented when only bug-fixes are added.</summary>
    public nuint Patch
    {
        get
        {
            //native output argument
            nuint patch;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawVersionInfo.GetPatch(base.NativePointer, out patch);

                if (Daq.Core.Types.Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            return patch;
        }
    }
}


#region Class Factory

// Factory functions of the &apos;CoreTypes&apos; library.
public static partial class CoreTypesFactory
{
    //ErrorCode createVersionInfo(daq.IVersionInfo** obj, daq.SizeT major, daq.SizeT minor, daq.SizeT patch); cdecl;
    [DllImport(CoreTypesDllInfo.FileName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ErrorCode createVersionInfo(out IntPtr obj, nuint major, nuint minor, nuint patch);

    public static ErrorCode CreateVersionInfo(out VersionInfo obj, nuint major, nuint minor, nuint patch)
    {
        //initialize output argument
        obj = default;

        //native output argument
        IntPtr objPtr;

        //call native function
        ErrorCode errorCode = createVersionInfo(out objPtr, major, minor, patch);

        if (Daq.Core.Types.Result.Succeeded(errorCode))
        {
            //create object
            obj = new VersionInfo(objPtr, incrementReference: false);
        }

        return errorCode;
    }

    public static VersionInfo CreateVersionInfo(nuint major, nuint minor, nuint patch)
    {
        //native output argument
        IntPtr objPtr;

        //call native function
        ErrorCode errorCode = createVersionInfo(out objPtr, major, minor, patch);

        if (Daq.Core.Types.Result.Failed(errorCode))
        {
            throw new OpenDaqException(errorCode);
        }

        //create and return object
        return new VersionInfo(objPtr, incrementReference: false);
    }
}

#endregion Class Factory
