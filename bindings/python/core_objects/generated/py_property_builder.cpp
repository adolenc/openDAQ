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

#include "py_core_objects/py_core_objects.h"
#include "py_core_types/py_converter.h"
#include "py_core_objects/py_variant_extractor.h"

PyDaqIntf<daq::IPropertyBuilder, daq::IBaseObject> declareIPropertyBuilder(pybind11::module_ m)
{
    return wrapInterface<daq::IPropertyBuilder, daq::IBaseObject>(m, "IPropertyBuilder");
}

void defineIPropertyBuilder(pybind11::module_ m, PyDaqIntf<daq::IPropertyBuilder, daq::IBaseObject> cls)
{
    cls.doc() = "The builder interface of Properties. Allows for construction of Properties through the `build` method.";

    m.def("PropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name){
        return daq::PropertyBuilder_Create(getVariantValue<daq::IString*>(name));
    }, py::arg("name"));

    m.def("BoolPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IBoolean*, bool, daq::IEvalValue*>& defaultValue){
        return daq::BoolPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IBoolean*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("IntPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IInteger*, int64_t, daq::IEvalValue*>& defaultValue){
        return daq::IntPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IInteger*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("FloatPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IFloat*, double, daq::IEvalValue*>& defaultValue){
        return daq::FloatPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IFloat*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("StringPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IString*, py::str, daq::IEvalValue*>& defaultValue){
        return daq::StringPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IString*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("ListPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IList*, py::list, daq::IEvalValue*>& defaultValue){
        return daq::ListPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IList*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("DictPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IDict*, py::dict>& defaultValue){
        return daq::DictPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IDict*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("RatioPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IRatio*, std::pair<int64_t, int64_t>>& defaultValue){
        return daq::RatioPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IRatio*>(defaultValue));
    }, py::arg("name"), py::arg("default_value"));

    m.def("ObjectPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, daq::IPropertyObject* defaultValue){
        return daq::ObjectPropertyBuilder_Create(getVariantValue<daq::IString*>(name), defaultValue);
    }, py::arg("name"), py::arg("default_value"));

    m.def("ReferencePropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, daq::IEvalValue* referencedPropertyEval){
        return daq::ReferencePropertyBuilder_Create(getVariantValue<daq::IString*>(name), referencedPropertyEval);
    }, py::arg("name"), py::arg("referenced_property_eval"));

    m.def("FunctionPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, daq::ICallableInfo* callableInfo){
        return daq::FunctionPropertyBuilder_Create(getVariantValue<daq::IString*>(name), callableInfo);
    }, py::arg("name"), py::arg("callable_info"));

    m.def("SelectionPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IList*, py::list, daq::IEvalValue*>& selectionValues, std::variant<daq::IInteger*, int64_t, daq::IEvalValue*>& defaultValue){
        return daq::SelectionPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IList*>(selectionValues), getVariantValue<daq::IInteger*>(defaultValue));
    }, py::arg("name"), py::arg("selection_values"), py::arg("default_value"));

    m.def("SparseSelectionPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, std::variant<daq::IDict*, py::dict>& selectionValues, std::variant<daq::IInteger*, int64_t, daq::IEvalValue*>& defaultValue){
        return daq::SparseSelectionPropertyBuilder_Create(getVariantValue<daq::IString*>(name), getVariantValue<daq::IDict*>(selectionValues), getVariantValue<daq::IInteger*>(defaultValue));
    }, py::arg("name"), py::arg("selection_values"), py::arg("default_value"));

    m.def("StructPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, daq::IStruct* defaultValue){
        return daq::StructPropertyBuilder_Create(getVariantValue<daq::IString*>(name), defaultValue);
    }, py::arg("name"), py::arg("default_value"));

    m.def("EnumerationPropertyBuilder", [](std::variant<daq::IString*, py::str, daq::IEvalValue*>& name, daq::IEnumeration* defaultValue){
        return daq::EnumerationPropertyBuilder_Create(getVariantValue<daq::IString*>(name), defaultValue);
    }, py::arg("name"), py::arg("default_value"));


    cls.def("build",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.build().detach();
        },
        "Builds and returns a Property using the currently set values of the Builder.");
    cls.def_property("value_type",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getValueType();
        },
        [](daq::IPropertyBuilder *object, daq::CoreType type)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setValueType(type);
        },
        "Gets the Value type of the Property. / Sets the Value type of the Property. Values written to the corresponding Property value must be of the same type.");
    cls.def_property("name",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getName().toStdString();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::IString*, py::str, daq::IEvalValue*>& name)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setName(getVariantValue<daq::IString*>(name));
        },
        "Gets the Name of the Property. / Sets the Name of the Property. The names of Properties in a Property object must be unique. The name is used as the key to the corresponding Property value when getting/setting the value.");
    cls.def_property("description",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getDescription().toStdString();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::IString*, py::str, daq::IEvalValue*>& description)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setDescription(getVariantValue<daq::IString*>(description));
        },
        "Gets the short string Description of the Property. / Sets the short string Description of the Property.");
    cls.def_property("unit",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getUnit().detach();
        },
        [](daq::IPropertyBuilder *object, const py::object& unit)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setUnit(pyObjectToBaseObject(unit));
        },
        py::return_value_policy::take_ownership,
        "Gets the Unit of the Property. / Sets the Unit of the Property.");
    cls.def_property("min_value",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getMinValue().detach();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::INumber*, double, int64_t, daq::IEvalValue*>& min)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setMinValue(getVariantValue<daq::INumber*>(min));
        },
        py::return_value_policy::take_ownership,
        "Gets the Minimum value of the Property. Available only if the Value type is `ctInt` or `ctFloat`. / Sets the Minimum value of the Property. Available only if the Value type is `ctInt` or `ctFloat`.");
    cls.def_property("max_value",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getMaxValue().detach();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::INumber*, double, int64_t, daq::IEvalValue*>& max)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setMaxValue(getVariantValue<daq::INumber*>(max));
        },
        py::return_value_policy::take_ownership,
        "Gets the Maximum value of the Property. Available only if the Value type is `ctInt` or `ctFloat`. / Sets the Maximum value of the Property. Available only if the Value type is `ctInt` or `ctFloat`.");
    cls.def_property("default_value",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return baseObjectToPyObject(objectPtr.getDefaultValue());
        },
        [](daq::IPropertyBuilder *object, const py::object& value)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setDefaultValue(pyObjectToBaseObject(value));
        },
        py::return_value_policy::take_ownership,
        "Gets the Default value of the Property. / Sets the Default value of the Property. The Default value must always be configured for a Property to be in a valid state. Exceptions are Function/Procedure and Reference properties. The function will freeze default value if it is freezable.");
    cls.def_property("suggested_values",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getSuggestedValues().detach();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::IList*, py::list, daq::IEvalValue*>& values)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setSuggestedValues(getVariantValue<daq::IList*>(values));
        },
        py::return_value_policy::take_ownership,
        "Gets the list of Suggested values. Contains values that are the optimal gettings for the corresponding Property value. These values, however, are not enforced when getting a new Property value. / Sets the list of Suggested values. Contains values that are the optimal settings for the corresponding Property value. These values, however, are not enforced when setting a new Property value.");
    cls.def_property("visible",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getVisible().detach();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::IBoolean*, bool, daq::IEvalValue*>& visible)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setVisible(getVariantValue<daq::IBoolean*>(visible));
        },
        py::return_value_policy::take_ownership,
        "Used to determine whether the property is visible or not. / Used to determine whether the property is visible or not.");
    cls.def_property("read_only",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getReadOnly().detach();
        },
        [](daq::IPropertyBuilder *object, std::variant<daq::IBoolean*, bool, daq::IEvalValue*>& readOnly)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setReadOnly(getVariantValue<daq::IBoolean*>(readOnly));
        },
        py::return_value_policy::take_ownership,
        "Used to determine whether the Property is a read-only property or not. / Used to determine whether the Property is a read-only property or not.");
    cls.def_property("selection_values",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return baseObjectToPyObject(objectPtr.getSelectionValues());
        },
        [](daq::IPropertyBuilder *object, const py::object& values)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setSelectionValues(pyObjectToBaseObject(values));
        },
        py::return_value_policy::take_ownership,
        "Gets the list or dictionary of selection values. / Sets the list or dictionary of selection values. If the list/dictionary is not empty, the property is a Selection property, and must have the Value type `ctInt`.");
    cls.def_property("referenced_property",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getReferencedProperty().detach();
        },
        [](daq::IPropertyBuilder *object, daq::IEvalValue* propertyEval)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setReferencedProperty(propertyEval);
        },
        py::return_value_policy::take_ownership,
        "Gets the referenced property. / Sets the referenced property. If set, all getters except for the `Name`, `Referenced property`, and `Is referenced` getters will return the value of the `Referenced property`.");
    cls.def_property("validator",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getValidator().detach();
        },
        [](daq::IPropertyBuilder *object, const py::object& validator)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setValidator(pyObjectToBaseObject(validator));
        },
        py::return_value_policy::take_ownership,
        "Gets the validator of the Property. / Sets the validator of the Property.");
    cls.def_property("coercer",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getCoercer().detach();
        },
        [](daq::IPropertyBuilder *object, const py::object& coercer)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setCoercer(pyObjectToBaseObject(coercer));
        },
        py::return_value_policy::take_ownership,
        "Gets the coercer of the Property. / Sets the coercer of the Property.");
    cls.def_property("callable_info",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getCallableInfo().detach();
        },
        [](daq::IPropertyBuilder *object, const py::object& callable)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setCallableInfo(pyObjectToBaseObject(callable));
        },
        py::return_value_policy::take_ownership,
        "Gets the Callable information objects of the Property that specifies the argument and return types of the callable object stored as the Property value. / Sets the Callable information objects of the Property that specifies the argument and return types of the callable object stored as the Property value.");
    /*
    cls.def_property("on_property_value_write",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getOnPropertyValueWrite().detach();
        },
        [](daq::IPropertyBuilder *object, daq::IEvent* event)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setOnPropertyValueWrite(event);
        },
        py::return_value_policy::take_ownership,
        "Gets a custom on-write event. Used mostly when cloning properties. / Sets a custom on-write event. Used mostly when cloning properties.");
    */
    /*
    cls.def_property("on_property_value_read",
        [](daq::IPropertyBuilder *object)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            return objectPtr.getOnPropertyValueRead().detach();
        },
        [](daq::IPropertyBuilder *object, daq::IEvent* event)
        {
            py::gil_scoped_release release;
            const auto objectPtr = daq::PropertyBuilderPtr::Borrow(object);
            objectPtr.setOnPropertyValueRead(event);
        },
        py::return_value_policy::take_ownership,
        "Gets a custom on-read event. Used mostly when cloning properties. / Sets a custom on-read event. Used mostly when cloning properties.");
    */
}
