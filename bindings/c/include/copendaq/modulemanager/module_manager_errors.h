/*
 * Copyright 2022-2025 openDAQ d.o.o.
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
#include <ccoretypes/errors.h>

/*!
 * @ingroup opendaq_errors_group
 * @addtogroup opendaq_errors_macros Error Code Macros
 * @{
 */

#define DAQ_ERRTYPE_MODULE_MANAGER 0x03u

#define DAQ_ERR_MODULE_MANAGER_UNKNOWN           DAQ_ERROR_CODE(DAQ_ERRTYPE_MODULE_MANAGER, 0x0000u)
#define DAQ_ERR_MODULE_LOAD_FAILED               DAQ_ERROR_CODE(DAQ_ERRTYPE_MODULE_MANAGER, 0x0001u)
#define DAQ_ERR_MODULE_NO_ENTRY_POINT            DAQ_ERROR_CODE(DAQ_ERRTYPE_MODULE_MANAGER, 0x0002u)
#define DAQ_ERR_MODULE_ENTRY_POINT_FAILED        DAQ_ERROR_CODE(DAQ_ERRTYPE_MODULE_MANAGER, 0x0003u)
#define DAQ_ERR_MODULE_INCOMPATIBLE_DEPENDENCIES DAQ_ERROR_CODE(DAQ_ERRTYPE_MODULE_MANAGER, 0x0004u)

/*!
 * @}
 */
