﻿using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;
using EazyDevirt.Core.IO;
using EazyDevirt.PatternMatching;
using EazyDevirt.PatternMatching.Patterns;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Math;

#pragma warning disable CS8618

namespace EazyDevirt.Devirtualization.Pipeline;

internal sealed class ResourceParsing : StageBase
{
    private MethodDefinition? _resourceGetterMethod;
    private MethodDefinition? _resourceInitializationMethod;
    private MethodDefinition? _resourceModulusStringMethod;
    private ManifestResource? _resource;

    private string _resourceString;
    private byte[] _keyBytes;
    private string _modulusString;
    
    private protected override bool Init()
    {
        var found = FindVMStreamMethods();
        if (_resourceGetterMethod == null)
            Ctx.Console.Error("Failed to find VM resource stream getter method.");

        if (_resourceInitializationMethod == null)
            Ctx.Console.Error("Failed to find VM resource stream initialization method.");

        if (_resourceModulusStringMethod == null || _resourceModulusStringMethod.CilMethodBody!.Instructions.All
                (i => i.OpCode != CilOpCodes.Ldstr))
            Ctx.Console.Error("Failed to find valid VM resource modulus string method. Have strings been decrypted?");

        if (found)
        {
            if (Ctx.Options.Verbose)
            {
                Ctx.Console.Success("Found VM resource stream getter, initializer, and modulus string methods!");

                if (Ctx.Options.VeryVerbose)
                {
                    Ctx.Console.InfoStr("VM Resource Stream Getter", _resourceGetterMethod!.MetadataToken);
                    Ctx.Console.InfoStr("VM Resource Stream Initializer", _resourceInitializationMethod!.MetadataToken);
                    //Ctx.Console.InfoStr("VM Resource Modulus String Method",
                    //    _resourceModulusStringMethod!.MetadataToken);
                }
            }

            _resourceString = _resourceGetterMethod!.CilMethodBody!.Instructions[5].Operand?.ToString()!;
            _resource = Ctx.Module.Resources.FirstOrDefault(r => r.Name == _resourceString);
            if (_resource == null)
            {
                Ctx.Console.Error("Failed to get VM resource");
                found = false;
            }
            else if (Ctx.Options.Verbose)
            {
                Ctx.Console.Success("Found VM resource!");
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.InfoStr("VM Resource", _resourceString);
            }

            var a1 = (SerializedFieldDefinition)_resourceGetterMethod!.CilMethodBody!.Instructions[10].Operand!;
            if (!a1.HasFieldRva || a1.FieldRva!.GetType() != typeof(DataSegment))
            {
                Ctx.Console.Error("Failed to get VM resource stream key byte array.");
                found = false;
            }
       
            _keyBytes = ((DataSegment)a1.FieldRva!).Data;
            if (Ctx.Options.Verbose)
            {
                Ctx.Console.Success("Found VM resource stream key bytes!");
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.InfoStr("VM Resource Stream Key Bytes", BitConverter.ToString(_keyBytes));
            }

            _modulusString = _resourceModulusStringMethod!.CilMethodBody!.Instructions.FirstOrDefault
                (i => i.OpCode == CilOpCodes.Ldstr)?.Operand?.ToString()!;
            if(_modulusString == null)
            {
                _modulusString = "xjCxZdBSUWeYfAFjmNMJGz3FMdHLrPts3Y4qxq5NB9MECf3FnKNymtYH3WPLOp23cIOA+gc8XoVHrIrkyFirT6OX9g+xy/9jvZaguojcybxdWqJK1R7CP81QH/KrBBOr1zxOVUrDdP5v3hksZNGTlVd8JF9AOLHmr8BhmumhdHk=";
            }
            if (string.IsNullOrWhiteSpace(_modulusString))
            {
                Ctx.Console.Error("VM resource modulus string is null.");
                found = false;
            }
            else if (Ctx.Options.Verbose)
            {
                Ctx.Console.Success("Found VM resource modulus string!");
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.InfoStr("VM Resource Modulus String", _modulusString);
            }
        }

        Ctx.VMResourceGetterMdToken = _resourceGetterMethod!.MetadataToken;

        return found;
    }

    public override bool Run()
    {
        // the fun begins...
        if (!Init()) return false;

        var modulus1 = Convert.FromBase64String(_modulusString);
        var modulus2 = new byte[_keyBytes.Length + _keyBytes.Length];
        Buffer.BlockCopy(_keyBytes, 0, modulus2, 0, _keyBytes.Length);
        Buffer.BlockCopy(modulus1, 0, modulus2, _keyBytes.Length, modulus1.Length);
        
        var mod = new BigInteger(1, modulus2);
        var exp = BigInteger.ValueOf(65537L);

        var buffer = _resource!.GetData()!;
        Ctx.VMStream = new VMCipherStream(buffer, mod, exp);
        Ctx.VMResolverStream = new VMCipherStream(buffer, mod, exp);

        return true;
    }

    private bool FindVMStreamMethods()
    {
        foreach (var type in Ctx.Module.GetAllTypes())
        {
            if (_resourceGetterMethod != null && _resourceInitializationMethod != null) 
                return true;
            foreach (var method in type.Methods.Where(m =>
                         m is { Managed: true, IsPublic: true, IsStatic: true } &&
                         m.Signature?.ReturnType.FullName == typeof(Stream).FullName))
            {
                if (_resourceGetterMethod != null && _resourceInitializationMethod != null) 
                    return true;
                // 打印Token
                Console.WriteLine($"method: {method.MetadataToken}");

                IPattern pattern = new GetVMStreamPattern();

                if (_resourceGetterMethod != null ||!PatternMatcher.MatchesPattern(pattern, method)) 
                    continue;
                
                _resourceGetterMethod = method;

                int count = pattern.Pattern.Count();
                int idx = 0;
                for(int i=count;i< method.CilMethodBody!.Instructions.Count; i++)
                {
                    if(method.CilMethodBody!.Instructions[i].OpCode == CilOpCodes.Call)
                    {

                        if (idx == 0)
                        {
                            
                            string name = method.CilMethodBody!.Instructions[i].Operand!.ToString();
                            Console.WriteLine($"method: {name}");
                            if (name.IndexOf("InitializeArray") == -1)
                            {
                                idx = i;
                                _resourceModulusStringMethod = (SerializedMethodDefinition)method.CilMethodBody!.Instructions[i].Operand!;
                            }
                        }
                        else
                        {
                            _resourceInitializationMethod = (SerializedMethodDefinition)method.CilMethodBody!.Instructions[i].Operand!;
                            break;
                        }
                    }
                }

                //_resourceInitializationMethod =
                //    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[13].Operand!;
                //_resourceModulusStringMethod =
                //    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[12].Operand!;
                var getVmInstanceMethod = type.Methods.First(m =>
                    m.MetadataToken != _resourceGetterMethod.MetadataToken &&
                    m.MetadataToken != _resourceModulusStringMethod!.MetadataToken);

                if (!getVmInstanceMethod.Signature!.ReturnsValue)
                    throw new Exception("Failed to get VM Declaring type!");

                Ctx.VMDeclaringType = (TypeDefinition)getVmInstanceMethod.Signature.ReturnType.ToTypeDefOrRef();
            }

        }

        return false;
    }

    public ResourceParsing(DevirtualizationContext ctx) : base(ctx)
    {
    }
}