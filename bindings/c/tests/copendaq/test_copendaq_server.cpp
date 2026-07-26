#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqServerTest = testing::Test;

TEST_F(COpendaqServerTest, ServerType)
{
    daqString* id = nullptr;
    daqString_createString(&id, "id");
    daqString* name = nullptr;
    daqString_createString(&name, "name");
    daqString* description = nullptr;
    daqString_createString(&description, "description");
    daqPropertyObject* defaultConfig = nullptr;
    daqPropertyObject_createPropertyObject(&defaultConfig);

    daqServerType* obj = nullptr;
    daqServerType_createServerType(&obj, id, name, description, defaultConfig);
    ASSERT_NE(obj, nullptr);

    daqUnknown_releaseRef(obj);
    daqUnknown_releaseRef(defaultConfig);
    daqUnknown_releaseRef(description);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(id);
}
