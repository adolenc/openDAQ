//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 17:17:48.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoretypes/enumeration.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_ENUMERATION_INTF_ID = { daq::IEnumeration::Id.Data1, daq::IEnumeration::Id.Data2, daq::IEnumeration::Id.Data3, daq::IEnumeration::Id.Data4_UInt64 };

daqErrCode daqEnumeration_getEnumerationType(daqEnumeration* self, daqEnumerationType** type)
{
    return reinterpret_cast<daq::IEnumeration*>(self)->getEnumerationType(reinterpret_cast<daq::IEnumerationType**>(type));
}

daqErrCode daqEnumeration_getValue(daqEnumeration* self, daqString** value)
{
    return reinterpret_cast<daq::IEnumeration*>(self)->getValue(reinterpret_cast<daq::IString**>(value));
}

daqErrCode daqEnumeration_getIntValue(daqEnumeration* self, daqInt* value)
{
    return reinterpret_cast<daq::IEnumeration*>(self)->getIntValue(value);
}

daqErrCode daqEnumeration_createEnumeration(daqEnumeration** obj, daqString* name, daqString* value, daqTypeManager* typeManager)
{
    daq::IEnumeration* ptr = nullptr;
    daqErrCode err = daq::createEnumeration(&ptr, reinterpret_cast<daq::IString*>(name), reinterpret_cast<daq::IString*>(value), reinterpret_cast<daq::ITypeManager*>(typeManager));
    *obj = reinterpret_cast<daqEnumeration*>(ptr);
    return err;
}

daqErrCode daqEnumeration_createEnumerationWithIntValue(daqEnumeration** obj, daqString* name, daqInteger* value, daqTypeManager* typeManager)
{
    daq::IEnumeration* ptr = nullptr;
    daqErrCode err = daq::createEnumerationWithIntValue(&ptr, reinterpret_cast<daq::IString*>(name), reinterpret_cast<daq::IInteger*>(value), reinterpret_cast<daq::ITypeManager*>(typeManager));
    *obj = reinterpret_cast<daqEnumeration*>(ptr);
    return err;
}

daqErrCode daqEnumeration_createEnumerationWithType(daqEnumeration** obj, daqEnumerationType* type, daqString* value)
{
    daq::IEnumeration* ptr = nullptr;
    daqErrCode err = daq::createEnumerationWithType(&ptr, reinterpret_cast<daq::IEnumerationType*>(type), reinterpret_cast<daq::IString*>(value));
    *obj = reinterpret_cast<daqEnumeration*>(ptr);
    return err;
}

daqErrCode daqEnumeration_createEnumerationWithIntValueAndType(daqEnumeration** obj, daqEnumerationType* type, daqInteger* value)
{
    daq::IEnumeration* ptr = nullptr;
    daqErrCode err = daq::createEnumerationWithIntValueAndType(&ptr, reinterpret_cast<daq::IEnumerationType*>(type), reinterpret_cast<daq::IInteger*>(value));
    *obj = reinterpret_cast<daqEnumeration*>(ptr);
    return err;
}
