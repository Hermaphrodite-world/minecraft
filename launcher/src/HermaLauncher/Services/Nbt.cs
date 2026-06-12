using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HermaLauncher.Services;

// 최소 NBT(Named Binary Tag) reader/writer — big-endian, **비압축**(servers.dat 형식 전용).
// 모르는 태그도 그대로 round-trip 보존(다른 서버 항목의 icon/acceptTextures 등 손실 방지).
// ※ gzip 압축 NBT(level.dat 등)에는 사용하지 않는다 — servers.dat 는 비압축이다.
public abstract class NbtTag { public string Name = string.Empty; public abstract byte Id { get; } }
public sealed class NbtByte : NbtTag { public sbyte Value; public override byte Id => 1; }
public sealed class NbtShort : NbtTag { public short Value; public override byte Id => 2; }
public sealed class NbtInt : NbtTag { public int Value; public override byte Id => 3; }
public sealed class NbtLong : NbtTag { public long Value; public override byte Id => 4; }
public sealed class NbtFloat : NbtTag { public float Value; public override byte Id => 5; }
public sealed class NbtDouble : NbtTag { public double Value; public override byte Id => 6; }
public sealed class NbtByteArray : NbtTag { public byte[] Value = Array.Empty<byte>(); public override byte Id => 7; }
public sealed class NbtString : NbtTag { public string Value = string.Empty; public override byte Id => 8; }
public sealed class NbtList : NbtTag { public byte ElementId; public List<NbtTag> Items = new(); public override byte Id => 9; }
public sealed class NbtIntArray : NbtTag { public int[] Value = Array.Empty<int>(); public override byte Id => 11; }
public sealed class NbtLongArray : NbtTag { public long[] Value = Array.Empty<long>(); public override byte Id => 12; }

public sealed class NbtCompound : NbtTag
{
    public List<NbtTag> Children = new();
    public override byte Id => 10;
    public NbtTag? Get(string name)
    {
        foreach (var c in Children)
            if (string.Equals(c.Name, name, StringComparison.Ordinal)) return c;
        return null;
    }
    public string GetString(string name) => Get(name) is NbtString s ? s.Value : string.Empty;
}

public static class Nbt
{
    // 손상/이상 NBT 에서 과도한 할당 방지(servers.dat 는 작다 — 정상값은 수십 이하).
    private const int MaxArrayLen = 1 << 24; // 16M
    private const int MaxListCount = 1 << 20; // ~100만

    public static NbtCompound ReadFile(string path)
    {
        using var fs = File.OpenRead(path);
        var id = ReadId(fs);
        if (id != 10) throw new InvalidDataException($"NBT root tag is {id}, expected compound(10)");
        var name = ReadString(fs);
        var root = (NbtCompound)ReadPayload(fs, 10);
        root.Name = name;
        return root;
    }

    // 원자적 쓰기(.tmp → replace)로 부분 쓰기 손상 방지.
    public static void WriteFile(string path, NbtCompound root)
    {
        var tmp = path + ".tmp";
        using (var fs = File.Create(tmp))
        {
            fs.WriteByte(10);
            WriteString(fs, root.Name);
            WritePayload(fs, root);
        }
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }

    // ── read ──
    private static NbtTag ReadNamed(Stream s, out bool end)
    {
        var id = ReadId(s);
        if (id == 0) { end = true; return new NbtByte(); }
        end = false;
        var name = ReadString(s);
        var tag = ReadPayload(s, id);
        tag.Name = name;
        return tag;
    }

    private static NbtTag ReadPayload(Stream s, byte id) => id switch
    {
        1 => new NbtByte { Value = (sbyte)ReadU8(s) },
        2 => new NbtShort { Value = (short)ReadU16(s) },
        3 => new NbtInt { Value = ReadI32(s) },
        4 => new NbtLong { Value = ReadI64(s) },
        5 => new NbtFloat { Value = BitConverter.Int32BitsToSingle(ReadI32(s)) },
        6 => new NbtDouble { Value = BitConverter.Int64BitsToDouble(ReadI64(s)) },
        7 => ReadByteArray(s),
        8 => new NbtString { Value = ReadString(s) },
        9 => ReadList(s),
        10 => ReadCompound(s),
        11 => ReadIntArray(s),
        12 => ReadLongArray(s),
        _ => throw new InvalidDataException($"Unknown NBT tag id {id}"),
    };

    private static NbtCompound ReadCompound(Stream s)
    {
        var c = new NbtCompound();
        while (true)
        {
            var child = ReadNamed(s, out var end);
            if (end) break;
            c.Children.Add(child);
        }
        return c;
    }

    private static NbtList ReadList(Stream s)
    {
        var elemId = ReadId(s);
        var count = ReadI32(s);
        if (count < 0 || count > MaxListCount)
            throw new InvalidDataException($"NBT list count out of range: {count}");
        var list = new NbtList { ElementId = elemId };
        for (var i = 0; i < count; i++)
            list.Items.Add(ReadPayload(s, elemId));
        return list;
    }

