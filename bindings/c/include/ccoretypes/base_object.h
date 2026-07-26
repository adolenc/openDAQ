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

#include <ccommon.h>

    EXPORTED extern const daqIntfID DAQ_UNKNOWN_INTF_ID;
    EXPORTED extern const daqIntfID DAQ_BASE_OBJECT_INTF_ID;

    /* IUnknown. Reference counting and interface discovery, available on every openDAQ object. */
    daqErrCode EXPORTED daqUnknown_queryInterface(daqUnknown* self, daqIntfID intfId, daqBaseObject** interfacePtr);
    int EXPORTED daqUnknown_addRef(daqUnknown* self);
    int EXPORTED daqUnknown_releaseRef(daqUnknown* self);

    /* IBaseObject, which extends IUnknown. */
    daqErrCode EXPORTED daqBaseObject_borrowInterface(daqBaseObject* self, daqIntfID intfId, daqBaseObject** interfacePtr);
    daqErrCode EXPORTED daqBaseObject_dispose(daqBaseObject* self);
    daqErrCode EXPORTED daqBaseObject_getHashCode(daqBaseObject* self, daqCSizeT* hashCode);
    daqErrCode EXPORTED daqBaseObject_equals(daqBaseObject* self, daqBaseObject* other, daqCBool* equal);
    daqErrCode EXPORTED daqBaseObject_toString(daqBaseObject* self, daqCCharPtr* str);
    daqErrCode EXPORTED daqBaseObject_createBaseObject(daqBaseObject** baseObject);

    static inline int daqAddRef(daqUnknown* self)
    {
        return daqUnknown_addRef(self);
    }

    static inline int daqReleaseRef(daqUnknown* self)
    {
        return daqUnknown_releaseRef(self);
    }

    static inline daqErrCode daqDispose(daqBaseObject* self)
    {
        return daqBaseObject_dispose(self);
    }

    static inline daqErrCode daqGetHashCode(daqBaseObject* self, daqCSizeT* hashCode)
    {
        return daqBaseObject_getHashCode(self, hashCode);
    }

    static inline daqErrCode daqEquals(daqBaseObject* self, daqBaseObject* other, daqCBool* equal)
    {
        return daqBaseObject_equals(self, other, equal);
    }

    static inline daqErrCode daqToString(daqBaseObject* self, daqCCharPtr* str)
    {
        return daqBaseObject_toString(self, str);
    }

    static inline daqErrCode daqQueryInterface(daqUnknown* self, daqIntfID intfId, daqBaseObject** interfacePtr)
    {
        return daqUnknown_queryInterface(self, intfId, interfacePtr);
    }

    static inline daqErrCode daqBorrowInterface(daqBaseObject* self, daqIntfID intfId, daqBaseObject** interfacePtr)
    {
        return daqBaseObject_borrowInterface(self, intfId, interfacePtr);
    }

    static inline daqBaseObject* daqQueryInterfacePtr(daqUnknown* self, daqIntfID intfId)
    {
        daqBaseObject* interfacePtr = NULL;
        daqErrCode err = daqUnknown_queryInterface(self, intfId, &interfacePtr);
        return err ? NULL : interfacePtr;
    }

    static inline daqBaseObject* daqBorrowInterfacePtr(daqBaseObject* self, daqIntfID intfId)
    {
        daqBaseObject* interfacePtr = NULL;
        daqErrCode err = daqBaseObject_borrowInterface(self, intfId, &interfacePtr);
        return err ? NULL : interfacePtr;
    }

#define DAQ_QUERY_INTERFACE(obj, intfId) daqQueryInterfacePtr((daqUnknown*)(obj), (intfId))
#define DAQ_BORROW_INTERFACE(obj, intfId) daqBorrowInterfacePtr((daqBaseObject*)(obj), (intfId))
#define DAQ_SUPPORTS_INTERFACE(obj, intfId) (daqBorrowInterfacePtr((daqBaseObject*)(obj), (intfId)) != NULL)

#ifdef __cplusplus
}
#endif