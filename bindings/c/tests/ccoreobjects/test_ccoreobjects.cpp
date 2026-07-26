#include <copendaq.h>
#include <coretypes/errorinfo.h>

#include <gtest/gtest.h>

using CCoreobjectsTest = testing::Test;

TEST_F(CCoreobjectsTest, ArgumentInfo)
{
    daqArgumentInfo* argInfo = nullptr;
    daqString* name = nullptr;
    daqString_createString(&name, "test_argument");
    daqErrCode err = 0;
    err = daqArgumentInfo_createArgumentInfo(&argInfo, name, daqCCoreType::daqCtInt);

    daqString* name2 = nullptr;
    daqCCoreType type = daqCCoreType::daqCtUndefined;
    err = daqArgumentInfo_getName(argInfo, &name2);
    ASSERT_EQ(err, 0u);
    err = daqArgumentInfo_getType(argInfo, &type);
    ASSERT_EQ(err, 0u);
    daqCConstCharPtr str = nullptr;
    daqString_getCharPtr(name2, &str);
    ASSERT_STREQ(str, "test_argument");
    ASSERT_EQ(type, daqCCoreType::daqCtInt);

    daqUnknown_releaseRef(name2);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(argInfo);
}

TEST_F(CCoreobjectsTest, AuthenticationProvider)
{
    daqAuthenticationProvider* authProvider = nullptr;
    daqErrCode err = 0;
    daqUser* user = nullptr;
    daqString* username = nullptr;
    daqString* passwordHash = nullptr;
    daqList* groups = nullptr;
    daqString_createString(&username, "test_user");
    daqString_createString(&passwordHash, "test_hash");
    daqList_createList(&groups);
    daqUser_createUser(&user, username, passwordHash, groups);

    daqList* userList = nullptr;
    daqList_createList(&userList);
    daqList_pushBack(userList, user);
    err = daqAuthenticationProvider_createStaticAuthenticationProvider(&authProvider, daqTrue, userList);
    ASSERT_EQ(err, 0u);

    daqUser* userOut = nullptr;
    err = daqAuthenticationProvider_authenticateAnonymous(authProvider, &userOut);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(userOut, nullptr);
    daqUnknown_releaseRef(userOut);
    userOut = nullptr;

    err = daqAuthenticationProvider_authenticate(authProvider, username, passwordHash, &userOut);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(userOut, nullptr);
    daqUnknown_releaseRef(userOut);
    userOut = nullptr;

    err = daqAuthenticationProvider_findUser(authProvider, username, &userOut);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(userOut, nullptr);
    daqUnknown_releaseRef(userOut);

    daqUnknown_releaseRef(userList);
    daqUnknown_releaseRef(groups);
    daqUnknown_releaseRef(user);
    daqUnknown_releaseRef(passwordHash);
    daqUnknown_releaseRef(username);
    daqUnknown_releaseRef(authProvider);
}

TEST_F(CCoreobjectsTest, CallableInfo)
{
    daqCallableInfo* callableInfo = nullptr;
    daqErrCode err = 0;

    daqList* argumentInfo = nullptr;
    daqList_createList(&argumentInfo);

    daqString* name = nullptr;
    daqString_createString(&name, "test_argument");
    daqArgumentInfo* argInfo = nullptr;
    daqArgumentInfo_createArgumentInfo(&argInfo, name, daqCCoreType::daqCtInt);
    daqList_pushBack(argumentInfo, argInfo);

    err = daqCallableInfo_createCallableInfo(&callableInfo, argumentInfo, daqCCoreType::daqCtInt, daqTrue);
    ASSERT_EQ(err, 0u);

    daqCBool isConst = daqFalse;
    daqCCoreType returnType = daqCCoreType::daqCtUndefined;
    daqList* arguments = nullptr;

    daqCallableInfo_isConst(callableInfo, &isConst);
    daqCallableInfo_getReturnType(callableInfo, &returnType);
    daqCallableInfo_getArguments(callableInfo, &arguments);

    ASSERT_EQ(isConst, daqTrue);
    ASSERT_EQ(returnType, daqCCoreType::daqCtInt);
    ASSERT_NE(arguments, nullptr);
    daqCSizeT size = 0;
    daqList_getCount(arguments, &size);
    ASSERT_EQ(size, 1u);

    daqUnknown_releaseRef(arguments);
    daqUnknown_releaseRef(argInfo);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(argumentInfo);
    daqUnknown_releaseRef(callableInfo);
}

