using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTGen.Shared
{
    /// <summary>
    /// What the C bindings contain and how their symbols are named. The C generator emits by these rules
    /// and the JSON generator describes their outcome, so the API description is a description of exactly
    /// what the C library exports.
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

        /// <summary>
        /// Enums whose options are not prefixed with the enum name in C, because their C++ options
        /// already carry a prefix of their own (ctBool) or because the short C names are long established.
        /// </summary>
        public static readonly ISet<string> EnumsWithUnprefixedOptions = new HashSet<string> { "CoreType", "LockingStrategy" };

        /// <summary>
        /// Arguments of the hand written functions whose C declaration is not the mechanical translation of
        /// the C++ one, keyed by "Interface::method": the C++ type and the name of each argument as the C
        /// header declares them. The interface queries return the object through a typed
        /// <c>daqBaseObject**</c> where C++ has an untyped <c>void**</c>.
        /// </summary>
        public static readonly IDictionary<string, KeyValuePair<string, string>[]> HandWrittenArguments =
            new Dictionary<string, KeyValuePair<string, string>[]>
            {
                ["IUnknown::queryInterface"] = new[]
                {
                    new KeyValuePair<string, string>("const IntfID&", "intfId"),
                    new KeyValuePair<string, string>("IBaseObject**", "interfacePtr")
                },
                ["IBaseObject::borrowInterface"] = new[]
                {
                    new KeyValuePair<string, string>("const IntfID&", "intfId"),
                    new KeyValuePair<string, string>("IBaseObject**", "interfacePtr")
                }
            };

        /// <summary>A function of the C bindings that has no counterpart in the C++ API.</summary>
        public class HandWrittenFactory
        {
            public string Interface;
            public string Name;
            /// <summary>Arguments after the created object: C++ type and name.</summary>
            public KeyValuePair<string, string>[] Arguments;
            public string Doc;
        }

        /// <summary>
        /// Factories that exist only in the C bindings and are written by hand (ccoretypes/factories.h).
        /// They are listed here so that the API description knows about them.
        /// </summary>
        public static readonly HandWrittenFactory[] HandWrittenFactories =
        {
            new HandWrittenFactory
            {
                Interface = "IEventHandler",
                Name = "createEventHandler",
                Arguments = new[] { new KeyValuePair<string, string>("EventCall", "call") },
                Doc = "@brief Creates an event handler that calls a C function.\n"
                      + "@param call The function to call with the sender and the arguments of the event.\n\n"
                      + "C++ creates event handlers from callables through templates, which have no exported "
                      + "symbol; this factory is provided by the C bindings."
            }
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

        /// <summary>The C spelling of a type that appears in a signature.</summary>
        /// <param name="cppName">Name in the C++ API, without namespace: "IString", "Bool", "SampleType".</param>
        /// <param name="enumNames">Names of the enums of the API.</param>
        public static string TypeName(string cppName, ISet<string> enumNames)
        {
            if (enumNames.Contains(cppName))
            {
                return EnumTypeName(cppName);
            }
            if (ValueTypeNames.TryGetValue(cppName, out string renamed))
            {
                return Prefix + renamed;
            }
            if (InterfaceNameRegex.IsMatch(cppName))
            {
                return InterfaceTypeName(cppName);
            }
            // C's own types (void, int) are spelled the same; openDAQ's value types get the prefix.
            return char.IsLower(cppName[0]) ? cppName : Prefix + cppName;
        }

        /// <summary>The C name of an enum: "SampleType" gives "daqSampleType".</summary>
        public static string EnumTypeName(string enumName)
        {
            return Prefix + (ValueTypeNames.TryGetValue(enumName, out string renamed) ? renamed : enumName);
        }

        /// <summary>The C name of an enum option: "SampleType::Float64" gives "daqSampleTypeFloat64".</summary>
        public static string EnumOptionName(string enumName, string optionName)
        {
            return EnumsWithUnprefixedOptions.Contains(enumName)
                       ? Prefix + char.ToUpperInvariant(optionName[0]) + optionName.Substring(1)
                       : Prefix + enumName + optionName;
        }

        /// <summary>The C name of a status code. The core prefixes them with "OPENDAQ_", the C bindings
        /// with "DAQ_": "OPENDAQ_ERR_NOTFOUND" gives "DAQ_ERR_NOTFOUND".</summary>
        public static string StatusName(string coreName)
        {
            const string corePrefix = "OPENDAQ_";
            return coreName.StartsWith(corePrefix, StringComparison.Ordinal)
                       ? "DAQ_" + coreName.Substring(corePrefix.Length)
                       : coreName;
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
            return MemberSymbol(interfaceName, FunctionMemberName(functionName));
        }

        /// <summary>The name a freestanding function goes by as a member of its interface:
        /// "daqSetErrorInfo" gives "setErrorInfo".</summary>
        public static string FunctionMemberName(string functionName)
        {
            string name = functionName.StartsWith(Prefix, StringComparison.Ordinal) && functionName.Length > Prefix.Length
                              ? functionName.Substring(Prefix.Length)
                              : functionName;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
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
