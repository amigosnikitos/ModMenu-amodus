using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Prints the members of some types of the game libraries (names and signatures only), straight from the metadata.
// Usage: ApiDump <folder with dlls> t:TypeName ... g:substring ...
//   t:Name  - print every member of the type with that simple name (nested types too)
//   g:text  - list every member of Assembly-CSharp whose name contains the text
internal static class Program
{
    private static int Main(string[] args)
    {
        string dir = args[0];
        var types = new HashSet<string>(StringComparer.Ordinal);
        var greps = new List<string>();

        foreach (var a in args.Skip(1))
        {
            if (a.StartsWith("t:"))
                types.Add(a.Substring(2));
            else if (a.StartsWith("g:"))
                greps.Add(a.Substring(2));
        }

        foreach (var file in Directory.GetFiles(dir, "*.dll").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                Scan(file, types, greps);
            }
            catch (Exception e)
            {
                Console.WriteLine("## cannot read " + Path.GetFileName(file) + ": " + e.GetType().Name);
            }
        }

        return 0;
    }

    private static void Scan(string file, HashSet<string> types, List<string> greps)
    {
        using var stream = File.OpenRead(file);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata)
            return;

        var reader = pe.GetMetadataReader();
        var provider = new NameProvider();
        string asmName = Path.GetFileName(file);
        bool isGameCode = asmName.Equals("Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase);

        foreach (var handle in reader.TypeDefinitions)
        {
            var td = reader.GetTypeDefinition(handle);
            string full = FullName(reader, handle);
            string simple = reader.GetString(td.Name);
            string top = TopLevelName(reader, handle);

            if (isGameCode && greps.Count > 0)
                Grep(reader, td, full, greps);

            if (!types.Contains(simple) && !types.Contains(top))
                continue;

            string baseName = td.BaseType.IsNil ? "-" : HandleName(reader, td.BaseType);
            bool isEnum = baseName == "System.Enum";
            Console.WriteLine("=== " + full + " : " + baseName + "   [" + asmName + "]");

            foreach (var fh in td.GetFields())
            {
                var fd = reader.GetFieldDefinition(fh);
                string name = reader.GetString(fd.Name);

                if (isEnum)
                {
                    if (name == "value__")
                        continue;

                    Console.WriteLine("  E " + name + " = " + EnumValue(reader, fd));
                    continue;
                }

                string kind = (fd.Attributes & FieldAttributes.Static) != 0 ? "static " : string.Empty;
                string type;
                try
                {
                    type = fd.DecodeSignature(provider, null);
                }
                catch
                {
                    type = "?";
                }

                Console.WriteLine("  F " + kind + type + " " + name);
            }

            foreach (var mh in td.GetMethods())
            {
                var md = reader.GetMethodDefinition(mh);
                string name = reader.GetString(md.Name);
                string kind = (md.Attributes & MethodAttributes.Static) != 0 ? "static " : string.Empty;

                try
                {
                    var sig = md.DecodeSignature(provider, null);
                    var names = new string[sig.ParameterTypes.Length];
                    foreach (var ph in md.GetParameters())
                    {
                        var p = reader.GetParameter(ph);
                        if (p.SequenceNumber >= 1 && p.SequenceNumber <= names.Length)
                            names[p.SequenceNumber - 1] = reader.GetString(p.Name);
                    }

                    var parts = new List<string>();
                    for (int i = 0; i < names.Length; i++)
                        parts.Add(sig.ParameterTypes[i] + " " + (names[i] ?? ("p" + i)));

                    Console.WriteLine("  M " + kind + sig.ReturnType + " " + name + "(" + string.Join(", ", parts) + ")");
                }
                catch
                {
                    Console.WriteLine("  M " + kind + name + "(?)");
                }
            }
        }
    }

    private static void Grep(MetadataReader reader, TypeDefinition td, string full, List<string> greps)
    {
        foreach (var mh in td.GetMethods())
        {
            string name = reader.GetString(reader.GetMethodDefinition(mh).Name);
            foreach (var g in greps)
            {
                if (name.IndexOf(g, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("GREP[" + g + "] " + full + "::" + name + "()");
                    break;
                }
            }
        }

        foreach (var fh in td.GetFields())
        {
            string name = reader.GetString(reader.GetFieldDefinition(fh).Name);
            foreach (var g in greps)
            {
                if (name.IndexOf(g, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("GREP[" + g + "] " + full + "." + name);
                    break;
                }
            }
        }
    }

    private static string EnumValue(MetadataReader reader, FieldDefinition fd)
    {
        try
        {
            var handle = fd.GetDefaultValue();
            if (handle.IsNil)
                return "?";

            var c = reader.GetConstant(handle);
            var blob = reader.GetBlobReader(c.Value);
            switch (c.TypeCode)
            {
                case ConstantTypeCode.Int32: return blob.ReadInt32().ToString();
                case ConstantTypeCode.UInt32: return blob.ReadUInt32().ToString();
                case ConstantTypeCode.Int16: return blob.ReadInt16().ToString();
                case ConstantTypeCode.UInt16: return blob.ReadUInt16().ToString();
                case ConstantTypeCode.Byte: return blob.ReadByte().ToString();
                case ConstantTypeCode.SByte: return blob.ReadSByte().ToString();
                case ConstantTypeCode.Int64: return blob.ReadInt64().ToString();
                case ConstantTypeCode.UInt64: return blob.ReadUInt64().ToString();
                default: return "?";
            }
        }
        catch
        {
            return "?";
        }
    }

    internal static string FullName(MetadataReader r, TypeDefinitionHandle h)
    {
        var td = r.GetTypeDefinition(h);
        string name = r.GetString(td.Name);
        var decl = td.GetDeclaringType();
        if (!decl.IsNil)
            return FullName(r, decl) + "/" + name;

        string ns = r.GetString(td.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string TopLevelName(MetadataReader r, TypeDefinitionHandle h)
    {
        var td = r.GetTypeDefinition(h);
        var decl = td.GetDeclaringType();
        return decl.IsNil ? r.GetString(td.Name) : TopLevelName(r, decl);
    }

    internal static string HandleName(MetadataReader r, EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.TypeDefinition:
                return FullName(r, (TypeDefinitionHandle)h);
            case HandleKind.TypeReference:
                var tr = r.GetTypeReference((TypeReferenceHandle)h);
                string ns = r.GetString(tr.Namespace);
                string name = r.GetString(tr.Name);
                return ns.Length == 0 ? name : ns + "." + name;
            case HandleKind.TypeSpecification:
                return r.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(new NameProvider(), null);
            default:
                return "?";
        }
    }
}

internal sealed class NameProvider : ISignatureTypeProvider<string, object>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        Program.FullName(reader, handle);

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        Program.HandleName(reader, handle);

    public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";

    public string GetByReferenceType(string elementType) => "ref " + elementType;

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        genericType + "<" + string.Join(",", typeArguments) + ">";

    public string GetGenericMethodParameter(object genericContext, int index) => "!!" + index;

    public string GetGenericTypeParameter(object genericContext, int index) => "!" + index;

    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

    public string GetPinnedType(string elementType) => elementType;
}
