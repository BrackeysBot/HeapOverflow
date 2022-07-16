using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HeapOverflow.Data.ValueConverters;

/// <summary>
///     Represents a <see cref="ValueConverter{TModel, TProvider}" /> which converts between a <see cref="List{T}" /> of strings,
///     and an array of <see cref="byte" /> values.
/// </summary>
internal sealed class ListOfStringToBytesConverter : ValueConverter<List<string>, byte[]>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ListOfStringToBytesConverter" /> class.
    /// </summary>
    public ListOfStringToBytesConverter()
        : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ListOfStringToBytesConverter" /> class.
    /// </summary>
    public ListOfStringToBytesConverter(ConverterMappingHints? mappingHints)
        : base(
            v => ToByteArray(v),
            v => FromByteArray(v),
            mappingHints)
    {
    }

    private static byte[] ToByteArray(List<string> list)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(list.Count);

        foreach (string item in list)
            writer.Write(item);

        return buffer.ToArray();
    }

    private static List<string> FromByteArray(byte[] bytes)
    {
        using var buffer = new MemoryStream(bytes);
        using var reader = new BinaryReader(buffer);
        int count = reader.ReadInt32();

        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
            list.Add(reader.ReadString());

        return list;
    }
}
