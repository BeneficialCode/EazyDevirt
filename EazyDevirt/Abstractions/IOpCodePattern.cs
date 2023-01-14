﻿using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;
using EazyDevirt.Core.IO;

namespace EazyDevirt.Abstractions;

internal interface IOpCodePattern : IPattern
{
    CilOpCode CilOpCode => CilOpCodes.Nop;
    SpecialOpCode? SpecialOpCode => null;

    /// <summary>
    /// Whether the pattern can translate to CIL opcodes or is a special vm action.
    /// </summary>
    bool IsSpecial => false;

    /// <summary>
    /// Additional verification to ensure the match is valid.
    /// </summary>
    /// <param name="vmOpCode">VMOpCode the pattern is for</param>
    /// <param name="index">Index of the pattern</param>
    /// <returns>Whether verification is successful</returns>
    bool Verify(VMOpCode vmOpCode, int index = 0) => Verify(vmOpCode.SerializedDelegateMethod, index);
}