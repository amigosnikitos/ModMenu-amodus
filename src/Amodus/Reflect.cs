using System;
using System.Collections.Generic;
using System.Reflection;

namespace Amodus;

/// <summary>
/// Reads a property or field by name and returns null when it does not exist.
/// Handy for game members whose names may change between game versions.
/// </summary>
internal static class Reflect
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<(Type, string), MemberInfo> Cache = new Dictionary<(Type, string), MemberInfo>();

    public static object Get(object target, string name)
    {
        if (target == null)
            return null;

        try
        {
            var type = target.GetType();
            if (!Cache.TryGetValue((type, name), out var member))
            {
                member = Find(type, name);
                Cache[(type, name)] = member;
            }

            switch (member)
            {
                case PropertyInfo property:
                    return property.GetValue(target);
                case FieldInfo field:
                    return field.GetValue(target);
                default:
                    return null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MemberInfo Find(Type type, string name)
    {
        // Walk up the class hierarchy so that inherited members are found too.
        for (var t = type; t != null; t = t.BaseType)
        {
            var property = t.GetProperty(name, Flags);
            if (property != null)
                return property;

            var field = t.GetField(name, Flags);
            if (field != null)
                return field;
        }

        return null;
    }
}
