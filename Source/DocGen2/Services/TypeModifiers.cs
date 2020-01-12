using System;

namespace Mal.DocGen2.Services
{
    [Flags]
    enum TypeModifiers
    {
        None      = 0,
        Private   = 0b00000001,
        Protected = 0b00000010,
        Internal  = 0b00000100,
        Public    = 0b00001000,
        Sealed    = 0b00010000,
        Abstract  = 0b00100000,
        Virtual   = 0b01000000,
        Static    = 0b10000000
    }
}