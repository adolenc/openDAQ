using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTGen.Shared
{
    /// <summary>
    /// What the C bindings contain and how their symbols are named. The C generator emits by these rules
    /// and the JSON generator records their outcome, so the API description always states exactly the
    /// symbols the C library exports.
    /// </summary>
    internal static class CBindingRules
    {
        public const string Prefix = "daq";

        /// <summary>
        /// Core headers that are deliberately not bound, keyed by file name, with the reason.
        /// Extend this when an interface cannot be expressed in C.
        /// </summary>
        /// <remarks>
        /// Most "private" and "internal" interfaces bind perfectly well and are generated; only the ones
        /// listed here are a problem, so they are excluded individually rather than by name pattern.
        /// </remarks>
        public static readonly IDictionary<string, string> UnboundHeaders = new Dictionary<string, string>
        {
            // Expose C++ only types (smart pointers, references, third party types) that have no C equivalent.
            ["logger_sink_base_private.h"]   = "returns spdlog SinkPtr",
            ["logger_thread_pool_private.h"] = "returns spdlog ThreadPoolPtr",
            ["task_internal.h"]              = "returns Taskflow and Task by value",
            ["data_rule_calc_private.h"]     = "takes a NumberPtr reference",
            ["streaming_private.h"]          = "takes a StringPtr",

            // Overloads collapse onto a single name in C.
            ["scaling_calc_private.h"]       = "two scaleData overloads would generate the same function"
        };

        /// <summary>
        /// Core headers whose C counterpart is written by hand instead of generated, with the reason. Their
        /// interfaces are part of the C bindings and follow the same naming rules.
        /// </summary>
        public static readonly IDictionary<string, string> HandWrittenHeaders = new Dictionary<string, string>
        {
            ["baseobject.h"] = "IUnknown and IBaseObject are written by hand in base_object.h"
        };

        /// <summary>The C names of the value types that alias C fundamental types.</summary>
        /// <remarks>
        /// Plain C values get a "daqC" prefix, openDAQ objects keep the plain "daq" prefix, so that every
        /// interface "IX" maps to "daqX" without exception. Without this the C bindings, which drop the "I"
        /// prefix from interface names, would emit the same identifier for an interface and a value type
        /// that share a name (Float/IFloat, CoreType/ICoreType).
        /// Only the aliases of C fundamental types are renamed; openDAQ's own enums (SampleType, PacketType,
        /// LogLevel, ...) as well as ErrCode and IntfID are domain types rather than C values and keep their
        /// "daq" prefix.
        /// </remarks>
        public static readonly IDictionary<string, string> ValueTypeNames = new Dictionary<string, string>
        {
            { "Bool", "CBool" },
            { "Int", "CInt" },
            { "UInt", "CUInt" },
            { "Float", "CFloat" },
            { "CharPtr", "CCharPtr" },
            { "ConstCharPtr", "CConstCharPtr" },
            { "VoidPtr", "CVoidPtr" },
            { "SizeT", "CSizeT" },
            { "EnumType", "CEnumType" },
            { "CoreType", "CCoreType" }
        };

        private static readonly Regex InterfaceNameRegex = new Regex(@"^I[A-Z]\w*$");

        /// <summary>Whether the header is left out of the C bindings altogether.</summary>
        public static bool IsUnbound(string headerPath, out string reason)
        {
            return UnboundHeaders.TryGetValue(Path.GetFileName(headerPath) ?? "", out reason);
        }

        /// <summary>Whether the C counterpart of the header is written by hand.</summary>
        public static bool IsHandWritten(string headerPath, out string reason)
        {
            return HandWrittenHeaders.TryGetValue(Path.GetFileName(headerPath) ?? "", out reason);
        }

        /// <summary>The opaque C handle type of an interface: "IStreamReader" gives "daqStreamReader".</summary>
        public static string InterfaceTypeName(string interfaceName)
        {
            return Prefix + interfaceName.Substring(1);
        }

        /// <summary>The C function of an interface method or of a factory creating the interface.</summary>
        public static string MemberSymbol(string interfaceName, string memberName)
        {
            return $"{InterfaceTypeName(interfaceName)}_{memberName}";
        }

        /// <summary>The C function that returns the interface id.</summary>
        public static string InterfaceIdSymbol(string interfaceName)
        {
            return MemberSymbol(interfaceName, "getInterfaceId");
        }

        /// <summary>
        /// The C function wrapping a freestanding exported function, which is named after the interface
        /// whose header declares it: "daqSetErrorInfo" in errorinfo.h gives "daqErrorInfo_setErrorInfo".
        /// </summary>
        public static string FunctionSymbol(string interfaceName, string functionName)
        {
            string name = functionName.StartsWith(Prefix, StringComparison.Ordinal) && functionName.Length > Prefix.Length
                              ? functionName.Substring(Prefix.Length)
                              : functionName;
            return MemberSymbol(interfaceName, char.ToLowerInvariant(name[0]) + name.Substring(1));
        }

        /// <summary>Whether every type in the signature of the function can be spelled in C.</summary>
        public static bool IsBindable(ExportedFunction function)
        {
            return TryMapType(function.ReturnType, out _, out _)
                   && function.Arguments.All(a => TryMapType(a.Key, out _, out _));
        }

        /// <summary>Translates a C++ type declaration of a freestanding function into C.</summary>
        /// <param name="cppDeclaration">The type as declared, for example "daq::IErrorInfo**".</param>
        /// <param name="cType">The C spelling, for example "daqErrorInfo**".</param>
        /// <param name="cppType">The C++ type to cast the C value to, or null when it needs no cast.</param>
        public static bool TryMapType(string cppDeclaration, out string cType, out string cppType)
        {
            cType = null;
            cppType = null;

            string modifiers = new string(cppDeclaration.Where(c => c == '*').ToArray());
            string name = cppDeclaration.Replace("*", " ").Replace("const ", " ").Trim();
            if (name.StartsWith("daq::", StringComparison.Ordinal))
            {
                name = name.Substring("daq::".Length);
            }

            if (name == "void")
            {
                cType = "void" + modifiers;
                return true;
            }

            if (InterfaceNameRegex.IsMatch(name))
            {
                cType = InterfaceTypeName(name) + modifiers;
                cppType = $"daq::{name}{modifiers}";
                return true;
            }

            if (name == "ErrCode" || ValueTypeNames.ContainsKey(name))
            {
                string cName = ValueTypeNames.TryGetValue(name, out string renamed) ? renamed : name;
                cType = Prefix + cName + modifiers;
                cppType = modifiers.Length > 0 ? $"daq::{name}{modifiers}" : null;
                return true;
            }

            return false;
        }
    }
}
