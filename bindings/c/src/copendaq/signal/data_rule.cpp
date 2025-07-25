//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:07:41.
// </auto-generated>
//------------------------------------------------------------------------------

#include <copendaq/signal/data_rule.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_DATA_RULE_INTF_ID = { daq::IDataRule::Id.Data1, daq::IDataRule::Id.Data2, daq::IDataRule::Id.Data3, daq::IDataRule::Id.Data4_UInt64 };

daqErrCode daqDataRule_getType(daqDataRule* self, daqDataRuleType* type)
{
    return reinterpret_cast<daq::IDataRule*>(self)->getType(reinterpret_cast<daq::DataRuleType*>(type));
}

daqErrCode daqDataRule_getParameters(daqDataRule* self, daqDict** parameters)
{
    return reinterpret_cast<daq::IDataRule*>(self)->getParameters(reinterpret_cast<daq::IDict**>(parameters));
}

daqErrCode daqDataRule_createLinearDataRule(daqDataRule** obj, daqNumber* delta, daqNumber* start)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createLinearDataRule(&ptr, reinterpret_cast<daq::INumber*>(delta), reinterpret_cast<daq::INumber*>(start));
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}

daqErrCode daqDataRule_createConstantDataRule(daqDataRule** obj)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createConstantDataRule(&ptr);
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}

daqErrCode daqDataRule_createExplicitDataRule(daqDataRule** obj)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createExplicitDataRule(&ptr);
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}

daqErrCode daqDataRule_createExplicitDomainDataRule(daqDataRule** obj, daqNumber* minExpectedDelta, daqNumber* maxExpectedDelta)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createExplicitDomainDataRule(&ptr, reinterpret_cast<daq::INumber*>(minExpectedDelta), reinterpret_cast<daq::INumber*>(maxExpectedDelta));
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}

daqErrCode daqDataRule_createDataRule(daqDataRule** obj, daqDataRuleType ruleType, daqDict* parameters)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createDataRule(&ptr, static_cast<daq::DataRuleType>(ruleType), reinterpret_cast<daq::IDict*>(parameters));
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}

daqErrCode daqDataRule_createDataRuleFromBuilder(daqDataRule** obj, daqDataRuleBuilder* builder)
{
    daq::IDataRule* ptr = nullptr;
    daqErrCode err = daq::createDataRuleFromBuilder(&ptr, reinterpret_cast<daq::IDataRuleBuilder*>(builder));
    *obj = reinterpret_cast<daqDataRule*>(ptr);
    return err;
}
