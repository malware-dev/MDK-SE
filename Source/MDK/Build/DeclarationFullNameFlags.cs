using System;

namespace MDK.Build
{
    [Flags]
    public enum DeclarationFullNameFlags
    {
        Default = 0b0000,

        WithoutNamespaceName = 0b0001
    }
}