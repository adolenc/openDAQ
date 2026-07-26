#include <copendaq.h>

#include <gtest/gtest.h>

using COpendaqSignalTest = testing::Test;

daqContext* createContext()
{
    daqContext* ctx = nullptr;
    daqList* sinks = nullptr;
    daqList_createList(&sinks);

    daqLoggerSink* sink = nullptr;
    daqLoggerSink_createStdErrLoggerSink(&sink);
    daqList_pushBack(sinks, sink);
    daqUnknown_releaseRef(sink);

    daqLogger* logger = nullptr;
    daqLogger_createLogger(&logger, sinks, daqLogLevel::daqLogLevelDebug);
    daqUnknown_releaseRef(sinks);

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

    return ctx;
}

daqDataDescriptor* createValueDescriptor()
{
    daqDataDescriptor* descriptor = nullptr;
    daqDataDescriptorBuilder* builder = nullptr;
    daqDataDescriptorBuilder_createDataDescriptorBuilder(&builder);
    daqDataDescriptorBuilder_setSampleType(builder, daqSampleType::daqSampleTypeInt64);

    daqUnitBuilder* unitBuilder = nullptr;
    daqUnitBuilder_createUnitBuilder(&unitBuilder);

    daqString* unitName = nullptr;
    daqString_createString(&unitName, "volts");
    daqUnitBuilder_setName(unitBuilder, unitName);
    daqUnknown_releaseRef(unitName);

    daqString* unitSymbol = nullptr;
    daqString_createString(&unitSymbol, "V");
    daqUnitBuilder_setSymbol(unitBuilder, unitSymbol);
    daqUnknown_releaseRef(unitSymbol);

    daqString* unitQuantity = nullptr;
    daqString_createString(&unitQuantity, "voltage");
    daqUnitBuilder_setQuantity(unitBuilder, unitQuantity);
    daqUnknown_releaseRef(unitQuantity);

    daqUnitBuilder_setId(unitBuilder, -1);

    daqUnit* unit = nullptr;
    daqUnitBuilder_build(unitBuilder, &unit);
    daqDataDescriptorBuilder_setUnit(builder, unit);
    daqUnknown_releaseRef(unitBuilder);
    daqUnknown_releaseRef(unit);

    daqString* name = nullptr;
    daqString_createString(&name, "vals");
    daqDataDescriptorBuilder_setName(builder, name);
    daqUnknown_releaseRef(name);

    daqDataDescriptorBuilder_build(builder, &descriptor);
    daqUnknown_releaseRef(builder);

    return descriptor;
}

daqDataDescriptor* createDomainDescriptor()
{
    daqDataDescriptor* descriptor = nullptr;

    daqDataDescriptorBuilder* builder = nullptr;
    daqDataDescriptorBuilder_createDataDescriptorBuilder(&builder);

    daqDataDescriptorBuilder_setSampleType(builder, daqSampleType::daqSampleTypeInt64);

    daqUnitBuilder* unitBuilder = nullptr;
    daqUnitBuilder_createUnitBuilder(&unitBuilder);

    daqString* unitName = nullptr;
    daqString_createString(&unitName, "seconds");
    daqUnitBuilder_setName(unitBuilder, unitName);
    daqUnknown_releaseRef(unitName);

    daqString* unitSymbol = nullptr;
    daqString_createString(&unitSymbol, "s");
    daqUnitBuilder_setSymbol(unitBuilder, unitSymbol);
    daqUnknown_releaseRef(unitSymbol);

    daqString* unitQuantity = nullptr;
    daqString_createString(&unitQuantity, "time");
    daqUnitBuilder_setQuantity(unitBuilder, unitQuantity);
    daqUnknown_releaseRef(unitQuantity);

    daqUnitBuilder_setId(unitBuilder, -1);

    daqUnit* unit = nullptr;
    daqUnitBuilder_build(unitBuilder, &unit);
    daqUnknown_releaseRef(unitBuilder);

    daqDataDescriptorBuilder_setUnit(builder, unit);
    daqUnknown_releaseRef(unit);

    daqString* name = nullptr;
    daqString_createString(&name, "time");
    daqDataDescriptorBuilder_setName(builder, name);
    daqUnknown_releaseRef(name);

    daqRatio* ratio = nullptr;
    daqRatio_createRatio(&ratio, 1, 1000);
    daqDataDescriptorBuilder_setTickResolution(builder, ratio);
    daqUnknown_releaseRef(ratio);

    daqInteger* delta = nullptr;
    daqInteger_createInteger(&delta, 1);
    daqInteger* start = nullptr;
    daqInteger_createInteger(&start, 0);

    daqNumber* deltaNum = nullptr;
    daqUnknown_queryInterface(delta, DAQ_NUMBER_INTF_ID, (void**) &deltaNum);
    daqNumber* startNum = nullptr;
    daqUnknown_queryInterface(start, DAQ_NUMBER_INTF_ID, (void**) &startNum);

    daqUnknown_releaseRef(delta);
    daqUnknown_releaseRef(start);

    daqDataRule* rule = nullptr;
    daqDataRule_createLinearDataRule(&rule, deltaNum, startNum);
    daqUnknown_releaseRef(deltaNum);
    daqUnknown_releaseRef(startNum);

    daqDataDescriptorBuilder_setRule(builder, rule);
    daqUnknown_releaseRef(rule);

    daqString* origin = nullptr;
    daqString_createString(&origin, "2025-01-01T00:00:00Z");
    daqDataDescriptorBuilder_setOrigin(builder, origin);
    daqUnknown_releaseRef(origin);

    daqDataDescriptorBuilder_build(builder, &descriptor);
    daqUnknown_releaseRef(builder);

    return descriptor;
}

