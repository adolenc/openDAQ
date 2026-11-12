# JSON description of the C bindings

[`opendaq.json`](./opendaq.json) is a machine-readable description of the
openDAQ C API (`bindings/c/`): every interface with its methods, factories and
functions, the enums and the status codes. It describes exactly what the C
library exports, under the names things have there, so that a binding for
another language can be generated from this file alone.

It also carries what makes up the shape of openDAQ underneath the C functions:
the COM-style interface ids, the inheritance, and the position of every method
in its vtable.

## Names

Everything is spelled as in C: an interface is its opaque handle type
(`daqStreamReader`), a type in a signature is the C type (`daqCSizeT`,
`daqSampleType`), an enumerator is the C enumerator (`daqSampleTypeFloat64`), a
status code the C macro (`DAQ_ERR_NOTFOUND`).

The C functions of an interface follow from its `name` by one rule, without
exceptions:

| In the JSON | C function |
| --- | --- |
| interface `daqStreamReader` | `daqStreamReader_getInterfaceId(daqIntfID* intfId)` returns its `id` |
| `methods[]`: `read` | `daqStreamReader_read(daqStreamReader* self, <arguments>)` |
| `factories[]`: `createStreamReader` | `daqStreamReader_createStreamReader(<arguments>)` |
| `functions[]`: `setErrorInfo` of `daqErrorInfo` | `daqErrorInfo_setErrorInfo(<arguments>)` |

`factories` are the constructors of objects that have the interface as their
main one; their first argument returns the new object. `functions` are the
functions the core declares next to an interface without making them methods
(the error info attached to the calling thread, for one).

## Schema notes

- `types` defines the types of the C ABI that are neither interfaces nor
  enums, as `bindings/c/include/ccommon.h` declares them: the aliases of C's
  own types (`daqCBool` is a `uint8_t`), the structs passed by value
  (`daqIntfID`, with their `fields` and, when packed, their `pack`), and the
  callbacks. Together with the interfaces and the enums they are every type
  the file uses, apart from C's own, and each name is defined once. (The
  handles `daqUnknown` and `daqBaseObject` are interfaces here, although
  `ccommon.h` declares them as `void` so that C converts any handle to them
  without a cast; at the ABI a pointer is a pointer either way.)
- A type is its C `name` and its number of `pointers`. An argument marked
  `is_reference` is a value type that C++ takes by const reference
  (`const IntfID&`): the C function takes the value itself, a call through the
  vtable passes its address.
- Arguments carry, where the core headers state them, a `default_value`,
  `allow_null`, `steals_reference` and `is_polymorphic`. Whether the callee
  reads or writes through a pointer is not recorded: the doc comment's
  `@param[out]` says so for a human reader.
- Container types carry the element types the headers annotate them with:
  `daqList` has a `value_type`, `daqDict` a `key_type` and a `value_type`
  (absent when the header leaves the container untyped). `daqEvent` likewise
  carries the `sender_type` and `args_type` its handlers are called with.
- `interfaces[].id` is the interface id and `base` the interface it extends.
  Methods are listed in vtable order: `absolute_vtable_slot` counts from the
  start of the whole vtable, `relative_vtable_slot` only the interface's own
  methods, so the two differ by the size of the inherited vtable (single
  inheritance, declaration order). The `abi` section states the calling
  convention.
- `error_codes` holds every status a call can return: the `DAQ_ERR_*` errors
  and the non-error statuses (`DAQ_SUCCESS`, `DAQ_NO_MORE_ITEMS`, ...).
- `library` and `header` say where in the core something is declared. A member
  carries them only when they differ from its interface's.
- `doc` is the documentation comment of the core header, which refers to
  arguments by the names used here.

## What is described

Whatever the C bindings contain, and nothing else. The few parts of the C++ API
that the C bindings leave out (internal interfaces whose signatures use C++
only types, factories that exist in some builds only) do not appear;
`bindings/c/README.md` lists them with the reasons.

Both this file and the C bindings are generated from the core C++ headers, and
name things by the same code,
[`CBindingRules.cs`](../../shared/tools/RTGen/src/project/RTGen.Shared/CBindingRules.cs).
`check_c_bindings.py` verifies the outcome: that every function described here
is declared in the C headers with the same signature and the other way around,
that the enums and status codes agree by name and value, and that the file
defines every type it uses.

## Regenerating the JSON

The JSON can be regenerated manually using the `RTGen.Json` project.
Run the `run_rtgen.sh` script from this folder:

```sh
./run_rtgen.sh
```

If the [generator sources](../../shared/tools/RTGen/src/project/RTGen.Json/) were updated
the `RTGen.Json.dll` binary has to be rebuilt first. `bindings/regenerate.sh` does both, for
the JSON and for the C bindings (it needs the mono C# compiler, `mcs`):

```sh
../regenerate.sh
```

Sources under `RTGen.Shared/` are compiled into both `RTGen.Json.dll` and `RTGen.C.dll`: the
two generators walk the same headers and must agree on what they declare.

The project is also part of `src/project/RTGen.sln` for building with Visual
Studio.

Please keep the binaries committed to the repository, so that the JSON can be
regenerated without requiring a build environment. When updating the generator,
try and keep the JSON backwards-compatible, so that existing language bindings
do not break.
