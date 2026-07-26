#include "ccoretypes/base_object.h"

#include <opendaq/opendaq.h>

#include "copendaq_private.h"

const daqIntfID DAQ_UNKNOWN_INTF_ID = { daq::IUnknown::Id.Data1, daq::IUnknown::Id.Data2, daq::IUnknown::Id.Data3, daq::IUnknown::Id.Data4_UInt64 };
const daqIntfID DAQ_BASE_OBJECT_INTF_ID = { daq::IBaseObject::Id.Data1, daq::IBaseObject::Id.Data2 , daq::IBaseObject::Id.Data3, daq::IBaseObject::Id.Data4_UInt64 };

daqErrCode daqUnknown_queryInterface(daqUnknown* self, daqIntfID intfId, daqBaseObject** interfacePtr)
{
    return static_cast<daq::IUnknown*>(self)->queryInterface(copendaq::utils::toDaqIntfId(intfId), interfacePtr);
}

int daqUnknown_addRef(daqUnknown* self)
{
    return static_cast<daq::IUnknown*>(self)->addRef();
}

int daqUnknown_releaseRef(daqUnknown* self)
{
    return static_cast<daq::IUnknown*>(self)->releaseRef();
}

daqErrCode daqBaseObject_dispose(daqBaseObject* self)
{
    return static_cast<daq::IBaseObject*>(self)->dispose();
}

daqErrCode daqBaseObject_getHashCode(daqBaseObject* self, daqCSizeT* hashCode)
{
    return static_cast<daq::IBaseObject*>(self)->getHashCode(hashCode);
}

daqErrCode daqBaseObject_equals(daqBaseObject* self, void* other, daqCBool* equal)
{
    return static_cast<daq::IBaseObject*>(self)->equals(static_cast<daq::IBaseObject*>(other), equal);
}

daqErrCode daqBaseObject_toString(daqBaseObject* self, daqCCharPtr* str)
{
    return static_cast<daq::IBaseObject*>(self)->toString(str);
}

daqErrCode daqBaseObject_createBaseObject(daqBaseObject** baseObject)
{
    *baseObject = reinterpret_cast<daqBaseObject*>(daq::BaseObject_Create());
    return *baseObject == nullptr ? OPENDAQ_ERR_NOMEMORY : OPENDAQ_SUCCESS;
}

daqErrCode daqBaseObject_borrowInterface(daqBaseObject* self, daqIntfID intfId, daqBaseObject** interfacePtr)
{
    return static_cast<daq::IBaseObject*>(self)->borrowInterface(copendaq::utils::toDaqIntfId(intfId), interfacePtr);
}