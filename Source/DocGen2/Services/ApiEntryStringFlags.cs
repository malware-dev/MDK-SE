using System;

namespace Mal.DocGen2.Services
{
    [Flags]
    enum ApiEntryStringFlags: long
    {
        None = 0,
        ReturnValue            = 0b0000000000000001,
        Namespaces             = 0b0000000000000010,
        CliTypeNames           = 0b0000000000000100,
        DeclaringTypes         = 0b0000000000001000,
        ParameterTypes         = 0b0000000000010000,
        ParameterNames         = 0b0000000000100000,
        GenericParameters      = 0b0000000001000000,
        CliNames               = 0b0000000010000000,
        Inheritance            = 0b0000000100000000,
        Modifiers              = 0b0000001000000000,
        Accessors              = 0b0000010000000000,
        Instantiation          = 0b0000100000000000,

        Default = ReturnValue | Namespaces | DeclaringTypes | ParameterNames | GenericParameters | Instantiation,
        ShortDisplayName = ParameterTypes | GenericParameters
    }
}