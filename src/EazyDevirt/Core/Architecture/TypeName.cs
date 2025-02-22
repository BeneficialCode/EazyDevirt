﻿
using System.Reflection;

namespace EazyDevirt.Core.Architecture;


// From saneki's eazdevirt

/// <summary>
/// Convenience class for interpreting the type names found in the
/// encrypted virtualization resources file.
/// </summary>
public class TypeName
{
    /// <summary>
    /// Full name as given in constructor.
    /// </summary>
    public string FullName { get; private set; }

    public TypeName(string fullName)
    {
        FullName = fullName;
    }

    /// <summary>
    /// Full assembly name.
    /// </summary>
    public string AssemblyFullName =>
        FullName[(FullName.IndexOf(", ", StringComparison.Ordinal) + 2)..];

    /// <summary>
    /// Assembly name.
    /// </summary>
    public AssemblyName AssemblyName => new(AssemblyFullName);

    /// <summary>
    /// Type name without namespace.
    /// </summary>
    public string NameWithoutNamespace => Name.Contains('.') ? Name.Split('.').Last() : Name;

    /// <summary>
    /// Namespace.
    /// </summary>
    public string Namespace => Name.Contains('.')
        ? string.Join(".",
            Name.Split('.').Reverse().Skip(1).Reverse().ToArray())
        : string.Empty;

    /// <summary>
    /// Type name without assembly info.
    /// </summary>
    public string Name
    {
        get
        {
            if (_nameInitialized) return _name;
            if (!FullName.Contains(", ")) return FullName;

            GetModifiersStack(FullName.Split(',')[0], out var fixedName);
            _name = fixedName;
            _nameInitialized = true;
            return fixedName;
        }
    }

    private bool _nameInitialized;
    private string _name = string.Empty;

    public Stack<string> Modifiers => FullName.Contains(", ")
        ? GetModifiersStack(FullName.Split(',')[0], out _)
        : new Stack<string>();

    /// <summary>
    /// Whether or not this name indicates the type is nested.
    /// </summary>
    public bool IsNested => Name.Contains('+');

    /// <summary>
    /// The parent type name if nested. If not nested, an empty string.
    /// </summary>
    public string ParentName => IsNested
        ? string.Join("+",
            Name.Split('+').Reverse().Skip(1).Reverse().ToArray())
        : string.Empty;

    /// <summary>
    /// Nested parent name without namespace, or empty if not nested.
    /// </summary>
    public string ParentNameWithoutNamespace => IsNested
        ? ParentName.Contains('.') ? ParentName.Split('.').Last() : ParentName
        : string.Empty;

    /// <summary>
    /// The nested child type name if nested. If not nested, an empty string.
    /// </summary>
    public string NestedName => IsNested ? Name.Split('+').Last() : string.Empty;

    /// <summary>
    /// Get a modifiers stack from a deserialized type name, and also
    /// provide the fixed name.
    /// </summary>
    /// <param name="rawName">Deserialized name</param>
    /// <param name="fixedName">Fixed name</param>
    /// <returns>Modifiers stack</returns>
    private static Stack<string> GetModifiersStack(string rawName, out string fixedName)
    {
        var stack = new Stack<string>();

        while (true)
        {
            if (rawName.EndsWith("[]"))
                stack.Push("[]");
            else if (rawName.EndsWith("*"))
                stack.Push("*");
            else if (rawName.EndsWith("&"))
                stack.Push("&");
            else break;

            rawName = rawName[..^stack.Peek().Length];
        }

        fixedName = rawName;
        return stack;
    }
}