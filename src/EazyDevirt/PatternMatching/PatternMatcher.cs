﻿using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching;

internal class PatternMatcher
{
    public PatternMatcher()
    {
        OpCodes = new Dictionary<int, VMOpCode>();
        OpCodePatterns = new List<IOpCodePattern>();
        foreach (var type in typeof(PatternMatcher).Assembly.GetTypes())
            if (type.GetInterface(nameof(IOpCodePattern)) != null)
                if (Activator.CreateInstance(type) is IOpCodePattern instance)
                    OpCodePatterns.Add(instance);
    }
    
    private Dictionary<int, VMOpCode> OpCodes { get; }
    private List<IOpCodePattern> OpCodePatterns { get; }
    
    public void SetOpCodeValue(int value, VMOpCode opCode) => OpCodes[value] = opCode;

    public VMOpCode GetOpCodeValue(int value) => OpCodes.TryGetValue(value, out var opc) ? opc : VMOpCode.DefaultNopOpCode;

    public IOpCodePattern FindOpCode(VMOpCode vmOpCode, int index = 0)
    {
        if (!vmOpCode.SerializedDelegateMethod.HasMethodBody) return null!;
        
        foreach (var pat in OpCodePatterns)
        {
            if (pat.MatchEntireBody 
                    ? !MatchesEntire(pat, vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions, index) || !pat.Verify(vmOpCode, index)
                    : GetAllMatchingInstructions(pat, vmOpCode, index).Count <= 0)
                continue;
            
            //if (!pat.AllowMultiple)
            //    OpCodePatterns.Remove(pat);
            return pat;
        }

        return null!;
    }

    /// <summary>
    /// Checks if pattern matches a method's instructions body
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against</param>
    /// <param name="method">Method to match body against</param>
    /// <param name="index">Index of method's instruction body to start matching at</param>
    /// <returns>Whether the pattern matches method's instruction body</returns>
    public static bool MatchesPattern(IOpCodePattern pattern, VMOpCode vmOpCode, int index = 0)
    {
        if (!vmOpCode.SerializedDelegateMethod.HasMethodBody) return false;

        return pattern.MatchEntireBody 
            ? MatchesEntire(pattern, vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions, index) &&
              pattern.Verify(vmOpCode, index) 
            : GetAllMatchingInstructions(pattern, vmOpCode, index).Count > 0;
    }
    
    /// <summary>
    /// Checks if pattern matches a method's instructions body
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against</param>
    /// <param name="method">Method to match body against</param>
    /// <param name="index">Index of method's instruction body to start matching at</param>
    /// <returns>Whether the pattern matches method's instruction body</returns>
    public static bool MatchesPattern(IPattern pattern, MethodDefinition? method, int index = 0)
    {
        if (!(method?.HasMethodBody).GetValueOrDefault()) return false;
        
        return pattern.MatchEntireBody 
            ? MatchesEntire(pattern, method!.CilMethodBody!.Instructions, index) &&
              pattern.Verify(method, index) 
            : GetAllMatchingInstructions(pattern, method!, index).Count > 0;
    }


    /// <summary>
    /// Checks if pattern matches a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="instructions">Instructions to match body against</param>
    /// <param name="index">Index of the instructions collection to start matching at</param>
    /// <returns>Whether the pattern matches the given instructions</returns>
    public static bool MatchesPattern(IPattern pattern, CilInstructionCollection instructions, int index = 0) =>
        pattern.MatchEntireBody
            ? MatchesEntire(pattern, instructions, index) &&
              pattern.Verify(instructions, index)
            : GetAllMatchingInstructions(pattern, instructions, index).Count > 0;

    private static bool CanInterchange(IPattern pat, CilInstruction ins, CilOpCode patOpCode)
    {
        var patIns = new CilInstruction(patOpCode);
        if (ins.IsLdcI4())
            return pat.InterchangeLdcI4OpCodes && patIns.IsLdcI4();

        if (ins.IsLdloc())
            return patIns.IsLdloc();

        if (ins.IsStloc())
            return pat.InterchangeStlocOpCodes && patIns.IsStloc();

        return false;
    }
    
    private static bool MatchesEntire(IPattern pattern, CilInstructionCollection instructions, int index)
    {
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count || (pattern.MatchEntireBody && pat.Count > instructions.Count)) return false;
        
