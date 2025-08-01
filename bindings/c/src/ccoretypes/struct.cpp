//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 17:18:02.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoretypes/struct.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_STRUCT_INTF_ID = { daq::IStruct::Id.Data1, daq::IStruct::Id.Data2, daq::IStruct::Id.Data3, daq::IStruct::Id.Data4_UInt64 };

daqErrCode daqStruct_getStructType(daqStruct* self, daqStructType** type)
{
    return reinterpret_cast<daq::IStruct*>(self)->getStructType(reinterpret_cast<daq::IStructType**>(type));
}

daqErrCode daqStruct_getFieldNames(daqStruct* self, daqList** names)
{
    return reinterpret_cast<daq::IStruct*>(self)->getFieldNames(reinterpret_cast<daq::IList**>(names));
}

daqErrCode daqStruct_getFieldValues(daqStruct* self, daqList** values)
{
    return reinterpret_cast<daq::IStruct*>(self)->getFieldValues(reinterpret_cast<daq::IList**>(values));
}

daqErrCode daqStruct_get(daqStruct* self, daqString* name, daqBaseObject** field)
{
    return reinterpret_cast<daq::IStruct*>(self)->get(reinterpret_cast<daq::IString*>(name), reinterpret_cast<daq::IBaseObject**>(field));
}

daqErrCode daqStruct_getAsDictionary(daqStruct* self, daqDict** dictionary)
{
    return reinterpret_cast<daq::IStruct*>(self)->getAsDictionary(reinterpret_cast<daq::IDict**>(dictionary));
}

daqErrCode daqStruct_hasField(daqStruct* self, daqString* name, daqBool* contains)
{
    return reinterpret_cast<daq::IStruct*>(self)->hasField(reinterpret_cast<daq::IString*>(name), contains);
}

daqErrCode daqStruct_createStruct(daqStruct** obj, daqString* name, daqDict* fields, daqTypeManager* typeManager)
{
    daq::IStruct* ptr = nullptr;
    daqErrCode err = daq::createStruct(&ptr, reinterpret_cast<daq::IString*>(name), reinterpret_cast<daq::IDict*>(fields), reinterpret_cast<daq::ITypeManager*>(typeManager));
    *obj = reinterpret_cast<daqStruct*>(ptr);
    return err;
}

daqErrCode daqStruct_createStructFromBuilder(daqStruct** obj, daqStructBuilder* builder)
{
    daq::IStruct* ptr = nullptr;
    daqErrCode err = daq::createStructFromBuilder(&ptr, reinterpret_cast<daq::IStructBuilder*>(builder));
    *obj = reinterpret_cast<daqStruct*>(ptr);
    return err;
}