TEST_F(CCoreobjectsTest, Coercer)
{
    daqCoercer* coercer = nullptr;
    daqErrCode err = 0;
    daqString* evalStr = nullptr;
    daqString_createString(&evalStr, "value + 2");
    err = daqCoercer_createCoercer(&coercer, evalStr);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(coercer, nullptr);
    daqInteger* value = nullptr;
    daqInteger_createInteger(&value, 10);
    daqInteger* coercedValue = nullptr;
    daqCoercer_coerce(coercer, nullptr, value, (daqBaseObject**) &coercedValue);
    ASSERT_NE(coercedValue, nullptr);
    daqCInt coercedInt = 0;
    daqInteger_getValue(coercedValue, &coercedInt);
    ASSERT_EQ(coercedInt, 12);

    daqUnknown_releaseRef(coercedValue);
    daqUnknown_releaseRef(value);
    daqUnknown_releaseRef(evalStr);
    daqUnknown_releaseRef(coercer);
}

TEST_F(CCoreobjectsTest, CoreEventArgs)
{
}

static daqCBool eventCalled = daqFalse;
static void onPropertyObjectUpdateEnd(daqBaseObject* sender, daqBaseObject* args)
{
    daqEndUpdateEventArgs* eventArgs = (daqEndUpdateEventArgs*) args;
    daqList* properties = nullptr;
    daqEndUpdateEventArgs_getProperties(eventArgs, &properties);
    daqCSizeT count = 0;
    daqList_getCount(properties, &count);
    if (count == 0u)
        eventCalled = daqTrue;

    daqUnknown_releaseRef(properties);
    daqUnknown_releaseRef(sender);
    daqUnknown_releaseRef(args);
}

TEST_F(CCoreobjectsTest, EndUpdateEventArgs)
{
    daqPropertyObject* propObj = nullptr;
    daqPropertyObject_createPropertyObject(&propObj);

    daqEvent* event = nullptr;
    daqPropertyObject_getOnEndUpdate(propObj, &event);

    daqEventHandler* handler = nullptr;
    daqEventHandler_createEventHandler(&handler, onPropertyObjectUpdateEnd);

    daqEvent_addHandler(event, handler);

    daqPropertyObject_beginUpdate(propObj);
    daqPropertyObject_endUpdate(propObj);
    ASSERT_EQ(eventCalled, daqTrue);

    daqUnknown_releaseRef(handler);
    daqUnknown_releaseRef(event);
    daqUnknown_releaseRef(propObj);
}

TEST_F(CCoreobjectsTest, EvalValue)
{
    daqPropertyObject* propObj = nullptr;
    daqPropertyObject_createPropertyObject(&propObj);

    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);
    daqProperty* prop = nullptr;
    daqProperty_createIntProperty(&prop, name, defaultValue, visible);

    daqPropertyObject_addProperty(propObj, prop);

    daqString* refName = nullptr;
    daqString_createString(&refName, "ref_property");
    daqString* evalStr = nullptr;
    daqString_createString(&evalStr, "%test_property");
    daqEvalValue* evalValue = nullptr;
    daqEvalValue_createEvalValue(&evalValue, evalStr);
    daqProperty* refProp = nullptr;
    daqProperty_createReferenceProperty(&refProp, refName, evalValue);

    daqPropertyObject_addProperty(propObj, refProp);

    daqInteger* value = nullptr;
    daqPropertyObject_getPropertyValue(propObj, refName, (daqBaseObject**) &value);
    ASSERT_NE(value, nullptr);
    daqCInt intValue = 0;
    daqInteger_getValue(value, &intValue);
    ASSERT_EQ(intValue, 10);

    daqUnknown_releaseRef(value);
    daqUnknown_releaseRef(refName);
    daqUnknown_releaseRef(evalStr);
    daqUnknown_releaseRef(evalValue);
    daqUnknown_releaseRef(refProp);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(prop);
    daqUnknown_releaseRef(propObj);
}

