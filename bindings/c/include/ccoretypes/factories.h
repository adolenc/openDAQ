/*
 * Copyright 2022-2026 openDAQ d.o.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#pragma once

#ifdef __cplusplus
extern "C"
{
#endif

#include <ccommon.h>

    typedef struct daqSerializer daqSerializer;
    typedef struct daqEventHandler daqEventHandler;
    typedef struct daqFunction daqFunction;
    typedef struct daqProcedure daqProcedure;

    daqErrCode EXPORTED daqSerializer_createJsonSerializer(daqSerializer** obj, daqCBool pretty);
    daqErrCode EXPORTED daqSerializer_createJsonSerializerWithVersion(daqSerializer** obj, daqCInt version, daqCBool pretty);
    daqErrCode EXPORTED daqEventHandler_createEventHandler(daqEventHandler** obj, daqEventCall call);

    /*
     * Like daqFunction_createFunction, daqProcedure_createProcedure and
     * daqEventHandler_createEventHandler, but every call also gets userData. The object owns userData:
     * once the object is destroyed, or if creating it fails, freeUserData (unless NULL) is called
     * with it, exactly once. The arguments are passed as by the factories without user data.
     */
    daqErrCode EXPORTED daqFunction_createFunctionWithData(daqFunction** obj, daqFuncCallWithData call, void* userData, daqFreeUserData freeUserData);
    daqErrCode EXPORTED daqProcedure_createProcedureWithData(daqProcedure** obj, daqProcCallWithData call, void* userData, daqFreeUserData freeUserData);
    daqErrCode EXPORTED daqEventHandler_createEventHandlerWithData(daqEventHandler** obj, daqEventCallWithData call, void* userData, daqFreeUserData freeUserData);

#ifdef __cplusplus
}
#endif
