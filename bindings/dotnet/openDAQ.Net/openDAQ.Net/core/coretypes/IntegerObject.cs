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
//     RTGen (CSharpGenerator v1.0.0) on 27.05.2024 12:23:59.
// </auto-generated>
//------------------------------------------------------------------------------


namespace Daq.Core.Types;


[StructLayout(LayoutKind.Sequential)]
internal unsafe class RawIntegerObject : RawBaseObject
{
    //ErrorCode getValue(daq.Int* value); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, out long, ErrorCode> GetValue;
    //ErrorCode equalsValue(daq.Int value, daq.Bool* equals); stdcall;
    public delegate* unmanaged[Stdcall]<IntPtr, long, out bool, ErrorCode> EqualsValue;
}

[Guid("b5c52f78-45f9-5c54-9bc1-ca65a46472cb")]
public class IntegerObject : BaseObject
{
    //type-casted base._virtualTable
    private readonly RawIntegerObject _rawIntegerObject;

    internal IntegerObject(IntPtr nativePointer, bool incrementReference)
        : base(nativePointer, incrementReference)
    {
        IntPtr objVirtualTable = Marshal.ReadIntPtr(nativePointer, 0); //read the pointer from the given address
        base._virtualTable =
            _rawIntegerObject = Marshal.PtrToStructure<RawIntegerObject>(objVirtualTable);
    }

    #region properties

    /// <summary>Gets an int value stored in the object.</summary>
    /// <remarks>Call this method to extract the int value that is stored in the object.</remarks>
    public long Value
    {
        get
        {
            //native output argument
            long value;

            unsafe //use native function pointer
            {
                //call native function
                ErrorCode errorCode = (ErrorCode)_rawIntegerObject.GetValue(base.NativePointer, out value);

                if (Result.Failed(errorCode))
                {
                    throw new OpenDaqException(errorCode);
                }
            }

            return value;
        }
    }

    #endregion properties

    /// <summary>Compares stored int value to the int parameter.</summary>
    /// <remarks>Call this method to directly compare the object to the value parameter.</remarks>
    /// <param name="value">Value for comparison.</param>
    /// <returns>The result of the comparison.</returns>
    public bool EqualsValue(long value)
    {
        //native output argument
        bool equals;

        unsafe //use native function pointer
        {
            //call native function
            ErrorCode errorCode = (ErrorCode)_rawIntegerObject.EqualsValue(base.NativePointer, value, out equals);

            if (Result.Failed(errorCode))
            {
                throw new OpenDaqException(errorCode);
            }
        }

        return equals;
    }

    #region operators

    //implicit cast operators 'Daq.Core.Types.IntegerObject' to/from 'long'

    /// <summary>Performs an implicit conversion from <see cref="long"/> to <see cref="Daq.Core.Types.IntegerObject"/>.</summary>
    /// <param name="value">The managed <c>long</c> value.</param>
    /// <returns>The SDK <c>IntegerObject</c>.</returns>
    public static implicit operator IntegerObject(long value) => CoreTypesFactory.CreateInteger(value);

    /// <summary>Performs an implicit conversion from <see cref="Daq.Core.Types.IntegerObject"/> to <see cref="long"/>.</summary>
    /// <param name="value">The SDK <c>IntegerObject</c>.</param>
    /// <returns>The managed <c>long</c> value.</returns>
    public static implicit operator long(IntegerObject value) => value.Value;

    /// <summary>Determines whether this instance and a specified <c>long</c>, have the same value.</summary>
    /// <param name="other">The other <c>long</c> to compare to this instance.</param>
    /// <returns><c>true</c> if the other <c>long</c> value is the same as this instance; otherwise, <c>false</c>.</returns>
    public bool Equals(long other) => ((long)this).Equals(other);

    #endregion operators
}


#region Class Factory

// Factory functions of the &apos;CoreTypes&apos; library.
public static partial class CoreTypesFactory
{
    //ErrorCode createInteger(daq.IInteger** obj, daq.Int value); cdecl;
    [DllImport(CoreTypesDllInfo.FileName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ErrorCode createInteger(out IntPtr obj, long value);

    public static ErrorCode CreateInteger(out IntegerObject obj, long value)
    {
        //initialize output argument
        obj = default;

        //native output argument
        IntPtr objPtr;

        //call native function
        ErrorCode errorCode = createInteger(out objPtr, value);

        if (Result.Succeeded(errorCode))
        {
            //create object
            obj = new IntegerObject(objPtr, incrementReference: false);
        }

        return errorCode;
    }

    public static IntegerObject CreateInteger(long value)
    {
        //native output argument
        IntPtr objPtr;

        //call native function
        ErrorCode errorCode = createInteger(out objPtr, value);

        if (Result.Failed(errorCode))
        {
            throw new OpenDaqException(errorCode);
        }

        //create and return object
        return new IntegerObject(objPtr, incrementReference: false);
    }
}

#endregion Class Factory
