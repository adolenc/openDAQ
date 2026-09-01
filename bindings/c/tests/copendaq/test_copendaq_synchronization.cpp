#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqSynchronizationTest = testing::Test;

TEST_F(COpendaqSynchronizationTest, SyncComponent)
{
    daqListObject* sinks = nullptr;
    daqList_createList(&sinks);

    daqLoggerSink* sink = nullptr;
    daqLoggerSink_createStdErrLoggerSink(&sink);
    daqList_pushBack(sinks, sink);

    daqLogger* logger = nullptr;
    daqLogger_createLogger(&logger, sinks, daqLogLevel::daqLogLevelDebug);

    daqTypeManager* typeManager = nullptr;
    daqTypeManager_createTypeManager(&typeManager);

    daqDictObject *options = nullptr, *discoveryServers = nullptr;
    daqDict_createDict(&options);
    daqDict_createDict(&discoveryServers);

    daqContext* context = nullptr;
    daqContext_createContext(&context, nullptr, logger, typeManager, nullptr, nullptr, options, discoveryServers);

    daqSyncComponent* syncComponent = nullptr;
    daqStringObject* localId = nullptr;
    daqString_createString(&localId, "localId");

    daqSyncComponent_createSyncComponent(&syncComponent, context, nullptr, localId);

    daqSyncComponentPrivate* syncComponentPrivate = nullptr;
    daqBaseObject_borrowInterface(syncComponent, DAQ_SYNC_COMPONENT_PRIVATE_INTF_ID, (void**) &syncComponentPrivate);

    ASSERT_NE(syncComponentPrivate, nullptr);

    daqPropertyObject* interface = nullptr;
    daqStringObject* className = nullptr;
    daqString_createString(&className, "InterfaceClockSync");
    daqPropertyObject_createPropertyObjectWithClassAndManager(&interface, typeManager, className);

    daqErrCode err = daqSyncComponentPrivate_addInterface(syncComponentPrivate, interface);
    ASSERT_EQ(err, 0u);

    daqDictObject* interfaces = nullptr;
    daqSyncComponent_getInterfaces(syncComponent, &interfaces);
    ASSERT_NE(interfaces, nullptr);
    daqSizeT size = 0u;
    daqDict_getCount(interfaces, &size);
    ASSERT_EQ(size, 1u);

    daqBaseObject_releaseRef(interfaces);
    daqBaseObject_releaseRef(interface);
    daqBaseObject_releaseRef(className);
    daqBaseObject_releaseRef(syncComponent);
    daqBaseObject_releaseRef(localId);
    daqBaseObject_releaseRef(context);
    daqBaseObject_releaseRef(discoveryServers);
    daqBaseObject_releaseRef(options);
    daqBaseObject_releaseRef(typeManager);
    daqBaseObject_releaseRef(logger);
    daqBaseObject_releaseRef(sink);
    daqBaseObject_releaseRef(sinks);
}
