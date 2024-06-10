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
//     RTGen (CSharpGenerator v1.0.0) on 27.05.2024 12:24:05.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Daq.Core.Types;


[StructLayout(LayoutKind.Sequential)]
internal unsafe class RawDaqType : RawBaseObject
{
    //ErrorCode getName(daq.IString** typeName); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, ErrorCode> GetName;
}

/// <summary>
/// The base object type that is inherited by all Types (eg. Struct type, Simple type, Property object class)
/// in openDAQ.
/// </summary>
/// <remarks>
/// Types are used for the construction of objects that are require validation/have pre-defined fields such as
/// Structs and Property objects. Types should be inserted into the Type manager to be used by different parts
/// of the SDK.
/// </remarks>
[Guid("c7383abd-1285-5e81-9e8b-059ed35f91c5")]
public class DaqType : BaseObject
{
    //type-casted base._virtualTable
    private readonly RawDaqType _rawDaqType;

    internal DaqType(IntPtr nativePointer, bool incrementReference)
        : base(nativePointer, incrementReference)
    {
        IntPtr objVirtualTable = Marshal.ReadIntPtr(nativePointer, 0); //read the pointer from the given address
        base._virtualTable =
            _rawDaqType = Marshal.PtrToStructure<RawDaqType>(objVirtualTable);
    }

    #region properties

    /// <summary>Gets the name of the Type</summary>
    public string Name
    {
        get
        {
            //native output argument
            IntPtr typeNamePtr;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawDaqType.GetName(base.NativePointer, out typeNamePtr);

                if (Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            // validate pointer
            if (typeNamePtr == IntPtr.Zero)
            {
                return default;
            }

            using var typeName = new StringObject(typeNamePtr, incrementReference: false);
            return typeName;
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
