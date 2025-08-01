//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:05:15.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoreobjects/permissions_builder.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_PERMISSIONS_BUILDER_INTF_ID = { daq::IPermissionsBuilder::Id.Data1, daq::IPermissionsBuilder::Id.Data2, daq::IPermissionsBuilder::Id.Data3, daq::IPermissionsBuilder::Id.Data4_UInt64 };

daqErrCode daqPermissionsBuilder_inherit(daqPermissionsBuilder* self, daqBool inherit)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->inherit(inherit);
}

daqErrCode daqPermissionsBuilder_assign(daqPermissionsBuilder* self, daqString* groupId, daqPermissionMaskBuilder* permissions)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->assign(reinterpret_cast<daq::IString*>(groupId), reinterpret_cast<daq::IPermissionMaskBuilder*>(permissions));
}

daqErrCode daqPermissionsBuilder_allow(daqPermissionsBuilder* self, daqString* groupId, daqPermissionMaskBuilder* permissions)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->allow(reinterpret_cast<daq::IString*>(groupId), reinterpret_cast<daq::IPermissionMaskBuilder*>(permissions));
}

daqErrCode daqPermissionsBuilder_deny(daqPermissionsBuilder* self, daqString* groupId, daqPermissionMaskBuilder* permissions)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->deny(reinterpret_cast<daq::IString*>(groupId), reinterpret_cast<daq::IPermissionMaskBuilder*>(permissions));
}

daqErrCode daqPermissionsBuilder_extend(daqPermissionsBuilder* self, daqPermissions* config)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->extend(reinterpret_cast<daq::IPermissions*>(config));
}

daqErrCode daqPermissionsBuilder_build(daqPermissionsBuilder* self, daqPermissions** configOut)
{
    return reinterpret_cast<daq::IPermissionsBuilder*>(self)->build(reinterpret_cast<daq::IPermissions**>(configOut));
}

daqErrCode daqPermissionsBuilder_createPermissionsBuilder(daqPermissionsBuilder** obj)
{
    daq::IPermissionsBuilder* ptr = nullptr;
    daqErrCode err = daq::createPermissionsBuilder(&ptr);
    *obj = reinterpret_cast<daqPermissionsBuilder*>(ptr);
    return err;
}
