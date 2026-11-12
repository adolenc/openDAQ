#include "ccoretypes/factories.h"

#include <opendaq/opendaq.h>

#include "private/c_event_handler_impl.h"

daqErrCode daqSerializer_createJsonSerializer(daqSerializer** obj, daqCBool pretty)
{
    daq::ISerializer* ptr = nullptr;
    daqErrCode err = daq::createJsonSerializer(&ptr, pretty);
    *obj = reinterpret_cast<daqSerializer*>(ptr);
    return err;
}

daqErrCode daqSerializer_createJsonSerializerWithVersion(daqSerializer** obj, daqCInt version, daqCBool pretty)
{
    daq::ISerializer* ptr = nullptr;
    daqErrCode err = daq::createJsonSerializerWithVersion(&ptr, version, pretty);
    *obj = reinterpret_cast<daqSerializer*>(ptr);
    return err;
}

daqErrCode daqEventHandler_createEventHandler(daqEventHandler** obj, daqEventCall call)
{
    daq::IEventHandler* ptr = nullptr;
    daqErrCode err = daq::createObjectForwarding<daq::IEventHandler, daq::CEventHandlerImpl>(&ptr, call);
    *obj = reinterpret_cast<daqEventHandler*>(ptr);
    return err;
}
namespace
{
    // Hands the user data to its release function when the object that holds it is destroyed.
    class UserData
    {
    public:
        UserData(void* data, daqFreeUserData freeData)
            : data(data)
            , freeData(freeData)
        {
        }

        UserData(const UserData&) = delete;
        UserData& operator=(const UserData&) = delete;

        ~UserData()
        {
            if (freeData)
                freeData(data);
        }

        void* const data;

    private:
        const daqFreeUserData freeData;
    };

    class CFunctionWithData : public daq::ImplementationOf<daq::IFunction, daq::ICoreType>
    {
    public:
        CFunctionWithData(daqFuncCallWithData callback, void* data, daqFreeUserData freeData)
            : callback(callback)
            , userData(data, freeData)
        {
        }

        daq::ErrCode INTERFACE_FUNC call(daq::IBaseObject* params, daq::IBaseObject** result) override
        {
            OPENDAQ_PARAM_NOT_NULL(result);
            return callback(params, reinterpret_cast<daqBaseObject**>(result), userData.data);
        }

        daq::ErrCode INTERFACE_FUNC getCoreType(daq::CoreType* coreType) override
        {
            OPENDAQ_PARAM_NOT_NULL(coreType);
            *coreType = daq::ctFunc;
            return OPENDAQ_SUCCESS;
        }

    private:
        const daqFuncCallWithData callback;
        UserData userData;
    };

    class CProcedureWithData : public daq::ImplementationOf<daq::IProcedure, daq::ICoreType>
    {
    public:
        CProcedureWithData(daqProcCallWithData callback, void* data, daqFreeUserData freeData)
            : callback(callback)
            , userData(data, freeData)
        {
        }

        daq::ErrCode INTERFACE_FUNC dispatch(daq::IBaseObject* params) override
        {
            return callback(params, userData.data);
        }

        daq::ErrCode INTERFACE_FUNC getCoreType(daq::CoreType* coreType) override
        {
            OPENDAQ_PARAM_NOT_NULL(coreType);
            *coreType = daq::ctProc;
            return OPENDAQ_SUCCESS;
        }

    private:
        const daqProcCallWithData callback;
        UserData userData;
    };

    class CEventHandlerWithData : public daq::ImplementationOf<daq::IEventHandler>
    {
    public:
        CEventHandlerWithData(daqEventCallWithData callback, void* data, daqFreeUserData freeData)
            : callback(callback)
            , userData(data, freeData)
        {
        }

        // As in CEventHandlerImpl, the callback is given a reference to the sender and the arguments.
        daq::ErrCode INTERFACE_FUNC handleEvent(daq::IBaseObject* sender, daq::IEventArgs* eventArgs) override
        {
            if (sender)
                sender->addRef();
            if (eventArgs)
                eventArgs->addRef();
            callback(sender, eventArgs, userData.data);
            return OPENDAQ_SUCCESS;
        }

    private:
        const daqEventCallWithData callback;
        UserData userData;
    };

    template <typename TInterface, typename TImpl, typename THandle, typename TCallback>
    daqErrCode createWithData(THandle** obj, TCallback callback, void* userData, daqFreeUserData freeUserData)
    {
        daqErrCode err = OPENDAQ_ERR_ARGUMENT_NULL;
        if (obj != nullptr && callback != nullptr)
        {
            TInterface* ptr = nullptr;
            err = daq::createObjectForwarding<TInterface, TImpl>(&ptr, callback, userData, freeUserData);
            *obj = reinterpret_cast<THandle*>(ptr);
            // Once constructed, the object releases the user data; it fails only before that.
            if (OPENDAQ_SUCCEEDED(err))
                return err;
        }
        if (freeUserData)
            freeUserData(userData);
        return err;
    }
}

daqErrCode daqFunction_createFunctionWithData(daqFunction** obj, daqFuncCallWithData call, void* userData, daqFreeUserData freeUserData)
{
    return createWithData<daq::IFunction, CFunctionWithData>(obj, call, userData, freeUserData);
}

daqErrCode daqProcedure_createProcedureWithData(daqProcedure** obj, daqProcCallWithData call, void* userData, daqFreeUserData freeUserData)
{
    return createWithData<daq::IProcedure, CProcedureWithData>(obj, call, userData, freeUserData);
}

daqErrCode daqEventHandler_createEventHandlerWithData(daqEventHandler** obj, daqEventCallWithData call, void* userData, daqFreeUserData freeUserData)
{
    return createWithData<daq::IEventHandler, CEventHandlerWithData>(obj, call, userData, freeUserData);
}
