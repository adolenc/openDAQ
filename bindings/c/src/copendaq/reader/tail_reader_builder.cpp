//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:07:31.
// </auto-generated>
//------------------------------------------------------------------------------

#include <copendaq/reader/tail_reader_builder.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_TAIL_READER_BUILDER_INTF_ID = { daq::ITailReaderBuilder::Id.Data1, daq::ITailReaderBuilder::Id.Data2, daq::ITailReaderBuilder::Id.Data3, daq::ITailReaderBuilder::Id.Data4_UInt64 };

daqErrCode daqTailReaderBuilder_build(daqTailReaderBuilder* self, daqTailReader** tailReader)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->build(reinterpret_cast<daq::ITailReader**>(tailReader));
}

daqErrCode daqTailReaderBuilder_setSignal(daqTailReaderBuilder* self, daqSignal* signal)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setSignal(reinterpret_cast<daq::ISignal*>(signal));
}

daqErrCode daqTailReaderBuilder_getSignal(daqTailReaderBuilder* self, daqSignal** signal)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getSignal(reinterpret_cast<daq::ISignal**>(signal));
}

daqErrCode daqTailReaderBuilder_setInputPort(daqTailReaderBuilder* self, daqInputPort* port)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setInputPort(reinterpret_cast<daq::IInputPort*>(port));
}

daqErrCode daqTailReaderBuilder_getInputPort(daqTailReaderBuilder* self, daqInputPort** port)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getInputPort(reinterpret_cast<daq::IInputPort**>(port));
}

daqErrCode daqTailReaderBuilder_setValueReadType(daqTailReaderBuilder* self, daqSampleType type)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setValueReadType(static_cast<daq::SampleType>(type));
}

daqErrCode daqTailReaderBuilder_getValueReadType(daqTailReaderBuilder* self, daqSampleType* type)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getValueReadType(reinterpret_cast<daq::SampleType*>(type));
}

daqErrCode daqTailReaderBuilder_setDomainReadType(daqTailReaderBuilder* self, daqSampleType type)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setDomainReadType(static_cast<daq::SampleType>(type));
}

daqErrCode daqTailReaderBuilder_getDomainReadType(daqTailReaderBuilder* self, daqSampleType* type)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getDomainReadType(reinterpret_cast<daq::SampleType*>(type));
}

daqErrCode daqTailReaderBuilder_setReadMode(daqTailReaderBuilder* self, daqReadMode mode)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setReadMode(static_cast<daq::ReadMode>(mode));
}

daqErrCode daqTailReaderBuilder_getReadMode(daqTailReaderBuilder* self, daqReadMode* mode)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getReadMode(reinterpret_cast<daq::ReadMode*>(mode));
}

daqErrCode daqTailReaderBuilder_setHistorySize(daqTailReaderBuilder* self, daqSizeT historySize)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setHistorySize(historySize);
}

daqErrCode daqTailReaderBuilder_getHistorySize(daqTailReaderBuilder* self, daqSizeT* historySize)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getHistorySize(historySize);
}

daqErrCode daqTailReaderBuilder_setSkipEvents(daqTailReaderBuilder* self, daqBool skipEvents)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->setSkipEvents(skipEvents);
}

daqErrCode daqTailReaderBuilder_getSkipEvents(daqTailReaderBuilder* self, daqBool* skipEvents)
{
    return reinterpret_cast<daq::ITailReaderBuilder*>(self)->getSkipEvents(skipEvents);
}

daqErrCode daqTailReaderBuilder_createTailReaderBuilder(daqTailReaderBuilder** obj)
{
    daq::ITailReaderBuilder* ptr = nullptr;
    daqErrCode err = daq::createTailReaderBuilder(&ptr);
    *obj = reinterpret_cast<daqTailReaderBuilder*>(ptr);
    return err;
}
