﻿using AsmResolver.DotNet.Code.Cil;

namespace EazyDevirt.Core.Architecture;

internal record VMExceptionHandler
{
    public int VMHandlerType { get; }
    public int CatchType { get; }
    public uint TryStart { get; }
    public uint HandlerStart { get; }
    public uint TryLength { get; }
    public uint FilterStart { get; }

    public CilExceptionHandlerType HandlerType
    {
        get
        {
            return VMHandlerType switch
            {
                0 => CilExceptionHandlerType.Exception,
                1 => CilExceptionHandlerType.Finally,
                // 4 =>  CilExceptionHandlerType.Fault,
                2 => CilExceptionHandlerType.Filter,
                _ => throw new NotSupportedException()
            };
        }
    }
    
    public VMExceptionHandler(BinaryReader reader)
    {
        VMHandlerType = reader.ReadByte();
        CatchType = reader.ReadInt32();
        TryStart = reader.ReadUInt32();
        HandlerStart = reader.ReadUInt32();
        TryLength = reader.ReadUInt32();
        FilterStart = reader.ReadUInt32();
    }

    public override string ToString() =>
        $"HandlerType: {HandlerType} | TryStart: {TryStart} | TryEnd: {TryStart + TryLength}";
}