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


PyDaqIntf<daq::IStreamReaderBuilder, daq::IBaseObject> declareIStreamReaderBuilder(pybind11::module_ m)
{
    return wrapInterface<daq::IStreamReaderBuilder, daq::IBaseObject>(m, "IStreamReaderBuilder");
}

void defineIStreamReaderBuilder(pybind11::module_ m, PyDaqIntf<daq::IStreamReaderBuilder, daq::IBaseObject> cls)
{
    cls.doc() = "Builder component of Stream reader objects. Contains setter methods to configure the Stream reader parameters and a `build` method that builds the Unit object.";

    m.def("StreamReaderBuilder", &daq::StreamReaderBuilder_Create);

    cls.def("build",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.build().detach();
        },
        "Builds and returns a Stream reader object using the currently set values of the Builder.");
    cls.def_property("signal",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getSignal().detach();
        },
        [](daq::IStreamReaderBuilder *object, daq::ISignal* signal)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setSignal(signal);
        },
        py::return_value_policy::take_ownership,
        "Gets the signal / Sets the signal to stream reader");
    cls.def_property("input_port",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getInputPort().detach();
        },
        [](daq::IStreamReaderBuilder *object, daq::IInputPort* port)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setInputPort(port);
        },
        py::return_value_policy::take_ownership,
        "Gets the input port / Sets the input port to stream reader");
    cls.def_property("value_read_type",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getValueReadType();
        },
        [](daq::IStreamReaderBuilder *object, daq::SampleType type)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setValueReadType(type);
        },
        "Gets the value signal read type / Sets the value signal read type");
    cls.def_property("domain_read_type",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getDomainReadType();
        },
        [](daq::IStreamReaderBuilder *object, daq::SampleType type)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setDomainReadType(type);
        },
        "Gets the domain signal read type / Sets the domain signal read type");
    cls.def_property("read_mode",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getReadMode();
        },
        [](daq::IStreamReaderBuilder *object, daq::ReadMode mode)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setReadMode(mode);
        },
        "Gets the read mode (Unscaled, Scaled, RawValue) / Sets the read mode (Unscaled, Scaled, RawValue)");
    cls.def_property("read_timeout_type",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getReadTimeoutType();
        },
        [](daq::IStreamReaderBuilder *object, daq::ReadTimeoutType type)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setReadTimeoutType(type);
        },
        "Gets the read timeout mode / Sets the read timeout mode");
    cls.def_property("skip_events",
        [](daq::IStreamReaderBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            return objectPtr.getSkipEvents();
        },
        [](daq::IStreamReaderBuilder *object, const bool skipEvents)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::StreamReaderBuilderPtr::Borrow(object);
            objectPtr.setSkipEvents(skipEvents);
        },
        "Gets the skip events / Sets the skip events");
}
