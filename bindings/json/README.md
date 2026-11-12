# opendaq.json

A machine-readable description of the openDAQ C API in [`bindings/c`](../c), from which a binding
for another language can be generated. It lists exactly what the C library exports, under its C
names, plus the interface ids and vtable slots.

| Key | Contents |
| --- | --- |
| `abi` | calling convention, interface id scheme, vtable layout |
| `types` | the types of `ccommon.h`: aliases (`daqCBool`), structs (`daqIntfID`), callbacks |
| `interfaces` | `id`, `base`, and `methods` (in vtable order), `factories`, `functions` |
| `enums`, `error_codes` | the contents of `cenums.h` and `cerrors.h` |

The C function names follow from the interface name:

| JSON | C |
| --- | --- |
| interface `daqStreamReader` | `daqStreamReader_getInterfaceId(daqIntfID*)` |
| method `read` | `daqStreamReader_read(daqStreamReader* self, ...)` |
| factory or function `createStreamReader` | `daqStreamReader_createStreamReader(...)` |

A type is a C `name` with a number of `pointers`. `is_reference` marks a value type that C++ takes
by const reference; the C function takes the value. `daqList`, `daqDict` and `daqEvent` may carry
their element types (`key_type`, `value_type`, `sender_type`, `args_type`).

What the C bindings leave out, and why, is listed in [`bindings/c/README.md`](../c/README.md).

## Regenerating

```sh
../regenerate.sh                         # rebuild the generators and regenerate the C bindings and this file (needs mcs)
./run_rtgen.sh                           # regenerate this file only, with the committed RTGen.Json.dll (needs mono)
python3 check_c_bindings.py              # check that this file and the C headers agree
```

Commit the rebuilt `shared/tools/RTGen/bin/*.dll` with the regenerated files, so that regenerating
needs no build environment.
