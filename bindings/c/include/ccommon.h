/*
 * Copyright 2022-2026 openDAQ d.o.o.
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

#pragma once

#ifdef __cplusplus
extern "C"
{
#endif

#if !defined(_WIN32)
#define EXPORTED __attribute__((visibility("default")))
#elif defined(BUILDING_SHARED_LIBRARY)
#define EXPORTED __declspec(dllexport)
#else
#define EXPORTED __declspec(dllimport)
#endif

#include <stddef.h>
#include <stdint.h>

    /* Plain C values are prefixed with "daqC", openDAQ objects with "daq". */
    typedef uint32_t daqErrCode;
    typedef uint8_t daqCBool;
    typedef int64_t daqCInt;
    typedef uint64_t daqCUInt;
    typedef double daqCFloat;
    typedef char* daqCCharPtr;
    typedef const char* daqCConstCharPtr;
    typedef void* daqCVoidPtr;
    typedef size_t daqCSizeT;
    typedef uint32_t daqCEnumType;

    typedef void daqUnknown;
    typedef void daqBaseObject;

#pragma pack(push, 1)
    typedef struct daqComplexFloat64
    {
        double real;
        double imaginary;
    } daqComplexFloat64;
#pragma pack(pop)

    enum
    {
        daqFalse = 0,
        daqTrue = 1
    };

    typedef enum daqCCoreType
    {
        daqCtBool = 0,             ///< Boolean, True or False
        daqCtInt,                  ///< 64 bit signed integer
        daqCtFloat,                ///< IEEE 754 64 bit floating point
        daqCtString,               ///< UTF8 zero terminated string
        daqCtList,                 ///< List of IBaseObject
        daqCtDict,                 ///< Dictionary of (key: IBaseObject, value: IBaseObject)
        daqCtRatio,                ///< Rational number (numerator / denominator)
        daqCtProc,                 ///< Callback without return value
        daqCtObject,               ///< Generic object
        daqCtBinaryData,           ///< Binary buffer with predefined size
        daqCtFunc,                 ///< Callback with return value
        daqCtComplexNumber,        ///< Complex number (real, imaginary)
        daqCtStruct,               ///< Constant structure with dictionary of fields and types
        daqCtEnumeration,          ///< Enumeration representing a predefined set of named integral constants
        daqCtUndefined = 0xFFFF,   ///< Undefined
    } daqCCoreType;

    typedef daqErrCode (*daqFuncCall)(daqBaseObject*, daqBaseObject**);
    typedef daqErrCode (*daqProcCall)(daqBaseObject*);
    typedef void (*daqEventCall)(daqBaseObject*, daqBaseObject*);

    typedef struct daqIntfID
    {
        uint32_t Data1;
        uint16_t Data2;
        uint16_t Data3;
        uint64_t Data4;
    } daqIntfID;

    typedef enum daqLockingStrategy
    {
        daqOwnLock = 0,        // Object locks its own mutex.
        daqInheritLock,        // Object locks the mutex of the nearest ancestor with the OwnLock strategy.
        daqForwardOwnerLockOwn // Object locks its own mutex, but forwards the `getMutexOwner` request to its owner/parent.
    } daqLockingStrategy;

#include <ccoreobjects/common.h>

#include <copendaq/component/common.h>
#include <copendaq/context/common.h>
#include <copendaq/device/common.h>
#include <copendaq/functionblock/common.h>
#include <copendaq/logger/common.h>
#include <copendaq/modulemanager/common.h>
#include <copendaq/opendaq/common.h>
#include <copendaq/reader/common.h>
#include <copendaq/scheduler/common.h>
#include <copendaq/server/common.h>
#include <copendaq/signal/common.h>
#include <copendaq/streaming/common.h>
#include <copendaq/synchronization/common.h>
#include <copendaq/utility/common.h>

#ifdef __cplusplus
}
#endif
