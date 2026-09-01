# C bindings

`include/` and `src/` contain a thin C wrapper around the openDAQ C++ API: one
`daq<Type>_<method>(...)` free function per interface method, opaque
`typedef struct daq<Type> daq<Type>;` handles, and `daq<Type>_create<Factory>(...)`
constructors. Every wrapper is a `reinterpret_cast` back to the C++ interface plus
a forwarded call, so the C bindings add no behaviour of their own.

The bindings are **generated**, not hand-maintained. RTGen discovers every
bindable core interface and emits it.

## Regenerating the bindings

From this directory:

```sh
./run_rtgen.sh
```

The generator lives in `shared/tools/RTGen/src/project/RTGen.C/`
(`CConfigGenerator.cs` = discovery/driver, `CGenerator.cs` = per-interface
emission, `Templates/` = the output templates). The committed
`shared/tools/RTGen/bin/RTGen.C.dll` is what `rtgen.exe` actually loads, so after
changing any source you must rebuild it. From `shared/tools/RTGen`:

```sh
mcs -target:library -out:bin/RTGen.C.dll \
    -r:bin/RTGen.Library.dll -r:bin/RTGen.Cpp.dll -r:bin/LightInject.dll -r:System.Core.dll \
    src/project/RTGen.C/Generators/*.cs \
    src/project/RTGen.C/CCompositionRoot.cs \
    src/project/RTGen.C/Properties/AssemblyInfo.cs
```

The project is also part of `src/project/RTGen.sln` for building with Visual
Studio. Keep the rebuilt binary committed so the bindings can be regenerated
without a build environment.

**Templates exist twice** — `src/project/RTGen.C/Templates/` (source of truth) and
`bin/Templates/` (what `rtgen.exe` reads). Keep them in sync; only the `bin/`
copies affect generated output.

## How the generator works

Notable mechanical rules encoded in `CGenerator`:

- Each generated `.cpp` includes both `<opendaq/opendaq.h>` **and** the interface's
  own core C++ header (`<coretypes|coreobjects|opendaq/<name>.h>`). The umbrella
  header does not declare private/internal/newer interfaces, so the specific header
  is needed for the `daq::I<Name>` type to be visible.
- **Symbol names are always `daq<Name>_<method>`**, with `<Name>` the C++ interface name
  minus its leading `I`, so every C function can be derived from the C++ one without a
  lookup table. Same for the interface id: `IList` → `DAQ_LIST_INTF_ID` and
  `daqList_getInterfaceId`.
- **Core-type `Object` suffix — on the type only.** The interfaces behind the
  `daqCoreType` enum members — `IBoolean`, `IInteger`, `IFloat`, `IString`, `IList`,
  `IDict`, `IRatio`, `IProcedure`, `IFunction`, `IBinaryData`, `IComplexNumber`,
  `IStruct`, `IEnumeration` — plus `ICoreType` name their opaque handle
  `daq<Name>Object`. Two of them (`IFloat`, `ICoreType`) would otherwise collide outright
  with the `daqFloat` (`double`) and `daqCoreType` (enum) value types declared in
  `ccommon.h`; the rest are suffixed so the family reads consistently. So `IList` gives
  `typedef struct daqListObject daqListObject;` while its members stay `daqList_pushBack`,
  `daqList_createList`, etc. Matched on the full interface name (with the leading `I`), so
  the value types themselves are untouched. `IBaseObject` is not in the set — its C form
  is the hand-written `daqBaseObject` (`void`) handle.
- void-returning methods emit a bare `void` return type (not `daqvoid`).
- Callback typedefs (`FuncCall`/`ProcCall`/`EventCall`) are forwarded with
  `reinterpret_cast`, not `static_cast`.

## Exceptions: what is intentionally not generated

The C surface is a subset of the C++ API. Everything the C++ API has that the C
bindings do not falls into one of the four categories below. All are encoded in
`CConfigGenerator` / `CGenerator`.

### 1. Skipped interfaces (`SkipHeaders`, plus the automatic overloaded-method skip)

Whole interfaces that are not emitted:

