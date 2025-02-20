using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;

namespace EazyDevirt.Core.Architecture;

internal record VMOpCode(SerializedFieldDefinition SerializedInstructionField = null!, SerializedMethodDefinition SerializedDelegateMethod = null!)
{
    public static  VMOpCode DefaultNopOpCode { get; } = new();
    
    /// <summary>
    /// Instruction field. These are all initialized in the .ctor of the container.
    /// </summary>
    public SerializedFieldDefinition SerializedInstructionField { get; } = SerializedInstructionField;
    
    /// <summary>
    /// The delegate method associated with this virtual instruction in the dictionary method.
    /// </summary>
    public SerializedMethodDefinition SerializedDelegateMethod { get; } = SerializedDelegateMethod;
    
    /// <summary>
    /// The virtual opcode, set when the instruction field is constructed.
    /// </summary>
    public int VirtualCode { get; set; } 
    
    /// <summary>
    /// The virtual operand type integer, set when the instruction field is constructed.
    /// </summary>
    public int VirtualOperandType { get; set; }
    
    /// <summary>
    /// The CIL operand type.
    /// </summary>
    // TODO: Implement pattern matching for operand types
    public CilOperandType CilOperandType
    {
        get
        {
            return VirtualOperandType switch
            {
                1 => CilOperandType.InlineBrTarget,
                3 => CilOperandType.InlineSwitch,
                7 => CilOperandType.InlineI,
                0 => CilOperandType.InlineTok,
                4 => CilOperandType.ShortInlineI,
                5 => CilOperandType.ShortInlineI,
                6 => CilOperandType.InlineI8,
                8 => CilOperandType.InlineR,
                11 => CilOperandType.ShortInlineI,
                10 => CilOperandType.InlineNone,
                12 => CilOperandType.InlineTok,
                9 => CilOperandType.ShortInlineVar,
                _ => throw new ArgumentOutOfRangeException(nameof(VirtualOperandType), VirtualOperandType, "Unknown operand type")
            };
        }
    }
    
    /// <summary>
    /// Whether or not the virtual opcode was successfully extracted from the container .ctor method.
    /// </summary>
    public bool HasVirtualCode { get; set; }

    /// <summary>
    /// Associated CIL opcode.
    /// </summary>
    public CilOpCode? CilOpCode { get; set; } = CilOpCodes.Nop;
    
    /// <summary>
    /// Associated special opcode.
    /// </summary>
    public SpecialOpCodes? SpecialOpCode { get; set; }
    
    /// <summary>
    /// Whether or not the virtual instruction was identified with a legitimate CIL or special opcode.
    /// </summary>
    public bool IsIdentified { get; set; }
    
    /// <summary>
    /// Whether or not the associated opcode is a special opcode.
    /// </summary>
    public bool IsSpecial { get; set; }

    public override string ToString() =>
        $"VirtualCode: {VirtualCode} | OperandType: {CilOperandType} ({VirtualOperandType}) | " +
        $"CilOpCode: {CilOpCode} | SpecialOpCode: {SpecialOpCode} | " +
        $"SerializedInstructionField: {SerializedInstructionField?.MetadataToken} | SerializedDelegateMethod: {SerializedDelegateMethod?.MetadataToken}";
}