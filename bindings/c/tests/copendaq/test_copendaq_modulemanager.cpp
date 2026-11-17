#include <copendaq.h>

#include <gtest/gtest.h>

class COpendaqModuleManagerTest : public testing::Test
{
    void SetUp() override
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

        daqContext_createContext(&ctx, nullptr, logger, typeManager, nullptr, nullptr, options, discoveryServers);

        daqUnknown_releaseRef(discoveryServers);
        daqUnknown_releaseRef(options);
        daqUnknown_releaseRef(typeManager);
        daqUnknown_releaseRef(logger);
        daqUnknown_releaseRef(sink);
        daqUnknown_releaseRef(sinks);
    }

    void TearDown() override
    {
        daqUnknown_releaseRef(ctx);
    }

protected:
    daqContext* ctx = nullptr;
};

// Even just constructing and destroying the ModuleManager produces leaks on some platforms
TEST_F(COpendaqModuleManagerTest, DISABLED_ModuleManager)
{
    daqModuleManager* moduleManager = nullptr;

    daqString* path = nullptr;
    daqString_createString(&path, ".");
    daqModuleManager_createModuleManager(&moduleManager, path);
    ASSERT_NE(moduleManager, nullptr);

    daqModuleManager_loadModules(moduleManager, ctx);

    daqList* modules = nullptr;
    daqModuleManager_getModules(moduleManager, &modules);

    ASSERT_NE(modules, nullptr);
    daqCSizeT size = 0;
    daqList_getCount(modules, &size);
    ASSERT_GT(size, 0u);

    daqModule* module = nullptr;
    daqList_getItemAt(modules, 0, (daqBaseObject**) &module);
    ASSERT_NE(module, nullptr);

    daqModuleInfo* info = nullptr;
    daqModule_getModuleInfo(module, &info);
    ASSERT_NE(info, nullptr);

    daqString* name = nullptr;
    daqModuleInfo_getId(info, &name);
    ASSERT_NE(name, nullptr);

    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(info);
    daqUnknown_releaseRef(module);
    daqUnknown_releaseRef(modules);
    daqUnknown_releaseRef(moduleManager);
    daqUnknown_releaseRef(path);
}
