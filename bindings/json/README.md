# JSON metadata bindings

[`opendaq.json`](./opendaq.json) contains a machine-readable description of the
openDAQ C++ API surface - COM-style interfaces with vtable positions,
`extern "C"` factory symbols, enums and error codes. This is intended as an
easily parsable up-to-date input for generating external language bindings, an
alternative to RTGen.

## Schema notes

- The default `namespace` (`daq`) is declared once at top level; a type
  carries its own `namespace` only when it differs (external `tf`, `spdlog`,
  `std` types).
- Methods are called through the vtable at `absolute_vtable_slot`, which
  counts from the start of the whole vtable; `relative_vtable_slot` counts
  only the interface's own methods, so the two differ by the size of the
  inherited vtable (single inheritance, declaration order).
- Container types carry the element types the headers annotate them with:
  `IList` has a `value_type`, `IDict` a `key_type` and a `value_type` (absent
  when the header leaves the container untyped). `IEvent` likewise carries the
  `sender_type` and `args_type` its handlers are called with.
- `interfaces[].factories` are the constructors for objects that have the
  corresponding interface as their main one.
- `functions` are the freestanding `extern "C"` exports that are not tied to
  an interface (the `daqSetErrorInfo`/`daqGetErrorInfo` error-info helpers and
  the serializer-factory registry).
- `error_codes` holds every status a call can return: the `OPENDAQ_ERR_*`
  errors and the non-error statuses (`OPENDAQ_SUCCESS`, `OPENDAQ_NO_MORE_ITEMS`,
  ...).

## C bindings

Languages that cannot call through vtables go through the C bindings
(`bindings/c/`), which wrap this same surface. The JSON states the C name of
everything the C library exports, so a binding generator never has to derive
or guess one:

- `interfaces[].c_name` is the opaque handle type (`IStreamReader` →
  `daqStreamReader`), `c_interface_id_symbol` the function returning the
  interface's `id` (`daqStreamReader_getInterfaceId(daqIntfID*)`; it exists only
  in the C library, as in C++ the ID is a static constant).
- `methods[].c_symbol` is the function wrapping the method. It takes `self`, of
  type `<c_name>*`, followed by the method's arguments:
  `daqStreamReader_read(self, samples, count, timeoutMs, status)`.
- `factories[].c_symbol` is the function wrapping the factory, with the
  factory's own arguments. A factory marked `c_only` exists in the C bindings
  alone. Factories the core only declares under a preprocessor conditional
  (`createWinDebugLoggerSink`) are not listed at all, as they are not part of
  the C bindings.
- `functions[].c_symbol` is the function wrapping a freestanding function
  (`daqSetErrorInfo` → `daqErrorInfo_setErrorInfo`).
- `enums[].c_name` and `enums[].options[].c_name` are the C enum and its
  enumerators.

**Whatever has no `c_name` / `c_symbol` is not part of the C bindings.** That is
the case for a handful of internal interfaces whose signatures use C++ only
types; `bindings/c/README.md` lists them with the reasons.

The names follow from the rules in
[`CBindingRules.cs`](../../shared/tools/RTGen/src/project/RTGen.Shared/CBindingRules.cs),
which is compiled into both this generator and the C bindings generator, and
`check_c_bindings.py` verifies that the JSON and the C headers agree symbol for
symbol.

Argument types translate to C as follows: an interface `IFoo` is the opaque
`daqFoo` with the same number of pointers; the aliases of C fundamental types
get a `daqC` prefix (`Bool` → `daqCBool`, `Int` → `daqCInt`, `SizeT` →
`daqCSizeT`, ...); other value types and enums a plain `daq` prefix
(`ErrCode` → `daqErrCode`, `IntfID` → `daqIntfID`, `SampleType` →
`daqSampleType`). A const reference to a value type (`const IntfID&`) is passed
by value.

## Known gaps

- The two interfaces with overloaded methods (`IDataRuleCalcPrivate`,
  `IScalingCalcPrivate`): MSVC lays out adjacent overloads in reverse
  declaration order, so those slots are compiler-dependent.

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