TEST_F(CCoreobjectsTest, Ownable)
{
    daqErrCode err = 0;
    daqPropertyObject* propObj = nullptr;
    daqPropertyObject_createPropertyObject(&propObj);
    daqPropertyObject* parentObj = nullptr;
    daqPropertyObject_createPropertyObject(&parentObj);

    daqOwnable* ownable = nullptr;
    daqBaseObject_borrowInterface(propObj, DAQ_OWNABLE_INTF_ID, (daqBaseObject**) &ownable);

    err = daqOwnable_setOwner(ownable, parentObj);
    ASSERT_EQ(err, 0u);
    err = daqOwnable_setOwner(ownable, nullptr);
    ASSERT_EQ(err, 0u);

    daqUnknown_releaseRef(ownable);
    daqUnknown_releaseRef(parentObj);
}

TEST_F(CCoreobjectsTest, Permissions)
{
    daqList* adminGroups = nullptr;
    daqList_createList(&adminGroups);
    daqList* guestGroups = nullptr;
    daqList_createList(&guestGroups);

    daqString* adminName = nullptr;
    daqString_createString(&adminName, "admin");
    daqString* guestName = nullptr;
    daqString_createString(&guestName, "guest");
    daqString* password = nullptr;
    daqString_createString(&password, "password");

    daqList_pushBack(adminGroups, adminName);
    daqList_pushBack(adminGroups, guestName);
    daqList_pushBack(guestGroups, guestName);

    daqUser* admin = nullptr;
    daqUser_createUser(&admin, adminName, password, adminGroups);
    daqUser* guest = nullptr;
    daqUser_createUser(&guest, guestName, password, guestGroups);

    daqPermissionManager* manager = nullptr;
    daqPermissionManager_createPermissionManager(&manager, nullptr);

    daqPermissionMaskBuilder* maskBuilder = nullptr;
    daqPermissionMaskBuilder_createPermissionMaskBuilder(&maskBuilder);
    daqPermissionMaskBuilder_read(maskBuilder);
    daqPermissionMaskBuilder_write(maskBuilder);

    daqPermissionsBuilder* permissionsBuilder = nullptr;
    daqPermissionsBuilder_createPermissionsBuilder(&permissionsBuilder);
    daqPermissionsBuilder_assign(permissionsBuilder, adminName, maskBuilder);
    daqPermissions* adminPermissions = nullptr;
    daqPermissionsBuilder_build(permissionsBuilder, &adminPermissions);

    daqPermissionManager_setPermissions(manager, adminPermissions);
    daqCBool isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, admin, daqPermission::daqPermissionRead, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqTrue);
    isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, admin, daqPermission::daqPermissionWrite, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqTrue);
    isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, admin, daqPermission::daqPermissionExecute, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqFalse);

    isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, guest, daqPermission::daqPermissionRead, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqFalse);
    isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, guest, daqPermission::daqPermissionWrite, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqFalse);
    isAuthorized = daqFalse;
    daqPermissionManager_isAuthorized(manager, guest, daqPermission::daqPermissionExecute, &isAuthorized);
    ASSERT_EQ(isAuthorized, daqFalse);

    daqUnknown_releaseRef(manager);
    daqUnknown_releaseRef(adminPermissions);
    daqUnknown_releaseRef(permissionsBuilder);
    daqUnknown_releaseRef(maskBuilder);
    daqUnknown_releaseRef(admin);
    daqUnknown_releaseRef(guest);
    daqUnknown_releaseRef(adminName);
    daqUnknown_releaseRef(guestName);
    daqUnknown_releaseRef(password);
    daqUnknown_releaseRef(adminGroups);
    daqUnknown_releaseRef(guestGroups);
}