TEST_F(COpendaqSignalTest, Allocator)
{
    daqAllocator* allocator = nullptr;
    daqAllocator_createMallocAllocator(&allocator);
    ASSERT_NE(allocator, nullptr);

    daqDataDescriptor* valueDescriptor = createValueDescriptor();

    void* address = nullptr;
    daqErrCode err = daqAllocator_allocate(allocator, valueDescriptor, 32, 4, &address);

    ASSERT_EQ(err, 0u);
    ASSERT_NE(address, nullptr);

    err = daqAllocator_free(allocator, address);
    ASSERT_EQ(err, 0u);

    daqUnknown_releaseRef(allocator);
    daqUnknown_releaseRef(valueDescriptor);
}

TEST_F(COpendaqSignalTest, DataDescriptor)
{
    daqDataDescriptor* valueDescriptor = createValueDescriptor();

    daqString* name = nullptr;
    daqDataDescriptor_getName(valueDescriptor, &name);
    daqCConstCharPtr nameStr = nullptr;
    daqString_getCharPtr(name, &nameStr);
    ASSERT_STREQ(nameStr, "vals");
    daqUnknown_releaseRef(name);

    daqUnit* unit = nullptr;
    daqDataDescriptor_getUnit(valueDescriptor, &unit);
    daqString* symbol = nullptr;
    daqUnit_getSymbol(unit, &symbol);
    daqCConstCharPtr symbolStr = nullptr;
    daqString_getCharPtr(symbol, &symbolStr);
    ASSERT_STREQ(symbolStr, "V");
    daqUnknown_releaseRef(symbol);
    daqUnknown_releaseRef(unit);

    daqSampleType sampleType = daqSampleType::daqSampleTypeNull;
    daqDataDescriptor_getSampleType(valueDescriptor, &sampleType);
    ASSERT_EQ(sampleType, daqSampleType::daqSampleTypeInt64);

    daqUnknown_releaseRef(valueDescriptor);
}

TEST_F(COpendaqSignalTest, DataPacket)
{
    daqDataDescriptor* valueDescriptor = createValueDescriptor();
    daqDataPacket* packet = nullptr;

    daqInteger* offset = nullptr;
    daqInteger_createInteger(&offset, 0);
    daqNumber* offsetNum = nullptr;
    daqUnknown_queryInterface(offset, DAQ_NUMBER_INTF_ID, (void**) &offsetNum);
    daqUnknown_releaseRef(offset);

    daqCSizeT sampleCount = 10u;
    daqDataPacket_createDataPacket(&packet, valueDescriptor, sampleCount, offsetNum);
    daqUnknown_releaseRef(offsetNum);

    void* data = nullptr;
    daqDataPacket_getRawData(packet, &data);
    ASSERT_NE(data, nullptr);

    daqUnknown_releaseRef(packet);
    daqUnknown_releaseRef(valueDescriptor);
}