        for (var i = 0; i < pat.Count; i++)
        {
            if (pat[i] == CilOpCodes.Nop)
                continue;

            var instruction = instructions[i + index];
            if (instructions[i + index].OpCode != pat[i] && !CanInterchange(pattern, instruction, pat[i]))
                return false;
        }

        return true;
    }
    
    /// <summary>
    /// Gets all matching instruction sets in a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="instructions">CIL instruction body to match pattern against</param>
    /// <param name="index">Index of method's instruction body to start matching at.</param>
    /// <returns>List of matching instruction sets</returns>
    public static List<CilInstruction[]> GetAllMatchingInstructions(IPattern pattern, CilInstructionCollection instructions, int index = 0)
    {
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return new List<CilInstruction[]>();

        var matchingInstructions = new List<CilInstruction[]>();
        for (var i = index; i < instructions.Count; i++)
        {
            var current = new List<CilInstruction>();

            for(int j = i, k = 0; j < instructions.Count && k < pat.Count; j++, k++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode != pat[k] && !CanInterchange(pattern, instruction, pat[k]))
                    break;
                current.Add(instructions[j]);
            }

            if (current.Count == pat.Count && pattern.Verify(instructions, index + i))
                matchingInstructions.Add(current.ToArray());
        }

        return matchingInstructions;
    }

    /// <summary>
    /// Gets all matching instruction sets in a method's instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="method">Method to match pattern against</param>
    /// <param name="index">Index of method's instruction body to start matching at.</param>
    /// <returns>List of matching instruction sets</returns>
    public static List<CilInstruction[]> GetAllMatchingInstructions(IPattern pattern, MethodDefinition method, int index = 0)
    {
        if (!method.HasMethodBody) return new List<CilInstruction[]>();
        if(method.CilMethodBody == null) return new List<CilInstruction[]>();
        var instructions = method.CilMethodBody!.Instructions;
        
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return new List<CilInstruction[]>();

        var matchingInstructions = new List<CilInstruction[]>();
        for (var i = index; i < instructions.Count; i++)
        {
            var current = new List<CilInstruction>();

            for(int j = i, k = 0; j < instructions.Count && k < pat.Count; j++, k++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode != pat[k] && !CanInterchange(pattern, instruction, pat[k]))
                    break;
                current.Add(instructions[j]);
            }

            if (current.Count == pat.Count && pattern.Verify(method, index + i))
                matchingInstructions.Add(current.ToArray());
        }

        return matchingInstructions;
    }
    
    /// <summary>
    /// Gets all matching instruction sets in a vm opcode delegate method instructions body.
    /// </summary>
    /// <param name="pattern">Pattern to match instructions against.</param>
    /// <param name="vmOpCode">VM Opcode to match pattern against</param>
    /// <param name="index">Index of method's instruction body to start matching at.</param>
    /// <returns>List of matching instruction sets</returns>
    public static List<CilInstruction[]> GetAllMatchingInstructions(IOpCodePattern pattern, VMOpCode vmOpCode, int index = 0)
    {
        if (!vmOpCode.SerializedDelegateMethod.HasMethodBody) return new List<CilInstruction[]>();
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        
        var pat = pattern.Pattern;
        if (index + pat.Count > instructions.Count) return new List<CilInstruction[]>();

        var matchingInstructions = new List<CilInstruction[]>();

        for(var i = 0; i < instructions.Count; i++)
        {
            // verify the i+1 is not out of index
            var idx = i + 1;
            if (idx >= instructions.Count) break;
            int count = pat.Count;
            idx = i + count - 1;
            if(idx >= instructions.Count) break;
            if (instructions[i].OpCode == pat[0] && instructions[i + 1].OpCode == pat[1]
                && instructions[i+count -1].OpCode == pat[count-1])
            {
                index = i;
                break;
            }
        }

        for (var i = index; i < instructions.Count; i++)
        {
            var current = new List<CilInstruction>();

            for(int j = i, k = 0; j < instructions.Count && k < pat.Count; j++, k++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode != pat[k] && !CanInterchange(pattern, instruction, pat[k]))
                    break;
                current.Add(instructions[j]);
            }
            if (index > instructions.Count || index + 1 > instructions.Count)
                break;
            if (current.Count == pat.Count && pattern.Verify(vmOpCode, index))
                matchingInstructions.Add(current.ToArray());
        }

        return matchingInstructions;
    }
}