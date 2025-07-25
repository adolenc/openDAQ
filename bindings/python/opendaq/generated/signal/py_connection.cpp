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

#include <pybind11/gil.h>

#include "py_opendaq/py_opendaq.h"
#include "py_core_types/py_converter.h"
#include "py_core_objects/py_variant_extractor.h"

PyDaqIntf<daq::IConnection, daq::IBaseObject> declareIConnection(pybind11::module_ m)
{
    return wrapInterface<daq::IConnection, daq::IBaseObject>(m, "IConnection");
}

void defineIConnection(pybind11::module_ m, PyDaqIntf<daq::IConnection, daq::IBaseObject> cls)
{
    cls.doc() = "Represents a connection between an Input port and Signal. Acts as a queue for packets sent by the signal, which can be read by the input port and the input port's owner.";

    m.def("Connection", &daq::Connection_Create);

    cls.def("enqueue",
        [](daq::IConnection *object, daq::IPacket* packet)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            objectPtr.enqueue(packet);
        },
        py::arg("packet"),
        "Places a packet at the back of the queue.");
    cls.def("enqueue_on_this_thread",
        [](daq::IConnection *object, daq::IPacket* packet)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            objectPtr.enqueueOnThisThread(packet);
        },
        py::arg("packet"),
        "Places a packet at the back of the queue.");
    cls.def("dequeue",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.dequeue().detach();
        },
        "Removes the packet at the front of the queue and returns it.");
    cls.def("peek",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.peek().detach();
        },
        "Returns the packet at the front of the queue without removing it.");
    cls.def_property_readonly("packet_count",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getPacketCount();
        },
        "Gets the number of queued packets.");
    cls.def_property_readonly("signal",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getSignal().detach();
        },
        py::return_value_policy::take_ownership,
        "Gets the Signal that is sending packets through the Connection.");
    cls.def_property_readonly("input_port",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getInputPort().detach();
        },
        py::return_value_policy::take_ownership,
        "Gets the Input port to which packets are being sent.");
    cls.def_property_readonly("available_samples",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getAvailableSamples();
        },
        "Gets the number of samples available in the queued packets. The returned value ignores any Sample-Descriptor changes.");
    cls.def_property_readonly("samples_until_next_descriptor",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getSamplesUntilNextDescriptor();
        },
        "Gets the number of same-type samples available in the queued packets. The returned value is up-to the next Sample-Descriptor-Changed packet if any.");
    cls.def_property_readonly("remote",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.isRemote();
        },
        "Returns true if the type of connection is remote.");
    cls.def("enqueue_multiple",
        [](daq::IConnection *object, std::variant<daq::IList*, py::list, daq::IEvalValue*>& packets)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            objectPtr.enqueueMultiple(getVariantValue<daq::IList*>(packets));
        },
        py::arg("packets"),
        "Places multiple packets at the back of the queue.");
    cls.def("dequeue_all",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.dequeueAll().detach();
        },
        "Removes all packets from the queue.");
    cls.def_property_readonly("samples_until_next_event_packet",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getSamplesUntilNextEventPacket();
        },
        "Gets the number of samples available in the queued packets until the next event packet. The returned value is up-to the next Event packet if any.");
    cls.def_property_readonly("samples_until_next_gap_packet",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.getSamplesUntilNextGapPacket();
        },
        "Gets the number of samples available in the queued packets until the next gap packet. The returned value is up-to the next Gap packet if any.");
    cls.def("has_event_packet",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.hasEventPacket();
        },
        "Queries if the connection has an event packet.");
    cls.def("has_gap_packet",
        [](daq::IConnection *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            return objectPtr.hasGapPacket();
        },
        "Queries if the connection has a gap packet.");
    cls.def("enqueue_with_scheduler",
        [](daq::IConnection *object, daq::IPacket* packet)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::ConnectionPtr::Borrow(object);
            objectPtr.enqueueWithScheduler(packet);
        },
        py::arg("packet"),
        "Places a packet at the back of the queue.");
}
