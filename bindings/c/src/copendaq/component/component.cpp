//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (CGenerator v0.7.0) on 03.06.2025 22:06:50.
// </auto-generated>
//------------------------------------------------------------------------------

#include <copendaq/component/component.h>

#include <opendaq/opendaq.h>

#include <copendaq_private.h>

const daqIntfID DAQ_COMPONENT_INTF_ID = { daq::IComponent::Id.Data1, daq::IComponent::Id.Data2, daq::IComponent::Id.Data3, daq::IComponent::Id.Data4_UInt64 };

daqErrCode daqComponent_getLocalId(daqComponent* self, daqString** localId)
{
    return reinterpret_cast<daq::IComponent*>(self)->getLocalId(reinterpret_cast<daq::IString**>(localId));
}

daqErrCode daqComponent_getGlobalId(daqComponent* self, daqString** globalId)
{
    return reinterpret_cast<daq::IComponent*>(self)->getGlobalId(reinterpret_cast<daq::IString**>(globalId));
}

daqErrCode daqComponent_getActive(daqComponent* self, daqBool* active)
{
    return reinterpret_cast<daq::IComponent*>(self)->getActive(active);
}

daqErrCode daqComponent_setActive(daqComponent* self, daqBool active)
{
    return reinterpret_cast<daq::IComponent*>(self)->setActive(active);
}

daqErrCode daqComponent_getContext(daqComponent* self, daqContext** context)
{
    return reinterpret_cast<daq::IComponent*>(self)->getContext(reinterpret_cast<daq::IContext**>(context));
}

daqErrCode daqComponent_getParent(daqComponent* self, daqComponent** parent)
{
    return reinterpret_cast<daq::IComponent*>(self)->getParent(reinterpret_cast<daq::IComponent**>(parent));
}

daqErrCode daqComponent_getName(daqComponent* self, daqString** name)
{
    return reinterpret_cast<daq::IComponent*>(self)->getName(reinterpret_cast<daq::IString**>(name));
}

daqErrCode daqComponent_setName(daqComponent* self, daqString* name)
{
    return reinterpret_cast<daq::IComponent*>(self)->setName(reinterpret_cast<daq::IString*>(name));
}

daqErrCode daqComponent_getDescription(daqComponent* self, daqString** description)
{
    return reinterpret_cast<daq::IComponent*>(self)->getDescription(reinterpret_cast<daq::IString**>(description));
}

daqErrCode daqComponent_setDescription(daqComponent* self, daqString* description)
{
    return reinterpret_cast<daq::IComponent*>(self)->setDescription(reinterpret_cast<daq::IString*>(description));
}

daqErrCode daqComponent_getTags(daqComponent* self, daqTags** tags)
{
    return reinterpret_cast<daq::IComponent*>(self)->getTags(reinterpret_cast<daq::ITags**>(tags));
}

daqErrCode daqComponent_getVisible(daqComponent* self, daqBool* visible)
{
    return reinterpret_cast<daq::IComponent*>(self)->getVisible(visible);
}

daqErrCode daqComponent_setVisible(daqComponent* self, daqBool visible)
{
    return reinterpret_cast<daq::IComponent*>(self)->setVisible(visible);
}

daqErrCode daqComponent_getLockedAttributes(daqComponent* self, daqList** attributes)
{
    return reinterpret_cast<daq::IComponent*>(self)->getLockedAttributes(reinterpret_cast<daq::IList**>(attributes));
}

daqErrCode daqComponent_getOnComponentCoreEvent(daqComponent* self, daqEvent** event)
{
    return reinterpret_cast<daq::IComponent*>(self)->getOnComponentCoreEvent(reinterpret_cast<daq::IEvent**>(event));
}

daqErrCode daqComponent_getStatusContainer(daqComponent* self, daqComponentStatusContainer** statusContainer)
{
    return reinterpret_cast<daq::IComponent*>(self)->getStatusContainer(reinterpret_cast<daq::IComponentStatusContainer**>(statusContainer));
}

daqErrCode daqComponent_findComponent(daqComponent* self, daqString* id, daqComponent** outComponent)
{
    return reinterpret_cast<daq::IComponent*>(self)->findComponent(reinterpret_cast<daq::IString*>(id), reinterpret_cast<daq::IComponent**>(outComponent));
}

daqErrCode daqComponent_getOperationMode(daqComponent* self, daqOperationModeType* modeType)
{
    return reinterpret_cast<daq::IComponent*>(self)->getOperationMode(reinterpret_cast<daq::OperationModeType*>(modeType));
}

daqErrCode daqComponent_createComponent(daqComponent** obj, daqContext* context, daqComponent* parent, daqString* localId, daqString* className)
{
    daq::IComponent* ptr = nullptr;
    daqErrCode err = daq::createComponent(&ptr, reinterpret_cast<daq::IContext*>(context), reinterpret_cast<daq::IComponent*>(parent), reinterpret_cast<daq::IString*>(localId), reinterpret_cast<daq::IString*>(className));
    *obj = reinterpret_cast<daqComponent*>(ptr);
    return err;
}
