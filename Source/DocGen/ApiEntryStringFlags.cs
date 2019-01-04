using System;

namespace DocGen
{
    [Flags]
    enum ApiEntryStringFlags: long
    {
        None = 0,
        ReturnValue            = 0b00000000000000001,
        Namespaces             = 0b00000000000000010,
        CliTypeNames           = 0b00000000000000100,
        DeclaringTypes         = 0b00000000000001000,
        ParameterTypes         = 0b00000000000010000,
        ParameterNames         = 0b00000000000100000,
        GenericParameters      = 0b00000000001000000,
        CliNames               = 0b00000000010000000,
        Inheritance            = 0b00000000100000000,
        Modifiers              = 0b00000001000000000,
        Accessors              = 0b00000010000000000,

        Default = ReturnValue | Namespaces | DeclaringTypes | ParameterNames | GenericParameters,
        ShortDisplayName = ParameterTypes | GenericParameters
    }
}