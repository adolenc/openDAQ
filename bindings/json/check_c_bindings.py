#!/usr/bin/env python3
"""Checks that opendaq.json and the C bindings describe the same API.

    check_c_bindings.py [opendaq.json [bindings/c/include]]

The JSON states, for every interface, method, factory, function and enum, the name it has in the C
bindings. This script reads the C headers and verifies the claim in both directions:

  * every C symbol the JSON names is declared, with the argument types the JSON implies,
  * every function the headers declare is named by the JSON,
  * the enums and the status codes agree by name and by value.

It is what keeps a language binding that is generated from the JSON alone from referring to a symbol
the C library does not export. Exits non-zero, listing the differences, when the two disagree.
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

# C spelling of the value types that are not enums. Everything else in a signature is an interface
# (IFoo -> daqFoo) or an enum (its c_name).
VALUE_TYPES = {
    "void": "void",
    "ErrCode": "daqErrCode",
    "Bool": "daqCBool",
    "Int": "daqCInt",
    "UInt": "daqCUInt",
    "Float": "daqCFloat",
    "SizeT": "daqCSizeT",
    "CharPtr": "daqCCharPtr",
    "ConstCharPtr": "daqCConstCharPtr",
    "VoidPtr": "daqCVoidPtr",
    "EnumType": "daqCEnumType",
    "IntfID": "daqIntfID",
    "ComplexFloat64": "daqComplexFloat64",
    "SourceLocation": "daqSourceLocation",
    "FuncCall": "daqFuncCall",
    "ProcCall": "daqProcCall",
    "EventCall": "daqEventCall",
    "int": "int",
}

# Hand-written in base_object.h with a shape that deliberately differs from the C++ one (the interface
# id is taken by value and the result is a daqBaseObject**), so only their presence is checked.
HAND_SHAPED = {"daqUnknown_queryInterface", "daqBaseObject_borrowInterface"}

FUNCTION_RE = re.compile(r"^[ \t]*(?!typedef\b)([\w \t\*]+?)\s+EXPORTED\s+(\w+)\s*\(([^;{]*)\)\s*;", re.M)
ENUM_RE = re.compile(r"typedef\s+enum\s+(\w+)\s*\{(.*?)\}\s*(\w+)\s*;", re.S)
DEFINE_RE = re.compile(r"^[ \t]*#define\s+(DAQ_\w+)\s+0x([0-9A-Fa-f]{8})u?\s*$", re.M)
COMMENT_RE = re.compile(r"/\*.*?\*/|//[^\n]*", re.S)


def normalize(c_type: str) -> str:
    return re.sub(r"\s+", "", c_type.replace("const ", "").replace("struct ", ""))


def read_headers(include: Path):
    functions: dict[str, tuple[str, list[str], str]] = {}
    enums: dict[str, dict[str, int]] = {}
    statuses: dict[str, int] = {}
    for header in sorted(include.rglob("*.h")):
        if "private" in header.relative_to(include).parts or header.name == "copendaq_private.h":
            continue
        text = COMMENT_RE.sub("", header.read_text(encoding="utf-8", errors="replace"))
        where = str(header.relative_to(include))
        for match in FUNCTION_RE.finditer(text):
            arguments = [a.strip() for a in match.group(3).split(",") if a.strip() and a.strip() != "void"]
            functions[match.group(2)] = (normalize(match.group(1)), arguments, where)
        for match in ENUM_RE.finditer(text):
            options, value = {}, -1
            for entry in (e.strip() for e in match.group(2).split(",")):
                if not entry:
                    continue
                name, _, raw = (part.strip() for part in entry.partition("="))
                value = int(raw.rstrip("uU"), 0) if raw else value + 1
                options[name] = value
            enums[match.group(3)] = options
        for match in DEFINE_RE.finditer(text):
            statuses[match.group(1)] = int(match.group(2), 16)
    return functions, enums, statuses


def main() -> int:
    here = Path(__file__).resolve().parent
    spec_path = Path(sys.argv[1]) if len(sys.argv) > 1 else here / "opendaq.json"
    include = Path(sys.argv[2]) if len(sys.argv) > 2 else here.parent / "c" / "include"

    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    functions, enums, statuses = read_headers(include)
    problems: list[str] = []

    enum_names = {e["name"]: e["c_name"] for e in spec["enums"]}

    def c_type(type_desc: dict) -> str:
        name = type_desc["name"]
        if name in VALUE_TYPES:
            base = VALUE_TYPES[name]
        elif name in enum_names:
            base = enum_names[name]
        elif re.match(r"I[A-Z]", name):
            base = "daq" + name[1:]
        else:
            base = "daq" + name
        # A const reference to a value type is passed by value in C.
        return base + "*" * type_desc.get("pointers", 0)

    # ---- what the JSON says the C bindings contain ----------------------------------------------
    expected: dict[str, tuple[str, list[dict], str]] = {}   # symbol -> (return type, arguments, origin)

    def expect(symbol: str, return_type: str, arguments: list[dict], origin: str) -> None:
        if symbol in expected:
            problems.append(f"{symbol}: named twice by the JSON ({expected[symbol][2]} and {origin})")
        expected[symbol] = (return_type, arguments, origin)

    for interface in spec["interfaces"]:
        c_name = interface.get("c_name")
        name = interface["name"]
        if c_name:
            intf_id = {"name": "intfId", "type": {"name": "IntfID", "pointers": 1}}
            expect(interface["c_interface_id_symbol"], "void", [intf_id], f"{name} interface id")
            self_argument = {"name": "self", "type": {"name": name, "pointers": 1}}
            for method in interface["methods"]:
                expect(method["c_symbol"], c_type(method["return_type"]),
                       [self_argument] + method["arguments"], f"{name}::{method['name']}")
        elif any("c_symbol" in m for m in interface["methods"]):
            problems.append(f"{name}: methods have a c_symbol but the interface has no c_name")
        for factory in interface.get("factories", []):
            if "c_symbol" in factory:
                expect(factory["c_symbol"], c_type(factory["return_type"]), factory["arguments"],
                       f"factory {factory['name']}")
    for function in spec["functions"]:
        if "c_symbol" in function:
            expect(function["c_symbol"], c_type(function["return_type"]), function["arguments"],
                   f"function {function['name']}")

    # ---- symbols ---------------------------------------------------------------------------------
    for symbol in sorted(set(expected) - set(functions)):
        problems.append(f"{symbol}: named by the JSON ({expected[symbol][2]}) but not declared in the C headers")
    for symbol in sorted(set(functions) - set(expected)):
        problems.append(f"{symbol}: declared in {functions[symbol][2]} but not named by the JSON")

    # ---- signatures ------------------------------------------------------------------------------
    for symbol in sorted(set(expected) & set(functions) - HAND_SHAPED):
        return_type, arguments, origin = expected[symbol]
        c_return, c_arguments, where = functions[symbol]
        if normalize(return_type) != c_return:
            problems.append(f"{symbol}: returns {c_return} in {where}, the JSON implies {return_type}")
        if len(arguments) != len(c_arguments):
            problems.append(f"{symbol}: {len(c_arguments)} arguments in {where}, the JSON implies {len(arguments)}")
            continue
        for argument, declared in zip(arguments, c_arguments):
            declared_type = normalize(re.sub(r"\w+$", "", declared))
            implied = normalize(c_type(argument["type"]))
            # Untyped out-pointers (void**) are spelled daqBaseObject** where they return objects.
            if declared_type != implied and (implied, declared_type) != ("void**", "daqBaseObject**"):
                problems.append(f"{symbol}: argument '{argument['name']}' is {declared_type} in {where}, "
                                f"the JSON implies {implied}")

    # ---- enums -----------------------------------------------------------------------------------
    for enum in spec["enums"]:
        declared = enums.get(enum["c_name"])
        if declared is None:
            problems.append(f"enum {enum['c_name']}: named by the JSON but not declared in the C headers")
            continue
        implied = {option["c_name"]: option.get("value") for option in enum["options"]}
        if declared != implied:
            problems.append(f"enum {enum['c_name']}: options differ, C headers {declared} vs JSON {implied}")
    for name in sorted(set(enums) - {e["c_name"] for e in spec["enums"]}):
        problems.append(f"enum {name}: declared in the C headers but not in the JSON")

    # ---- status codes ----------------------------------------------------------------------------
    implied_statuses = {re.sub(r"^OPENDAQ_", "DAQ_", code["name"]): code["code"] for code in spec["error_codes"]}
    for name in sorted(set(implied_statuses) | set(statuses)):
        if statuses.get(name) != implied_statuses.get(name):
            have = "missing" if name not in statuses else f"0x{statuses[name]:08X}"
            want = "missing" if name not in implied_statuses else f"0x{implied_statuses[name]:08X}"
            problems.append(f"status {name}: C headers {have}, JSON {want}")

    unbound = [i["name"] for i in spec["interfaces"] if "c_name" not in i]
    print(f"{spec_path.name} names {len(expected)} C functions, {len(spec['enums'])} enums and "
          f"{len(implied_statuses)} status codes; the C headers declare {len(functions)} functions, "
          f"{len(enums)} enums and {len(statuses)} status codes.")
    print(f"Not in the C bindings ({len(unbound)} interfaces): {', '.join(unbound)}")

    if problems:
        print(f"\n{len(problems)} difference(s):", file=sys.stderr)
        for problem in problems:
            print(f"  {problem}", file=sys.stderr)
        return 1
    print("The JSON and the C headers agree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