    private static NbtByteArray ReadByteArray(Stream s)
    {
        var len = ReadI32(s);
        if (len < 0 || len > MaxArrayLen)
            throw new InvalidDataException($"NBT byte array length out of range: {len}");
        var buf = ReadBytes(s, len);
        return new NbtByteArray { Value = buf };
    }

    private static NbtIntArray ReadIntArray(Stream s)
    {
        var len = ReadI32(s);
        if (len < 0 || len > MaxArrayLen)
            throw new InvalidDataException($"NBT int array length out of range: {len}");
        var arr = new int[len];
        for (var i = 0; i < len; i++) arr[i] = ReadI32(s);
        return new NbtIntArray { Value = arr };
    }

    private static NbtLongArray ReadLongArray(Stream s)
    {
        var len = ReadI32(s);
        if (len < 0 || len > MaxArrayLen)
            throw new InvalidDataException($"NBT long array length out of range: {len}");
        var arr = new long[len];
        for (var i = 0; i < len; i++) arr[i] = ReadI64(s);
        return new NbtLongArray { Value = arr };
    }

    // ── write ──
    private static void WriteNamed(Stream s, NbtTag t)
    {
        s.WriteByte(t.Id);
        WriteString(s, t.Name);
        WritePayload(s, t);
    }

    private static void WritePayload(Stream s, NbtTag t)
    {
        switch (t)
        {
            case NbtByte b: s.WriteByte((byte)b.Value); break;
            case NbtShort sh: WriteU16(s, (ushort)sh.Value); break;
            case NbtInt i: WriteI32(s, i.Value); break;
            case NbtLong l: WriteI64(s, l.Value); break;
            case NbtFloat f: WriteI32(s, BitConverter.SingleToInt32Bits(f.Value)); break;
            case NbtDouble d: WriteI64(s, BitConverter.DoubleToInt64Bits(d.Value)); break;
            case NbtByteArray ba: WriteI32(s, ba.Value.Length); s.Write(ba.Value, 0, ba.Value.Length); break;
            case NbtString str: WriteString(s, str.Value); break;
            case NbtList list:
                // 빈 리스트는 elementId 0(End) 가 표준. 비어있지 않으면 첫 item 의 실제 타입에서 도출
                // (stale ElementId 로 잘못된 NBT 를 쓰는 것 방지 — Codex). 혼합 타입 item 은 NBT 위반이므로 거부.
                var eid = list.Items.Count == 0 ? (byte)0 : list.Items[0].Id;
                for (var k = 1; k < list.Items.Count; k++)
                    if (list.Items[k].Id != eid)
                        throw new InvalidDataException("NBT list has mixed element types");
                s.WriteByte(eid);
                WriteI32(s, list.Items.Count);
                foreach (var it in list.Items) WritePayload(s, it);
                break;
            case NbtCompound c:
                foreach (var child in c.Children) WriteNamed(s, child);
                s.WriteByte(0); // End
                break;
            case NbtIntArray ia: WriteI32(s, ia.Value.Length); foreach (var v in ia.Value) WriteI32(s, v); break;
            case NbtLongArray la: WriteI32(s, la.Value.Length); foreach (var v in la.Value) WriteI64(s, v); break;
            default: throw new InvalidDataException($"Cannot write NBT tag {t.GetType().Name}");
        }
    }

    // ── primitives (big-endian) ──
    private static byte ReadId(Stream s) => ReadU8(s);

    private static byte ReadU8(Stream s)
    {
        var b = s.ReadByte();
        if (b < 0) throw new EndOfStreamException();
        return (byte)b;
    }

    private static byte[] ReadBytes(Stream s, int len)
    {
        if (len < 0) throw new InvalidDataException("Negative NBT length");
        var buf = new byte[len];
        var off = 0;
        while (off < len)
        {
            var n = s.Read(buf, off, len - off);
            if (n <= 0) throw new EndOfStreamException();
            off += n;
        }
        return buf;
    }

    private static ushort ReadU16(Stream s) { var b = ReadBytes(s, 2); return (ushort)((b[0] << 8) | b[1]); }
    private static int ReadI32(Stream s) { var b = ReadBytes(s, 4); return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]; }
    private static long ReadI64(Stream s)
    {
        var b = ReadBytes(s, 8);
        long v = 0;
        for (var i = 0; i < 8; i++) v = (v << 8) | b[i];
        return v;
    }

    private static string ReadString(Stream s)
    {
        var len = ReadU16(s);
        var b = ReadBytes(s, len);
        // NBT 는 modified-UTF8 이지만 BMP(서버명/IP/base64 아이콘)에서는 표준 UTF-8 과 동일.
        return Encoding.UTF8.GetString(b);
    }

    private static void WriteU16(Stream s, ushort v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)(v & 0xff)); }
    private static void WriteI32(Stream s, int v)
    {
        s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v);
    }
    private static void WriteI64(Stream s, long v)
    {
        for (var i = 7; i >= 0; i--) s.WriteByte((byte)(v >> (i * 8)));
    }

    private static void WriteString(Stream s, string v)
    {
        var b = Encoding.UTF8.GetBytes(v);
        if (b.Length > ushort.MaxValue) throw new InvalidDataException("NBT string too long");
        WriteU16(s, (ushort)b.Length);
        s.Write(b, 0, b.Length);
    }
}
