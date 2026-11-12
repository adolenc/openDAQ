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

    typedef daqErrCode (*daqFuncCall)(daqBaseObject*, daqBaseObject**);
    typedef daqErrCode (*daqProcCall)(daqBaseObject*);
    typedef void (*daqEventCall)(daqBaseObject*, daqBaseObject*);

    /* The same callbacks with a pointer to the caller's own data as their last argument, and the
       function that releases that data (see the ...WithData factories in ccoretypes/factories.h). */
    typedef daqErrCode (*daqFuncCallWithData)(daqBaseObject*, daqBaseObject**, void*);
    typedef daqErrCode (*daqProcCallWithData)(daqBaseObject*, void*);
    typedef void (*daqEventCallWithData)(daqBaseObject*, daqBaseObject*, void*);
    typedef void (*daqFreeUserData)(void*);

    typedef struct daqIntfID
    {
        uint32_t Data1;
        uint16_t Data2;
        uint16_t Data3;
        uint64_t Data4;
    } daqIntfID;

    /* Layout-compatible with daq::SourceLocation, which is packed. */
#pragma pack(push, 1)
    typedef struct daqSourceLocation
    {
        daqCConstCharPtr fileName;
        daqCInt line;
        daqCConstCharPtr funcName;
    } daqSourceLocation;
#pragma pack(pop)

#ifdef __cplusplus
}
#endif

/* The enums and the status codes of the API, both generated from the core headers. */
#include <cenums.h>
#include <cerrors.h>
