#!/usr/bin/env python3
"""Checks that opendaq.json describes the C bindings, the whole of them and nothing else.

    check_c_bindings.py [opendaq.json [bindings/c/include]]

The JSON is generated from the core C++ headers, the C bindings partly from the same headers and
partly by hand. This script reads the C headers and verifies that the two agree:

  * every function the JSON describes is declared, with the same return and argument types,
  * every function the headers declare is described by the JSON,
  * the enums and the status codes agree by name and by value,
  * every type the JSON uses is one it defines, and it defines each only once.

It is what lets a language binding be generated from the JSON alone. Exits non-zero, listing the
differences, when the JSON and the headers disagree.
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

# The types of C itself, which the JSON uses without defining them.
C_TYPES = {"void", "char", "int", "double", "size_t",
           "uint8_t", "uint16_t", "uint32_t", "uint64_t", "int8_t", "int16_t", "int32_t", "int64_t"}

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
            # The argument type is the declaration without the argument's name.
            types = [normalize(re.sub(r"\w+$", "", a)) for a in arguments]
            functions[match.group(2)] = (normalize(match.group(1)), types, where)
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


def c_type(type_desc: dict) -> str:
    # A reference (is_reference) is how C++ passes a value type; C passes the value itself.
    return type_desc["name"] + "*" * type_desc.get("pointers", 0)


def main() -> int:
    here = Path(__file__).resolve().parent
    spec_path = Path(sys.argv[1]) if len(sys.argv) > 1 else here / "opendaq.json"
    include = Path(sys.argv[2]) if len(sys.argv) > 2 else here.parent / "c" / "include"

    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    functions, enums, statuses = read_headers(include)
    problems: list[str] = []

    # ---- what the JSON describes -----------------------------------------------------------------
    described: dict[str, tuple[str, list[str], str]] = {}   # C function -> (return type, argument types, origin)

    def describe(function: str, return_type: str, argument_types: list[str], origin: str) -> None:
        if function in described:
            problems.append(f"{function}: described twice ({described[function][2]} and {origin})")
        described[function] = (return_type, argument_types, origin)

    for interface in spec["interfaces"]:
        name = interface["name"]
        describe(f"{name}_getInterfaceId", "void", ["daqIntfID*"], f"interface id of {name}")
        for kind, takes_self in (("methods", True), ("factories", False), ("functions", False)):
            for member in interface.get(kind, []):
                argument_types = [c_type(a["type"]) for a in member["arguments"]]
                if takes_self:
                    argument_types.insert(0, name + "*")
                describe(f"{name}_{member['name']}", c_type(member["return_type"]), argument_types,
                         f"{kind[:-1] if kind != 'factories' else 'factory'} of {name}")

    # ---- functions -------------------------------------------------------------------------------
    for function in sorted(set(described) - set(functions)):
        problems.append(f"{function}: described by the JSON ({described[function][2]}) but not declared in the C headers")
    for function in sorted(set(functions) - set(described)):
        problems.append(f"{function}: declared in {functions[function][2]} but not described by the JSON")

    for function in sorted(set(described) & set(functions)):
        return_type, argument_types, _ = described[function]
        declared_return, declared_types, where = functions[function]
        if normalize(return_type) != declared_return:
            problems.append(f"{function}: returns {declared_return} in {where}, {return_type} in the JSON")
        if [normalize(t) for t in argument_types] != declared_types:
            problems.append(f"{function}: takes ({', '.join(declared_types)}) in {where}, "
                            f"({', '.join(argument_types)}) in the JSON")

    # ---- enums -----------------------------------------------------------------------------------
    for enum in spec["enums"]:
        declared = enums.get(enum["name"])
        if declared is None:
            problems.append(f"enum {enum['name']}: described by the JSON but not declared in the C headers")
            continue
        options = {option["name"]: option.get("value") for option in enum["options"]}
        if declared != options:
            problems.append(f"enum {enum['name']}: {declared} in the C headers, {options} in the JSON")
    for name in sorted(set(enums) - {e["name"] for e in spec["enums"]}):
        problems.append(f"enum {name}: declared in the C headers but not described by the JSON")

    # ---- status codes ----------------------------------------------------------------------------
    codes = {code["name"]: code["code"] for code in spec["error_codes"]}
    for name in sorted(set(codes) | set(statuses)):
        if statuses.get(name) != codes.get(name):
            have = "missing" if name not in statuses else f"0x{statuses[name]:08X}"
            want = "missing" if name not in codes else f"0x{codes[name]:08X}"
            problems.append(f"status {name}: {have} in the C headers, {want} in the JSON")

    # ---- the JSON is self-contained --------------------------------------------------------------
    definitions = ([t["name"] for t in spec["types"]] + [i["name"] for i in spec["interfaces"]]
                   + [e["name"] for e in spec["enums"]])
    for name in sorted({n for n in definitions if definitions.count(n) > 1}):
        problems.append(f"type {name}: defined {definitions.count(name)} times by the JSON")
    defined = C_TYPES | set(definitions)
    used: dict[str, str] = {}

    def use(type_desc: dict, where: str) -> None:
        for key in ("name", "key_type", "value_type", "sender_type", "args_type"):
            if key in type_desc:
                used.setdefault(type_desc[key], where)

    for type_ in spec["types"]:
        for field in type_.get("fields", []):
            use(field["type"], type_["name"])
        for part in [type_.get("type"), type_.get("return_type")] + [a["type"] for a in type_.get("arguments", [])]:
            if part:
                use(part, type_["name"])
    for interface in spec["interfaces"]:
        if "base" in interface:
            used.setdefault(interface["base"], interface["name"])
        for kind in ("methods", "factories", "functions"):
            for member in interface.get(kind, []):
                where = f"{interface['name']}_{member['name']}"
                use(member["return_type"], where)
                for argument in member["arguments"]:
                    use(argument["type"], where)
    for name in sorted(set(used) - defined):
        problems.append(f"type {name}: used by {used[name]} but not defined by the JSON")

    print(f"{spec_path.name} describes {len(described)} functions, {len(spec['enums'])} enums and "
          f"{len(codes)} status codes; the C headers declare {len(functions)} functions, "
          f"{len(enums)} enums and {len(statuses)} status codes.")

    if problems:
        print(f"\n{len(problems)} difference(s):", file=sys.stderr)
        for problem in problems:
            print(f"  {problem}", file=sys.stderr)
        return 1
    print("The JSON and the C headers agree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
