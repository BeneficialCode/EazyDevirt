﻿namespace EazyDevirt.Architecture;

/// <summary>
/// These opcodes typically pertain to actions within the vm itself.
/// </summary>
internal enum SpecialOpCodes : uint
{
    /// <summary>
    /// Used when calling a virtualized method from within another virtualized method.
    /// </summary>
    EazCall = 0x80000000 // [1232524850, 5] 0x060003CD
}