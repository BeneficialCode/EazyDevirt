﻿using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Devirtualization;
// ReSharper disable InconsistentNaming

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record Newarr : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldloc_1,     // 46	0071	ldloc.1
        CilOpCodes.Call,        // 47	0072	call	class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type, int32)
        CilOpCodes.Stloc_S,     // 48	0077	stloc.s	V_6 (6)
        CilOpCodes.Ldarg_0,     // 49	0079	ldarg.0
        CilOpCodes.Newobj,      // 50	007A	newobj	instance void VMArray::.ctor()
        CilOpCodes.Dup,         // 51	007F	dup
        CilOpCodes.Ldloc_S,     // 52	0080	ldloc.s	V_6 (6)
        CilOpCodes.Callvirt,    // 53	0082	callvirt	instance void VMArray::method_4(class [mscorlib]System.Array)
        CilOpCodes.Callvirt,    // 54	0087	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 55	008C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Newarr;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedMemberReference
        {
            FullName: "System.Array System.Array::CreateInstance(System.Type, System.Int32)"
        };
}

internal record Ldlen : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Castclass,   // 3	000B	castclass	[mscorlib]System.Array
        CilOpCodes.Stloc_0,     // 4	0010	stloc.0
        CilOpCodes.Ldarg_0,     // 5	0011	ldarg.0
        CilOpCodes.Ldloc_0,     // 6	0012	ldloc.0
        CilOpCodes.Callvirt,    // 7	0013	callvirt	instance int32 [mscorlib]System.Array::get_Length()
        CilOpCodes.Newobj,      // 8	0018	newobj	instance void VMIntOperand::.ctor(int32)
        CilOpCodes.Callvirt,    // 9	001D	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 10	0022	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldlen;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 7].Operand is SerializedMemberReference
        {
            FullName: "System.Int32 System.Array::get_Length()"
        };
}

#region Ldelem

internal record LdelemInnerPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Call,        // 1	0001	call	instance class VMOperandType VM::PopStack()
        CilOpCodes.Castclass,   // 2	0006	castclass	Class38
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldarg_0,     // 5	000D	ldarg.0
        CilOpCodes.Ldloc_0,     // 6	000E	ldloc.0
        CilOpCodes.Call,        // 7	000F	call	instance class VMOperandType VM::method_85(class Class38)
        CilOpCodes.Callvirt,    // 8	0014	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldarg_1,     // 9	0019	ldarg.1
        CilOpCodes.Call,        // 10	001A	call	class VMOperandType VMOperandType::smethod_0(object, class [mscorlib]System.Type)
        CilOpCodes.Call,        // 11	001F	call	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 12	0024	ret
    };

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;
}

internal record Ldelem : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 9	0015	ldarg.0
        CilOpCodes.Ldloc_1,     // 10	0016	ldloc.1
        CilOpCodes.Callvirt,    // 11	0017	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 12	001C	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 2].Operand as SerializedMethodDefinition)!);
}

internal record Ldelem_Ref : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldsfld,      // 1	0001	ldsfld	class [mscorlib]System.Type TypeHelpers::type_object
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_Ref;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedFieldDefinition fieldDef
        && DevirtualizationContext.Instance.VMTypeFields.All(x =>
            x.Key.MetadataToken != fieldDef.MetadataToken);
}

#region Ldelem_I

internal record Ldelem_I : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldsfld,      // 1	0001	ldsfld	class [mscorlib]System.Type VM::type_5
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_I;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[2].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedFieldDefinition fieldDef
        && DevirtualizationContext.Instance.VMTypeFields.Any(x =>
            x.Key.MetadataToken == fieldDef.MetadataToken && x.Value.FullName is "System.IntPtr");
}

internal record Ldelem_I1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.SByte
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_I1;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.SByte"
        };
}

internal record Ldelem_I2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int16
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_I2;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Int16"
        };
}

internal record Ldelem_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int32
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_I4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Int32"
        };
}

internal record Ldelem_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Int64
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_I8;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Int64"
        };
}
#endregion Ldelem_I

#region Ldelem_R

internal record Ldelem_R4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Single
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_R4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Single"
        };
}

internal record Ldelem_R8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Double
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_R8;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Double"
        };
}
#endregion Ldelem_R

#region Ldelem_U

internal record Ldelem_U1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.Byte
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_U1;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.Byte"
        };
}

internal record Ldelem_U2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.UInt16
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_U2;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.UInt16"
        };
}

