//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 06.06.2025 19:05:55.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoreobjects/property_object_class_internal.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_PROPERTY_OBJECT_CLASS_INTERNAL_INTF_ID = { daq::IPropertyObjectClassInternal::Id.Data1, daq::IPropertyObjectClassInternal::Id.Data2, daq::IPropertyObjectClassInternal::Id.Data3, daq::IPropertyObjectClassInternal::Id.Data4_UInt64 };

daqErrCode daqPropertyObjectClassInternal_clone(daqPropertyObjectClassInternal* self, daqPropertyObjectClass** cloned, daqTypeManager* typeManager)
{
    return reinterpret_cast<daq::IPropertyObjectClassInternal*>(self)->clone(reinterpret_cast<daq::IPropertyObjectClass**>(cloned), reinterpret_cast<daq::ITypeManager*>(typeManager));
}
