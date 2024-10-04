//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
//
//     RTGen (PythonGenerator).
// </auto-generated>
//------------------------------------------------------------------------------

/*
 * Copyright 2022-2024 openDAQ d.o.o.
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

#include <pybind11/gil.h>

#include "py_opendaq/py_opendaq.h"
#include "py_core_types/py_converter.h"


PyDaqIntf<daq::IReader, daq::IBaseObject> declareIReader(pybind11::module_ m)
{
    py::enum_<daq::ReadTimeoutType>(m, "ReadTimeoutType")
        .value("Any", daq::ReadTimeoutType::Any)
        .value("All", daq::ReadTimeoutType::All);

    return wrapInterface<daq::IReader, daq::IBaseObject>(m, "IReader");
}

void defineIReader(pybind11::module_ m, PyDaqIntf<daq::IReader, daq::IBaseObject> cls)
{
    cls.doc() = "A basic signal reader that simplifies accessing the signals's data stream.";

    cls.def_property_readonly("available_count",
        [](daq::IReader *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ReaderPtr::Borrow(object);
            return objectPtr.getAvailableCount();
        },
        "Gets the number of segments available to read");
    cls.def_property("on_data_available",
        nullptr,
        [](daq::IReader *object, daq::IProcedure* callback)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ReaderPtr::Borrow(object);
            objectPtr.setOnDataAvailable(callback);
        },
        "Sets the specified callback function to be called when there is available data in the reader. Pass @c nullptr to unset the callback. The callback should take no arguments.");
    cls.def_property_readonly("empty",
        [](daq::IReader *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ReaderPtr::Borrow(object);
            return objectPtr.getEmpty();
        },
        "Checks if there is data to read.");
}
