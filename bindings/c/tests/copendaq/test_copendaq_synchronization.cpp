#include <copendaq.h>
#include <coretypes/errorinfo.h>

#include <gtest/gtest.h>

using COpendaqSynchronizationTest = testing::Test;

TEST_F(COpendaqSynchronizationTest, SyncComponent)
{
    daqList* sinks = nullptr;
    daqList_createList(&sinks);

    daqLoggerSink* sink = nullptr;
    daqLoggerSink_createStdErrLoggerSink(&sink);
    daqList_pushBack(sinks, sink);

    daqLogger* logger = nullptr;
    daqLogger_createLogger(&logger, sinks, daqLogLevel::daqLogLevelDebug);

    daqTypeManager* typeManager = nullptr;
    daqTypeManager_createTypeManager(&typeManager);

    daqDict *options = nullptr, *discoveryServers = nullptr;
    daqDict_createDict(&options);
    daqDict_createDict(&discoveryServers);

    daqContext* context = nullptr;
    daqContext_createContext(&context, nullptr, logger, typeManager, nullptr, nullptr, options, discoveryServers);

    daqSyncComponent* syncComponent = nullptr;
    daqString* localId = nullptr;
    daqString_createString(&localId, "localId");

    daqSyncComponent_createSyncComponent(&syncComponent, context, nullptr, localId);

    daqSyncComponentPrivate* syncComponentPrivate = nullptr;
    daqBaseObject_borrowInterface(syncComponent, DAQ_SYNC_COMPONENT_PRIVATE_INTF_ID, (void**) &syncComponentPrivate);

    ASSERT_NE(syncComponentPrivate, nullptr);

    daqPropertyObject* interface = nullptr;
    daqString* className = nullptr;
    daqString_createString(&className, "InterfaceClockSync");
    daqPropertyObject_createPropertyObjectWithClassAndManager(&interface, typeManager, className);

    daqErrCode err = daqSyncComponentPrivate_addInterface(syncComponentPrivate, interface);
    ASSERT_EQ(err, 0u);

    daqDict* interfaces = nullptr;
    daqSyncComponent_getInterfaces(syncComponent, &interfaces);
    ASSERT_NE(interfaces, nullptr);
    daqCSizeT size = 0u;
    daqDict_getCount(interfaces, &size);
    ASSERT_EQ(size, 1u);

    daqUnknown_releaseRef(interfaces);
    daqUnknown_releaseRef(interface);
    daqUnknown_releaseRef(className);
    daqUnknown_releaseRef(syncComponent);
    daqUnknown_releaseRef(localId);
    daqUnknown_releaseRef(context);
    daqUnknown_releaseRef(discoveryServers);
    daqUnknown_releaseRef(options);
    daqUnknown_releaseRef(typeManager);
    daqUnknown_releaseRef(logger);
    daqUnknown_releaseRef(sink);
    daqUnknown_releaseRef(sinks);
}

TEST_F(COpendaqSynchronizationTest, Synchronization)
{
    daqList* sinks = nullptr;
    daqList_createList(&sinks);

    daqLoggerSink* sink = nullptr;
    daqLoggerSink_createStdErrLoggerSink(&sink);
    daqList_pushBack(sinks, sink);

    daqLogger* logger = nullptr;
    daqLogger_createLogger(&logger, sinks, daqLogLevel::daqLogLevelDebug);

    daqTypeManager* typeManager = nullptr;
    daqTypeManager_createTypeManager(&typeManager);

    daqDict *options = nullptr, *discoveryServers = nullptr;
    daqDict_createDict(&options);
    daqDict_createDict(&discoveryServers);

    // The context registers the synchronization status types in the type manager
    daqContext* context = nullptr;
    daqContext_createContext(&context, nullptr, logger, typeManager, nullptr, nullptr, options, discoveryServers);

    daqString* deviceId = nullptr;
    daqString_createString(&deviceId, "testDevice");

    daqSynchronization* synchronization = nullptr;
    ASSERT_EQ(daqSynchronization_createSynchronization(&synchronization, typeManager, deviceId), 0u);
    ASSERT_NE(synchronization, nullptr);

    daqCSizeT count = 0u;

    daqDict* interfaces = nullptr;
    daqSynchronization_getInterfaces(synchronization, &interfaces);
    daqDict_getCount(interfaces, &count);
    ASSERT_EQ(count, 1u);

    daqDict* sources = nullptr;
    daqSynchronization_getAvailableSources(synchronization, &sources);
    daqDict_getCount(sources, &count);
    ASSERT_EQ(count, 1u);

    daqList* referenceDomainIds = nullptr;
    daqSynchronization_getReferenceDomainIds(synchronization, &referenceDomainIds);
    daqList_getCount(referenceDomainIds, &count);
    ASSERT_EQ(count, 1u);

    daqSyncInterface* source = nullptr;
    daqSynchronization_getSource(synchronization, &source);
    ASSERT_NE(source, nullptr);

    daqString* id = nullptr;
    daqSyncInterface_getId(source, &id);
    daqString* syncType = nullptr;
    daqSyncInterface_getSyncType(source, &syncType);
    daqString* referenceDomainId = nullptr;
    daqSyncInterface_getReferenceDomainId(source, &referenceDomainId);
    daqCBool sourceSupported = daqFalse;
    daqSyncInterface_getSourceSupported(source, &sourceSupported);
    daqSyncMode mode = daqSyncModeOff;
    daqSyncInterface_getMode(source, &mode);

    daqCConstCharPtr idStr = nullptr;
    daqString_getCharPtr(id, &idStr);
    daqCConstCharPtr syncTypeStr = nullptr;
    daqString_getCharPtr(syncType, &syncTypeStr);
    daqCConstCharPtr referenceDomainIdStr = nullptr;
    daqString_getCharPtr(referenceDomainId, &referenceDomainIdStr);

    ASSERT_STREQ(idStr, "ClockSyncInterface");
    ASSERT_STREQ(syncTypeStr, "local");
    ASSERT_STREQ(referenceDomainIdStr, "local:testDevice");
    ASSERT_TRUE(sourceSupported);
    ASSERT_EQ(mode, daqSyncModeInput);

    daqString* unknownSource = nullptr;
    daqString_createString(&unknownSource, "DoesNotExist");
    ASSERT_NE(daqSynchronization_setSource(synchronization, unknownSource), 0u);
    daqClearErrorInfo();

    daqUnknown_releaseRef(unknownSource);
    daqUnknown_releaseRef(referenceDomainId);
    daqUnknown_releaseRef(syncType);
    daqUnknown_releaseRef(id);
    daqUnknown_releaseRef(source);
    daqUnknown_releaseRef(referenceDomainIds);
    daqUnknown_releaseRef(sources);
    daqUnknown_releaseRef(interfaces);
    daqUnknown_releaseRef(synchronization);
    daqUnknown_releaseRef(deviceId);
    daqUnknown_releaseRef(context);
    daqUnknown_releaseRef(discoveryServers);
    daqUnknown_releaseRef(options);
    daqUnknown_releaseRef(typeManager);
    daqUnknown_releaseRef(logger);
    daqUnknown_releaseRef(sink);
    daqUnknown_releaseRef(sinks);
}