TEST_F(COpendaqSignalTest, DimensionRule)
{
    daqDimensionRuleBuilder* builder = nullptr;
    daqDimensionRuleBuilder_createDimensionRuleBuilder(&builder);
    daqDimensionRuleBuilder_setType(builder, daqDimensionRuleType::daqDimensionRuleTypeLinear);
    daqInteger* delta = nullptr;
    daqInteger_createInteger(&delta, 1);
    daqInteger* start = nullptr;
    daqInteger_createInteger(&start, 0);
    daqInteger* size = nullptr;
    daqInteger_createInteger(&size, 10);
    daqNumber* deltaNum = nullptr;
    daqUnknown_queryInterface(delta, DAQ_NUMBER_INTF_ID, (void**) &deltaNum);
    daqNumber* startNum = nullptr;
    daqUnknown_queryInterface(start, DAQ_NUMBER_INTF_ID, (void**) &startNum);
    daqNumber* sizeNum = nullptr;
    daqUnknown_queryInterface(size, DAQ_NUMBER_INTF_ID, (void**) &sizeNum);
    daqUnknown_releaseRef(delta);
    daqUnknown_releaseRef(start);
    daqUnknown_releaseRef(size);

    daqString* deltaStr = nullptr;
    daqString_createString(&deltaStr, "delta");
    daqString* startStr = nullptr;
    daqString_createString(&startStr, "start");
    daqString* sizeStr = nullptr;
    daqString_createString(&sizeStr, "size");
    daqDimensionRuleBuilder_addParameter(builder, sizeStr, sizeNum);
    daqDimensionRuleBuilder_addParameter(builder, deltaStr, deltaNum);
    daqDimensionRuleBuilder_addParameter(builder, startStr, startNum);
    daqUnknown_releaseRef(deltaNum);
    daqUnknown_releaseRef(startNum);
    daqUnknown_releaseRef(sizeNum);

    daqDimensionRule* rule = nullptr;
    daqDimensionRuleBuilder_build(builder, &rule);
    daqUnknown_releaseRef(builder);

    ASSERT_NE(rule, nullptr);

    daqDimensionRuleType ruleType = daqDimensionRuleType::daqDimensionRuleTypeOther;
    daqDimensionRule_getType(rule, &ruleType);

    ASSERT_EQ(ruleType, daqDimensionRuleType::daqDimensionRuleTypeLinear);

    daqDict* params = nullptr;
    daqDimensionRule_getParameters(rule, &params);
    ASSERT_NE(params, nullptr);

    daqNumber* outDelta = nullptr;
    daqDict_get(params, deltaStr, (daqBaseObject**) &outDelta);
    daqNumber* outStart = nullptr;
    daqDict_get(params, startStr, (daqBaseObject**) &outStart);
    daqNumber* outSize = nullptr;
    daqDict_get(params, sizeStr, (daqBaseObject**) &outSize);

    daqCInt deltaInt = -1;
    daqCInt startInt = -1;
    daqCInt sizeInt = -1;
    daqNumber_getIntValue(outDelta, &deltaInt);
    daqNumber_getIntValue(outStart, &startInt);
    daqNumber_getIntValue(outSize, &sizeInt);
    ASSERT_EQ(deltaInt, 1);
    ASSERT_EQ(startInt, 0);
    ASSERT_EQ(sizeInt, 10);

    daqUnknown_releaseRef(outSize);
    daqUnknown_releaseRef(outDelta);
    daqUnknown_releaseRef(outStart);
    daqUnknown_releaseRef(params);
    daqUnknown_releaseRef(rule);
    daqUnknown_releaseRef(deltaStr);
    daqUnknown_releaseRef(startStr);
    daqUnknown_releaseRef(sizeStr);
}

TEST_F(COpendaqSignalTest, EventPacket)
{
    daqEventPacket* packet = nullptr;
    daqDataDescriptor* valueDescriptor = createValueDescriptor();
    daqDataDescriptor* domainDescriptor = createDomainDescriptor();
    daqEventPacket_createDataDescriptorChangedEventPacket(&packet, valueDescriptor, domainDescriptor);

    ASSERT_NE(packet, nullptr);
    daqString* id = nullptr;
    daqEventPacket_getEventId(packet, &id);
    daqCConstCharPtr idStr = nullptr;
    daqString_getCharPtr(id, &idStr);
    ASSERT_STREQ(idStr, "DATA_DESCRIPTOR_CHANGED");

    daqUnknown_releaseRef(id);
    daqUnknown_releaseRef(packet);
    daqUnknown_releaseRef(valueDescriptor);
    daqUnknown_releaseRef(domainDescriptor);
}

TEST_F(COpendaqSignalTest, InputPort)
{
    daqInputPortConfig* inputPortConfig = nullptr;
    daqContext* ctx = createContext();
    daqString* id = nullptr;
    daqString_createString(&id, "daqInputPort");
    daqInputPortConfig_createInputPort(&inputPortConfig, ctx, nullptr, id, daqFalse);
    ASSERT_NE(inputPortConfig, nullptr);

    daqUnknown_releaseRef(id);
    daqUnknown_releaseRef(ctx);
    daqUnknown_releaseRef(inputPortConfig);
}