TEST_F(CCoreobjectsTest, Property)
{
    daqProperty* prop = nullptr;
    daqErrCode err = 0;
    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);
    err = daqProperty_createIntProperty(&prop, name, defaultValue, visible);
    ASSERT_EQ(err, 0u);
    daqInteger* defaultValueOut = nullptr;
    daqProperty_getDefaultValue(prop, (daqBaseObject**) &defaultValueOut);
    ASSERT_NE(defaultValueOut, nullptr);
    daqCInt value = 0;
    err = daqInteger_getValue(defaultValueOut, &value);
    ASSERT_EQ(value, 10);

    daqString* nameOut = nullptr;
    daqProperty_getName(prop, &nameOut);
    daqCConstCharPtr str = nullptr;
    daqString_getCharPtr(nameOut, &str);
    ASSERT_STREQ(str, "test_property");

    daqCBool isVisible = daqFalse;
    daqProperty_getVisible(prop, &isVisible);
    ASSERT_EQ(isVisible, daqTrue);

    daqUnknown_releaseRef(nameOut);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValueOut);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(prop);
}

TEST_F(CCoreobjectsTest, PropertyBuilder)
{
    daqPropertyBuilder* propBuilder = nullptr;
    daqErrCode err = 0;

    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);
    err = daqPropertyBuilder_createIntPropertyBuilder(&propBuilder, name, defaultValue);
    ASSERT_EQ(err, 0u);
    err = daqPropertyBuilder_setVisible(propBuilder, visible);
    ASSERT_EQ(err, 0u);
    daqProperty* property = nullptr;
    err = daqPropertyBuilder_build(propBuilder, &property);
    ASSERT_EQ(err, 0u);

    daqInteger* defaultValueOut = nullptr;
    daqProperty_getDefaultValue(property, (daqBaseObject**) &defaultValueOut);
    ASSERT_NE(defaultValueOut, nullptr);
    daqCInt value = 0;
    err = daqInteger_getValue(defaultValueOut, &value);
    ASSERT_EQ(value, 10);

    daqString* nameOut = nullptr;
    daqProperty_getName(property, &nameOut);
    daqCConstCharPtr str = nullptr;
    daqString_getCharPtr(nameOut, &str);
    ASSERT_STREQ(str, "test_property");

    daqCBool isVisible = daqFalse;
    daqProperty_getVisible(property, &isVisible);
    ASSERT_EQ(isVisible, daqTrue);

    daqUnknown_releaseRef(nameOut);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValueOut);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(property);
    daqUnknown_releaseRef(propBuilder);
}

TEST_F(CCoreobjectsTest, PropertyObject)
{
    daqPropertyObject* propObj = nullptr;
    daqErrCode err = 0;

    daqPropertyObject_createPropertyObject(&propObj);
    ASSERT_NE(propObj, nullptr);

    daqProperty* prop = nullptr;
    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);
    err = daqProperty_createIntProperty(&prop, name, defaultValue, visible);
    ASSERT_EQ(err, 0u);
    err = daqPropertyObject_addProperty(propObj, prop);
    ASSERT_EQ(err, 0u);

    daqProperty* propOut = nullptr;
    err = daqPropertyObject_getProperty(propObj, name, &propOut);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(propOut, nullptr);

    daqCBool equal = daqFalse;
    err = daqBaseObject_equals(prop, propOut, &equal);
    ASSERT_EQ(err, 0u);
    ASSERT_EQ(equal, daqTrue);

    err = daqPropertyObject_hasProperty(propObj, name, &equal);
    ASSERT_EQ(err, 0u);
    ASSERT_EQ(equal, daqTrue);

    err = daqPropertyObject_removeProperty(propObj, name);
    ASSERT_EQ(err, 0u);

    err = daqPropertyObject_hasProperty(propObj, name, &equal);
    ASSERT_EQ(err, 0u);
    ASSERT_EQ(equal, daqFalse);

    daqUnknown_releaseRef(propOut);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(prop);
    daqUnknown_releaseRef(propObj);
}

