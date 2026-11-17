#include <private/utils.h>

namespace copendaq::utils
{

daq::IntfID toDaqIntfId(daqIntfID id)
{
    auto intf = daq::IntfID{ id.Data1, id.Data2, id.Data3};
    intf.Data4_UInt64 = id.Data4; // could be assigned on construction with c++20
    return intf;
}

daq::ComplexFloat64 toDaqComplexFloat64(daqComplexFloat64 value)
{
    return daq::ComplexFloat64{value.real, value.imaginary};
}

daq::SourceLocation toDaqSourceLocation(daqSourceLocation location)
{
    return daq::SourceLocation{location.fileName, location.line, location.funcName};
}

daq::PropertyEventType toDaqPropertyEventType(daqPropertyEventType type)
{
    switch (type)
    {
        case daqPropertyEventType::daqPropertyEventTypeClear:
            return daq::PropertyEventType::Clear;
        case daqPropertyEventType::daqPropertyEventTypeRead:
            return daq::PropertyEventType::Read;
        case daqPropertyEventType::daqPropertyEventTypeUpdate:
        default:
            return daq::PropertyEventType::Update; // Default case to avoid compiler warnings
    }
}

daqPropertyEventType toCPropertyEventType(daq::PropertyEventType type)
{
    switch (type)
    {
        case daq::PropertyEventType::Clear:
            return daqPropertyEventType::daqPropertyEventTypeClear;
        case daq::PropertyEventType::Read:
            return daqPropertyEventType::daqPropertyEventTypeRead;
        case daq::PropertyEventType::Update:
        default:
            return daqPropertyEventType::daqPropertyEventTypeUpdate; // Default case to avoid compiler warnings
    }
}

}  // namespace copendaq::utils