TEST_F(COpendaqSignalTest, Range)
{
    daqRange* range = nullptr;
    daqInteger* lowValue = nullptr;
    daqInteger_createInteger(&lowValue, 0);
    daqInteger* highValue = nullptr;
    daqInteger_createInteger(&highValue, 10);
    daqNumber* lowValueNum = nullptr;
    daqUnknown_queryInterface(lowValue, DAQ_NUMBER_INTF_ID, (void**) &lowValueNum);
    daqUnknown_releaseRef(lowValue);
    daqNumber* highValueNum = nullptr;
    daqUnknown_queryInterface(highValue, DAQ_NUMBER_INTF_ID, (void**) &highValueNum);
    daqUnknown_releaseRef(highValue);
    daqRange_createRange(&range, lowValueNum, highValueNum);
    daqUnknown_releaseRef(lowValueNum);
    daqUnknown_releaseRef(highValueNum);

    ASSERT_NE(range, nullptr);

    daqNumber* outLowValue = nullptr;
    daqNumber* outHighValue = nullptr;
    daqRange_getLowValue(range, &outLowValue);
    daqRange_getHighValue(range, &outHighValue);
    daqCInt lowValueInt = -1;
    daqCInt highValueInt = -1;
    daqNumber_getIntValue(outLowValue, &lowValueInt);
    daqNumber_getIntValue(outHighValue, &highValueInt);
    ASSERT_EQ(lowValueInt, 0);
    ASSERT_EQ(highValueInt, 10);

    daqUnknown_releaseRef(outHighValue);
    daqUnknown_releaseRef(outLowValue);
    daqUnknown_releaseRef(range);
}

TEST_F(COpendaqSignalTest, Scaling)
{
    daqScalingBuilder* builder = nullptr;
    daqScalingBuilder_createScalingBuilder(&builder);
    daqScalingBuilder_setInputDataType(builder, daqSampleType::daqSampleTypeInt16);
    daqScalingBuilder_setOutputDataType(builder, daqScaledSampleType::daqScaledSampleTypeFloat32);
    daqScalingBuilder_setScalingType(builder, daqScalingType::daqScalingTypeLinear);

    daqDict* params = nullptr;
    daqDict_createDict(&params);

    daqString* scaleStr = nullptr;
    daqString_createString(&scaleStr, "scale");
    daqString* offsetStr = nullptr;
    daqString_createString(&offsetStr, "offset");

    daqInteger* scale = nullptr;
    daqInteger_createInteger(&scale, 10);
    daqInteger* offset = nullptr;
    daqInteger_createInteger(&offset, 10);

    daqDict_set(params, scaleStr, scale);
    daqDict_set(params, offsetStr, offset);
    daqScalingBuilder_setParameters(builder, params);
    daqUnknown_releaseRef(scale);
    daqUnknown_releaseRef(offset);
    daqUnknown_releaseRef(scaleStr);
    daqUnknown_releaseRef(offsetStr);

    daqScaling* scaling = nullptr;
    daqScalingBuilder_build(builder, &scaling);
    daqUnknown_releaseRef(builder);

    ASSERT_NE(scaling, nullptr);

    daqScalingType scalingType = daqScalingType::daqScalingTypeOther;
    daqScaling_getType(scaling, &scalingType);
    ASSERT_EQ(scalingType, daqScalingType::daqScalingTypeLinear);
    daqSampleType inputSampleType = daqSampleType::daqSampleTypeNull;
    daqScaling_getInputSampleType(scaling, &inputSampleType);
    ASSERT_EQ(inputSampleType, daqSampleType::daqSampleTypeInt16);
    daqScaledSampleType outputSampleType = daqScaledSampleType::daqScaledSampleTypeInvalid;
    daqScaling_getOutputSampleType(scaling, &outputSampleType);
    ASSERT_EQ(outputSampleType, daqScaledSampleType::daqScaledSampleTypeFloat32);
    daqDict* scalingParams = nullptr;
    daqScaling_getParameters(scaling, &scalingParams);
    ASSERT_NE(scalingParams, nullptr);

    daqCBool equal = daqFalse;
    daqBaseObject_equals(scalingParams, params, &equal);
    ASSERT_EQ(equal, daqTrue);

    daqUnknown_releaseRef(scalingParams);
    daqUnknown_releaseRef(scaling);
    daqUnknown_releaseRef(params);
}

TEST_F(COpendaqSignalTest, Signal)
{
    daqSignalConfig* signalConfig = nullptr;
    daqString* id = nullptr;
    daqString_createString(&id, "sig");
    daqContext* ctx = createContext();
    daqSignalConfig_createSignal(&signalConfig, ctx, nullptr, id, nullptr);
    ASSERT_NE(signalConfig, nullptr);
    daqUnknown_releaseRef(id);
    daqUnknown_releaseRef(ctx);
    daqUnknown_releaseRef(signalConfig);
}
