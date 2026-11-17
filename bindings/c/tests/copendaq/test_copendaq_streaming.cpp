#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqStreamingTest = testing::Test;

TEST_F(COpendaqStreamingTest, StreamingType)
{
    daqStreamingType* streamingType = nullptr;
    daqString* id = nullptr;
    daqString_createString(&id, "streamingType");
    daqString* name = nullptr;
    daqString_createString(&name, "streamingTypeName");
    daqString* description = nullptr;
    daqString_createString(&description, "streamingTypeDescription");
    daqString* prefix = nullptr;
    daqString_createString(&prefix, "streamingTypePrefix");
    daqStreamingType_createStreamingType(&streamingType, id, name, description, prefix, nullptr);
    ASSERT_NE(streamingType, nullptr);
    daqUnknown_releaseRef(streamingType);
    daqUnknown_releaseRef(id);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(description);
    daqUnknown_releaseRef(prefix);
}

TEST_F(COpendaqStreamingTest, SubscriptionEventArgs)
{
    daqSubscriptionEventArgs* subscriptionEventArgs = nullptr;
    daqString* streamingConnectionString = nullptr;
    daqString_createString(&streamingConnectionString, "streamingConnectionString");
    daqSubscriptionEventType type = daqSubscriptionEventType::daqSubscriptionEventTypeUnsubscribed;

    daqSubscriptionEventArgs_createSubscriptionEventArgs(&subscriptionEventArgs, streamingConnectionString, type);
    ASSERT_NE(subscriptionEventArgs, nullptr);
    daqUnknown_releaseRef(subscriptionEventArgs);
    daqUnknown_releaseRef(streamingConnectionString);
}