internal record Ldelem_U4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldtoken,     // 1	0001	ldtoken	[System.Runtime]System.UInt32
        CilOpCodes.Call,        // 2	0006	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 3	000B	callvirt	instance void VM::LdelemInner(class [mscorlib]System.Type)
        CilOpCodes.Ret          // 4	0010	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelem_U4;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new LdelemInnerPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[3].Operand as SerializedMethodDefinition)!)
        && vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand is SerializedTypeReference
        {
            FullName: "System.UInt32"
        };
}
#endregion Ldelem_U

internal record Ldelema : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 12	001C	ldarg.0
        CilOpCodes.Callvirt,    // 13	001D	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 14	0022	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Castclass,   // 15	0027	castclass	[mscorlib]System.Array
        CilOpCodes.Stloc_3,     // 16	002C	stloc.3
        CilOpCodes.Ldarg_0,     // 17	002D	ldarg.0
        CilOpCodes.Newobj,      // 18	002E	newobj	instance void Class41::.ctor()
    };

    public CilOpCode CilOpCode => CilOpCodes.Ldelema;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 6].Operand is SerializedMethodDefinition
        {
            DeclaringType.Fields.Count: 2
        } typeDef && typeDef.DeclaringType.Fields.All(x => x.Signature?.FieldType.FullName is "System.Array" or "System.Int64");

}

#endregion Ldelem

#region Stelem

internal record Stelem_I1 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 80	00E2	ldarg.0
        CilOpCodes.Ldtoken,     // 81	00E3	ldtoken	[mscorlib]System.SByte
        CilOpCodes.Call,        // 82	00E8	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Ldloc_0,     // 83	00ED	ldloc.0
        CilOpCodes.Ldloc_1,     // 84	00EE	ldloc.1
        CilOpCodes.Ldloc_2,     // 85	00EF	ldloc.2
        CilOpCodes.Callvirt,    // 86	00F0	callvirt	instance void VM::StelemInner(class [mscorlib]System.Type, object, int64, class [mscorlib]System.Array)
        CilOpCodes.Ret          // 87	00F5	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Stelem_I1;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedTypeReference
        {
            FullName: "System.SByte"
        };
}

internal record Stelem_I2 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 80	00E2	ldarg.0
        CilOpCodes.Ldtoken,     // 81	00E3	ldtoken	[mscorlib]System.Int16
        CilOpCodes.Call,        // 82	00E8	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Ldloc_0,     // 83	00ED	ldloc.0
        CilOpCodes.Ldloc_1,     // 84	00EE	ldloc.1
        CilOpCodes.Ldloc_2,     // 85	00EF	ldloc.2
        CilOpCodes.Callvirt,    // 86	00F0	callvirt	instance void VM::StelemInner(class [mscorlib]System.Type, object, int64, class [mscorlib]System.Array)
        CilOpCodes.Ret          // 87	00F5	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Stelem_I2;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedTypeReference
        {
            FullName: "System.Int16"
        };
}

internal record Stelem_I4 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 80	00E2	ldarg.0
        CilOpCodes.Ldtoken,     // 81	00E3	ldtoken	[mscorlib]System.Int32
        CilOpCodes.Call,        // 82	00E8	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Ldloc_0,     // 83	00ED	ldloc.0
        CilOpCodes.Ldloc_1,     // 84	00EE	ldloc.1
        CilOpCodes.Ldloc_2,     // 85	00EF	ldloc.2
        CilOpCodes.Callvirt,    // 86	00F0	callvirt	instance void VM::StelemInner(class [mscorlib]System.Type, object, int64, class [mscorlib]System.Array)
        CilOpCodes.Ret          // 87	00F5	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Stelem_I4;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedTypeReference
        {
            FullName: "System.Int32"
        };
}

internal record Stelem_I8 : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 80	00E2	ldarg.0
        CilOpCodes.Ldtoken,     // 81	00E3	ldtoken	[mscorlib]System.Int64
        CilOpCodes.Call,        // 82	00E8	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Ldloc_0,     // 83	00ED	ldloc.0
        CilOpCodes.Ldloc_1,     // 84	00EE	ldloc.1
        CilOpCodes.Ldloc_2,     // 85	00EF	ldloc.2
        CilOpCodes.Callvirt,    // 86	00F0	callvirt	instance void VM::StelemInner(class [mscorlib]System.Type, object, int64, class [mscorlib]System.Array)
        CilOpCodes.Ret          // 87	00F5	ret
    };

    public CilOpCode CilOpCode => CilOpCodes.Stelem_I8;

    public bool MatchEntireBody => false;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[index + 1].Operand is SerializedTypeReference
        {
            FullName: "System.Int64"
        };
}

#endregion Stelem