- **C++-dependent core headers.** `task_internal` (`#include <opendaq/task_flow.h>`
  → Taskflow), `streaming_private`, `logger_sink_base_private`,
  `logger_thread_pool_private` (spdlog). Including their core C++ header drags in
  third-party headers that are not on the C bindings include path.
- **Overloaded methods.** `IScalingCalcPrivate` (`scaleData` ×2),
  `IDataRuleCalcPrivate` (`calculateRule` ×2, `calculateSample` ×2). C has no
  function overloading, so two methods would collapse to one duplicate C symbol.
  Any interface with a duplicate method name is skipped automatically.
- **Unmarshallable / build-excluded internals.** `dict_element_type` and
  `list_element_type` (an `IntfID*` out-parameter the wrapper cannot marshal),
  `type_manager_private`, `permissions_internal`, `property_object_class_internal`,
  `user_internal`, `component_deserialize_context`, `wrapped_data_packet`. These were
  never part of the built surface (commented out of the old CMake list).
- **`baseobject`** — hand-written instead (see category 4).

### 2. Factory-only headers

Headers that declare constructors for an interface defined elsewhere
(`OPENDAQ_DECLARE_CLASS_FACTORY_WITH_INTERFACE(...)` with no
`DECLARE_OPENDAQ_INTERFACE`). To parse them the sanitizer injects a throwaway
placeholder interface, and placeholder-only files are then skipped, so their
factories are not emitted. Examples: `core_opendaq_event_args` (19 constructors of
`ICoreEventArgs`), `component/search_filter` (5 of `ISearchFilter`),
`owning_list_factory`/`owning_dict_factory` (`IList`/`IDict`), `binary_data_packet`
(`IDataPacket`), `json_deserializer` (`IDeserializer`). (The pre-existing committed
`component/search_filter.h` is kept because generation writes in place.)

### 3. Forbidden methods and factories

Individual members dropped or commented out (`ForbiddenMethods`,
`ForbiddenFactories`, `ForbiddenTypes`):

- `IInspectable::getInterfaceIds` — returns an `IntfID**` out-array with no clean C
  ownership; emitted commented-out.
- `createClient` — declared on `IInstance` but constructs an `IDevice`; the factory
  wrapper cannot reconcile the mismatched out-parameter type.
- `createMiMallocAllocator` — the mimalloc symbol is not universally linkable.
- Methods taking `ComplexFloat64` or `SourceLocation` arguments — those C types are
  not implemented, so such methods are commented out.

Additionally, `createWinDebugLoggerSink` is emitted wrapped in `#ifdef _WIN32`
(`WindowsOnlyFactories`), because its C++ symbol only exists on Windows.

### 4. Hand-written base ABI (`ccoretypes/base_object.h`)

`IUnknown` (`queryInterface`, `addRef`, `releaseRef`) and
`IBaseObject : IUnknown` (`borrowInterface`, `dispose`, `getHashCode`, `equals`,
`toString`) are hand-written because their C shape deliberately differs from a
mechanical translation:

- `queryInterface`/`borrowInterface` take `daqIntfID` **by value** and return the
  result as `daqBaseObject**`, rather than the raw C++ `(IntfID, void** obj)`.
- `addRef`/`releaseRef` return `int` and are exposed as `static inline` helpers
  (`daqAddRef`, `daqReleaseRef`).
- It adds members with no interface equivalent (`daqBaseObject_create`, inline
  `daqDispose`/`daqGetHashCode`/… convenience wrappers).

These six functions are the only part of the C surface that genuinely cannot be
derived from the C++ headers; everything else in the gap is one of the rules above.

Other hand-written, non-generated files: `ccommon.h`, `copendaq.h`,
`copendaq_private.h`, `private/*`, and the per-library `common.h`, `errors.h`, and
`factories.h`.

## Building and testing

Configure with `-DOPENDAQ_GENERATE_C_BINDINGS=ON`; the tests turn on with
`-DOPENDAQ_ENABLE_TESTS=ON` (target `copendaq`, tests `test_ccoretypes`,
`test_ccoreobjects`, `test_copendaq_*`, and the pure-C `test_c_compile_and_run`).
