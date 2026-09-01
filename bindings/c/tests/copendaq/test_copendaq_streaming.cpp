#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqStreamingTest = testing::Test;

TEST_F(COpendaqStreamingTest, StreamingType)
{
    daqStreamingType* streamingType = nullptr;
    daqStringObject* id = nullptr;
    daqString_createString(&id, "streamingType");
    daqStringObject* name = nullptr;
    daqString_createString(&name, "streamingTypeName");
    daqStringObject* description = nullptr;
    daqString_createString(&description, "streamingTypeDescription");
    daqStringObject* prefix = nullptr;
    daqString_createString(&prefix, "streamingTypePrefix");
    daqStreamingType_createStreamingType(&streamingType, id, name, description, prefix, nullptr);
    ASSERT_NE(streamingType, nullptr);
    daqBaseObject_releaseRef(streamingType);
    daqBaseObject_releaseRef(id);
    daqBaseObject_releaseRef(name);
    daqBaseObject_releaseRef(description);
    daqBaseObject_releaseRef(prefix);
}

TEST_F(COpendaqStreamingTest, SubscriptionEventArgs)
{
    daqSubscriptionEventArgs* subscriptionEventArgs = nullptr;
    daqStringObject* streamingConnectionString = nullptr;
    daqString_createString(&streamingConnectionString, "streamingConnectionString");
    daqSubscriptionEventType type = daqSubscriptionEventType::daqSubscriptionEventTypeUnsubscribed;

    daqSubscriptionEventArgs_createSubscriptionEventArgs(&subscriptionEventArgs, streamingConnectionString, type);
    ASSERT_NE(subscriptionEventArgs, nullptr);
    daqBaseObject_releaseRef(subscriptionEventArgs);
    daqBaseObject_releaseRef(streamingConnectionString);
}
