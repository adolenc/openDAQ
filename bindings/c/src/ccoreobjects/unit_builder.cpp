//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:05:20.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoreobjects/unit_builder.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_UNIT_BUILDER_INTF_ID = { daq::IUnitBuilder::Id.Data1, daq::IUnitBuilder::Id.Data2, daq::IUnitBuilder::Id.Data3, daq::IUnitBuilder::Id.Data4_UInt64 };

daqErrCode daqUnitBuilder_build(daqUnitBuilder* self, daqUnit** unit)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->build(reinterpret_cast<daq::IUnit**>(unit));
}

daqErrCode daqUnitBuilder_setId(daqUnitBuilder* self, daqInt id)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->setId(id);
}

daqErrCode daqUnitBuilder_getId(daqUnitBuilder* self, daqInt* id)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->getId(id);
}

daqErrCode daqUnitBuilder_setSymbol(daqUnitBuilder* self, daqString* symbol)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->setSymbol(reinterpret_cast<daq::IString*>(symbol));
}

daqErrCode daqUnitBuilder_getSymbol(daqUnitBuilder* self, daqString** symbol)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->getSymbol(reinterpret_cast<daq::IString**>(symbol));
}

daqErrCode daqUnitBuilder_setName(daqUnitBuilder* self, daqString* name)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->setName(reinterpret_cast<daq::IString*>(name));
}

daqErrCode daqUnitBuilder_getName(daqUnitBuilder* self, daqString** name)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->getName(reinterpret_cast<daq::IString**>(name));
}

daqErrCode daqUnitBuilder_setQuantity(daqUnitBuilder* self, daqString* quantity)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->setQuantity(reinterpret_cast<daq::IString*>(quantity));
}

daqErrCode daqUnitBuilder_getQuantity(daqUnitBuilder* self, daqString** quantity)
{
    return reinterpret_cast<daq::IUnitBuilder*>(self)->getQuantity(reinterpret_cast<daq::IString**>(quantity));
}

daqErrCode daqUnitBuilder_createUnitBuilder(daqUnitBuilder** obj)
{
    daq::IUnitBuilder* ptr = nullptr;
    daqErrCode err = daq::createUnitBuilder(&ptr);
    *obj = reinterpret_cast<daqUnitBuilder*>(ptr);
    return err;
}

daqErrCode daqUnitBuilder_createUnitBuilderFromExisting(daqUnitBuilder** obj, daqUnit* unitToCopy)
{
    daq::IUnitBuilder* ptr = nullptr;
    daqErrCode err = daq::createUnitBuilderFromExisting(&ptr, reinterpret_cast<daq::IUnit*>(unitToCopy));
    *obj = reinterpret_cast<daqUnitBuilder*>(ptr);
    return err;
}