TEST_F(CCoreobjectsTest, PropertyObjectClass)
{
    daqPropertyObjectClass* propObjClass = nullptr;
    daqPropertyObjectClassBuilder* builder = nullptr;
    daqErrCode err = 0;
    daqString* name = nullptr;
    daqString_createString(&name, "test_property_class");

    err = daqPropertyObjectClassBuilder_createPropertyObjectClassBuilder(&builder, name);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(builder, nullptr);

    daqProperty* prop = nullptr;
    daqString* propName = nullptr;
    daqString_createString(&propName, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);
    err = daqProperty_createIntProperty(&prop, propName, defaultValue, visible);
    ASSERT_EQ(err, 0u);

    err = daqPropertyObjectClassBuilder_addProperty(builder, prop);
    ASSERT_EQ(err, 0u);

    err = daqPropertyObjectClassBuilder_build(builder, &propObjClass);
    ASSERT_EQ(err, 0u);

    daqProperty* propOut = nullptr;
    err = daqPropertyObjectClass_getProperty(propObjClass, propName, &propOut);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(propOut, nullptr);

    daqCBool equal = daqFalse;
    err = daqBaseObject_equals(prop, propOut, &equal);
    ASSERT_EQ(err, 0u);
    ASSERT_EQ(equal, daqTrue);

    daqUnknown_releaseRef(propOut);
    daqUnknown_releaseRef(propName);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(prop);
    daqUnknown_releaseRef(propObjClass);
    daqUnknown_releaseRef(builder);
    daqUnknown_releaseRef(name);
}

TEST_F(CCoreobjectsTest, PropertyObjectProtected)
{
    daqErrCode err = 0u;
    daqPropertyObject* propObj = nullptr;
    daqPropertyObjectProtected* propObjProtected = nullptr;
    daqPropertyObject_createPropertyObject(&propObj);
    daqBaseObject_borrowInterface(propObj, DAQ_PROPERTY_OBJECT_PROTECTED_INTF_ID, (daqBaseObject**) &propObjProtected);

    daqPropertyBuilder* propBuilder = nullptr;
    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* readOnly = nullptr;
    daqBoolean_createBoolean(&readOnly, daqTrue);
    daqPropertyBuilder_createIntPropertyBuilder(&propBuilder, name, defaultValue);
    daqPropertyBuilder_setReadOnly(propBuilder, readOnly);
    daqProperty* prop = nullptr;
    daqPropertyBuilder_build(propBuilder, &prop);
    daqPropertyObject_addProperty(propObj, prop);
    daqInteger* value = nullptr;
    daqInteger_createInteger(&value, 20);

    err = daqPropertyObject_setPropertyValue(propObj, name, value);
    ASSERT_NE(err, 0u);
    daqClearErrorInfo();

    err = daqPropertyObjectProtected_setProtectedPropertyValue(propObjProtected, name, value);
    ASSERT_EQ(err, 0u);
    daqInteger* valueOut = nullptr;
    daqPropertyObject_getPropertyValue(propObj, name, (daqBaseObject**) &valueOut);
    ASSERT_NE(valueOut, nullptr);
    daqCInt intValue = 0;
    daqInteger_getValue(valueOut, &intValue);
    ASSERT_EQ(intValue, 20);

    daqUnknown_releaseRef(valueOut);
    daqUnknown_releaseRef(value);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(readOnly);
    daqUnknown_releaseRef(prop);
    daqUnknown_releaseRef(propBuilder);
    daqUnknown_releaseRef(propObjProtected);
}

TEST_F(CCoreobjectsTest, PropertyValueEventArgs)
{
    daqPropertyValueEventArgs* eventArgs = nullptr;
    daqErrCode err = 0u;

    daqProperty* prop = nullptr;
    daqString* name = nullptr;
    daqString_createString(&name, "test_property");
    daqInteger* defaultValue = nullptr;
    daqInteger_createInteger(&defaultValue, 10);
    daqBoolean* visible = nullptr;
    daqBoolean_createBoolean(&visible, daqTrue);

    err = daqProperty_createIntProperty(&prop, name, defaultValue, visible);
    ASSERT_EQ(err, 0u);

    daqInteger* value1 = nullptr;
    daqInteger* value2 = nullptr;

    daqInteger_createInteger(&value1, 20);
    daqInteger_createInteger(&value2, 30);

    err = daqPropertyValueEventArgs_createPropertyValueEventArgs(
        &eventArgs, prop, value2, value1, daqPropertyEventType::daqPropertyEventTypeEventTypeUpdate, daqFalse);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(eventArgs, nullptr);

    daqInteger* valueOut = nullptr;
    daqPropertyValueEventArgs_getValue(eventArgs, (daqBaseObject**) &valueOut);
    ASSERT_NE(valueOut, nullptr);
    daqCBool equal = daqFalse;
    err = daqBaseObject_equals(valueOut, value2, &equal);
    ASSERT_EQ(equal, daqTrue);
    daqUnknown_releaseRef(valueOut);

    daqPropertyValueEventArgs_getOldValue(eventArgs, (daqBaseObject**) &valueOut);
    ASSERT_NE(valueOut, nullptr);
    err = daqBaseObject_equals(valueOut, value1, &equal);
    ASSERT_EQ(equal, daqTrue);

    daqUnknown_releaseRef(valueOut);
    daqUnknown_releaseRef(value1);
    daqUnknown_releaseRef(value2);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(defaultValue);
    daqUnknown_releaseRef(visible);
    daqUnknown_releaseRef(prop);
    daqUnknown_releaseRef(eventArgs);
}

