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
//     RTGen (CSharpGenerator v1.0.0) on 27.05.2024 12:23:55.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Daq.Core.Types;


/// <summary>Core type enumeration</summary>
public enum CoreType
{
    /// <summary>Boolean, True or False</summary>
    ctBool = 0,
    /// <summary>64 bit signed integer</summary>
    ctInt,
    /// <summary>IEEE 754 64 bit floating point</summary>
    ctFloat,
    /// <summary>UTF8 zero terminated string</summary>
    ctString,
    /// <summary>List of IBaseObject</summary>
    ctList,
    /// <summary>Dictionary of (key: IBaseObject, value: IBaseObject)</summary>
    ctDict,
    /// <summary>Rational number (numerator / denominator)</summary>
    ctRatio,
    /// <summary>Callback without return value</summary>
    ctProc,
    /// <summary>Generic object</summary>
    ctObject,
    /// <summary>Binary buffer with predefined size</summary>
    ctBinaryData,
    /// <summary>Callback with return value</summary>
    ctFunc,
    /// <summary>Complex number (real, imaginary)</summary>
    ctComplexNumber,
    /// <summary>Constant structure with dictionary of fields and types</summary>
    ctStruct,
    /// <summary>Enumeration representing a predefined set of named integral constants</summary>
    ctEnumeration,
    /// <summary>Undefined</summary>
    ctUndefined = 65535,
};


[StructLayout(LayoutKind.Sequential)]
internal unsafe class RawCoreTypeObject : RawBaseObject
{
    //ErrorCode getCoreType(daq.CoreType* coreType); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out CoreType, ErrorCode> GetCoreType;
}

/// <summary>Adds the ability for openDAQ objects to report what type of object they represent.</summary>
/// <remarks>
/// An object can implement this interface to give clients introspection ability on the type
/// of the object. The client can use this information to further query for the interface of the
/// specific type. For example, if the <c>CoreType</c> reported is ctInt, the object is expected to
/// implement the IIntObject interface.
/// </remarks>
[Guid("0562d045-c94e-5e6d-8360-2cfc9db76a04")]
public class CoreTypeObject : BaseObject
{
    //type-casted base._virtualTable
    private readonly RawCoreTypeObject _rawCoreTypeObject;

    internal CoreTypeObject(IntPtr nativePointer, bool incrementReference)
        : base(nativePointer, incrementReference)
    {
        IntPtr objVirtualTable = Marshal.ReadIntPtr(nativePointer, 0); //read the pointer from the given address
        base._virtualTable =
            _rawCoreTypeObject = Marshal.PtrToStructure<RawCoreTypeObject>(objVirtualTable);
    }

    #region properties

    /// <summary>Gets the object&apos;s CoreType.</summary>
    public CoreType CoreType
    {
        get
        {
            //native output argument
            CoreType coreType;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawCoreTypeObject.GetCoreType(base.NativePointer, out coreType);

                if (Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            return coreType;
        }
    }

    #endregion properties
}


#region Class Factory

// Factory functions of the &apos;CoreTypes&apos; library.
public static partial class CoreTypesFactory
{
}

#endregion Class Factory
