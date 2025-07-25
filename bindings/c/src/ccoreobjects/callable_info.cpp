//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:05:10.
// </auto-generated>
//------------------------------------------------------------------------------

#include <ccoreobjects/callable_info.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_CALLABLE_INFO_INTF_ID = { daq::ICallableInfo::Id.Data1, daq::ICallableInfo::Id.Data2, daq::ICallableInfo::Id.Data3, daq::ICallableInfo::Id.Data4_UInt64 };

daqErrCode daqCallableInfo_getReturnType(daqCallableInfo* self, daqCoreType* type)
{
    return reinterpret_cast<daq::ICallableInfo*>(self)->getReturnType(reinterpret_cast<daq::CoreType*>(type));
}

daqErrCode daqCallableInfo_getArguments(daqCallableInfo* self, daqList** argumentInfo)
{
    return reinterpret_cast<daq::ICallableInfo*>(self)->getArguments(reinterpret_cast<daq::IList**>(argumentInfo));
}

daqErrCode daqCallableInfo_isConst(daqCallableInfo* self, daqBool* constFlag)
{
    return reinterpret_cast<daq::ICallableInfo*>(self)->isConst(constFlag);
}

daqErrCode daqCallableInfo_createCallableInfo(daqCallableInfo** obj, daqList* argumentInfo, daqCoreType returnType, daqBool constFlag)
{
    daq::ICallableInfo* ptr = nullptr;
    daqErrCode err = daq::createCallableInfo(&ptr, reinterpret_cast<daq::IList*>(argumentInfo), static_cast<daq::CoreType>(returnType), constFlag);
    *obj = reinterpret_cast<daqCallableInfo*>(ptr);
    return err;
}