TEST_F(CCoreobjectsTest, Unit)
{
    daqUnitBuilder* unitBuilder = nullptr;
    daqErrCode err = 0u;
    daqString* name = nullptr;
    daqString_createString(&name, "test_unit");
    daqString* symbol = nullptr;
    daqString_createString(&symbol, "tu");

    err = daqUnitBuilder_createUnitBuilder(&unitBuilder);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(unitBuilder, nullptr);

    daqUnitBuilder_setName(unitBuilder, name);
    daqUnitBuilder_setSymbol(unitBuilder, symbol);

    daqUnit* unit = nullptr;
    err = daqUnitBuilder_build(unitBuilder, &unit);
    ASSERT_EQ(err, 0u);
    ASSERT_NE(unit, nullptr);

    daqString* nameOut = nullptr;
    daqString* symbolOut = nullptr;
    daqUnit_getName(unit, &nameOut);
    daqUnit_getSymbol(unit, &symbolOut);
    daqCConstCharPtr nameStr = nullptr;
    daqCConstCharPtr symbolStr = nullptr;
    daqString_getCharPtr(nameOut, &nameStr);
    daqString_getCharPtr(symbolOut, &symbolStr);
    ASSERT_STREQ(nameStr, "test_unit");
    ASSERT_STREQ(symbolStr, "tu");

    daqUnknown_releaseRef(nameOut);
    daqUnknown_releaseRef(symbolOut);
    daqUnknown_releaseRef(name);
    daqUnknown_releaseRef(symbol);
    daqUnknown_releaseRef(unit);
    daqUnknown_releaseRef(unitBuilder);
}

TEST_F(CCoreobjectsTest, User)
{
    daqString* username = nullptr;
    daqString_createString(&username, "test_user");
    daqString* passwordHash = nullptr;
    daqString_createString(&passwordHash, "test_hash");
    daqList* groups = nullptr;
    daqList_createList(&groups);
    daqUser* user = nullptr;
    daqUser_createUser(&user, username, passwordHash, groups);

    daqString* usernameOut = nullptr;
    daqUser_getUsername(user, &usernameOut);

    daqCConstCharPtr str = nullptr;
    daqString_getCharPtr(usernameOut, &str);
    ASSERT_STREQ(str, "test_user");

    daqUnknown_releaseRef(usernameOut);
    daqUnknown_releaseRef(user);
    daqUnknown_releaseRef(username);
    daqUnknown_releaseRef(passwordHash);
    daqUnknown_releaseRef(groups);
}

TEST_F(CCoreobjectsTest, Validator)
{
    daqValidator* validator = nullptr;
    daqErrCode err = 0;
    daqString* evalStr = nullptr;
    daqString_createString(&evalStr, "value > 5");
    err = daqValidator_createValidator(&validator, evalStr);
    ASSERT_EQ(err, 0u);

    daqInteger* value = nullptr;
    daqInteger_createInteger(&value, 10);
    err = daqValidator_validate(validator, nullptr, value);
    ASSERT_EQ(err, 0u);
    daqInteger* invalidValue = nullptr;
    daqInteger_createInteger(&invalidValue, 3);
    err = daqValidator_validate(validator, nullptr, invalidValue);
    ASSERT_NE(err, 0u);
    daqClearErrorInfo();

    daqUnknown_releaseRef(value);
    daqUnknown_releaseRef(invalidValue);
    daqUnknown_releaseRef(evalStr);
    daqUnknown_releaseRef(validator);
}
