using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// Minecraft Server List Ping(1.7+ handshake+status). 단순 TCP 연결보다 강해 실제 MC 서버 여부를 확인(P1-10).
// 비-MC 프로세스가 같은 포트를 점유해도 status 응답이 없으면 false → localhost probe false-positive 제거.
public static class ServerPing
{
    // host:port 가 응답하는 MC 서버면 true. 타임아웃/비-MC/오류 = false. 사용자 취소 = throw.
    public static async Task<bool> IsServerUpAsync(string host, int port, CancellationToken ct, int timeoutMs = 2000)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            using var stream = client.GetStream();

            // (1) Handshake — next state = 1(status)
            using (var hs = new MemoryStream())
            {
                WriteVarInt(hs, 0x00);                 // packet id
                WriteVarInt(hs, -1);                   // protocol version(임의)
                WriteString(hs, host);
                hs.WriteByte((byte)(port >> 8));       // port: ushort big-endian
                hs.WriteByte((byte)(port & 0xFF));
                WriteVarInt(hs, 1);                     // next state = status
                await WritePacketAsync(stream, hs.ToArray(), cts.Token).ConfigureAwait(false);
            }
            // (2) Status request(빈 바디, id 0x00)
            using (var req = new MemoryStream())
            {
                WriteVarInt(req, 0x00);
                await WritePacketAsync(stream, req.ToArray(), cts.Token).ConfigureAwait(false);
            }
            // (3) Status response: length(varint) → packet id(varint, 0x00) → string len(varint) → UTF-8 JSON.
            //     JSON 이 '{' 로 시작하는지까지 확인해야 임의 0x00 바이트를 에코하는 비-MC 서버를 배제(P1-10, Codex).
            var len = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
            if (len <= 0 || len > (1 << 20)) return false;
            var pid = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
            if (pid != 0x00) return false;
            var jsonLen = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
            if (jsonLen <= 0 || jsonLen > (1 << 20)) return false;
            // 선두 비공백 바이트가 '{' 면 MC status JSON 으로 간주.
            var first = await ReadFirstNonWhitespaceAsync(stream, cts.Token).ConfigureAwait(false);
            return first == (byte)'{';
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return false; }
    }

    // 메인 화면 상태 pill 용 — status JSON 전체를 읽어 players/MOTD 를 파싱. 실패/비-MC = null. 취소 = throw.
    public static async Task<ServerStatus?> QueryStatusAsync(string host, int port, CancellationToken ct, int timeoutMs = 2500)
    {
        try
        {
            var json = await ReadStatusJsonAsync(host, port, ct, timeoutMs).ConfigureAwait(false);
            return json is null ? null : ServerStatus.Parse(json);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return null; }
    }

    // handshake + status request 후 응답 JSON 문자열 전체를 읽어 반환(없으면 null).
    private static async Task<string?> ReadStatusJsonAsync(string host, int port, CancellationToken ct, int timeoutMs)
    {
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
        using var stream = client.GetStream();

        using (var hs = new MemoryStream())
        {
            WriteVarInt(hs, 0x00);
            WriteVarInt(hs, -1);
            WriteString(hs, host);
            hs.WriteByte((byte)(port >> 8));
            hs.WriteByte((byte)(port & 0xFF));
            WriteVarInt(hs, 1);
            await WritePacketAsync(stream, hs.ToArray(), cts.Token).ConfigureAwait(false);
        }
        using (var req = new MemoryStream())
        {
            WriteVarInt(req, 0x00);
            await WritePacketAsync(stream, req.ToArray(), cts.Token).ConfigureAwait(false);
        }

        var len = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
        if (len <= 0 || len > (1 << 20)) return null;
        var pid = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
        if (pid != 0x00) return null;
        var jsonLen = await ReadVarIntAsync(stream, cts.Token).ConfigureAwait(false);
        if (jsonLen <= 0 || jsonLen > (1 << 20)) return null;

        var buf = await ReadExactAsync(stream, jsonLen, cts.Token).ConfigureAwait(false);
        return buf is null ? null : Encoding.UTF8.GetString(buf);
    }

    // 정확히 count 바이트를 읽는다(부분 읽기 누적). 조기 EOF = null.
    private static async Task<byte[]?> ReadExactAsync(Stream s, int count, CancellationToken ct)
    {
        var buf = new byte[count];
        var read = 0;
        while (read < count)
        {
            var n = await s.ReadAsync(buf.AsMemory(read, count - read), ct).ConfigureAwait(false);
            if (n == 0) return null; // 조기 EOF
            read += n;
        }
        return buf;
    }

    // JSON 본문 선두의 비공백 바이트 1개를 읽는다(공백/개행 스킵, 최대 16바이트). EOF/초과 시 0.
    private static async Task<byte> ReadFirstNonWhitespaceAsync(Stream s, CancellationToken ct)
    {
        var one = new byte[1];
        for (var i = 0; i < 16; i++)
        {
            var n = await s.ReadAsync(one.AsMemory(0, 1), ct).ConfigureAwait(false);
            if (n == 0) return 0;
            var b = one[0];
            if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
                return b;
        }
        return 0;
    }

    private static async Task WritePacketAsync(Stream s, byte[] data, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        WriteVarInt(ms, data.Length);
        ms.Write(data, 0, data.Length);
        var buf = ms.ToArray();
        await s.WriteAsync(buf, ct).ConfigureAwait(false);
        await s.FlushAsync(ct).ConfigureAwait(false);
    }

    private static void WriteVarInt(Stream s, int value)
    {
        var v = (uint)value;
        do
        {
            var b = (byte)(v & 0x7F);
            v >>= 7;
            if (v != 0) b |= 0x80;
            s.WriteByte(b);
        } while (v != 0);
    }

    private static void WriteString(Stream s, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        WriteVarInt(s, bytes.Length);
        s.Write(bytes, 0, bytes.Length);
    }

    private static async Task<int> ReadVarIntAsync(Stream s, CancellationToken ct)
    {
        int result = 0, shift = 0;
        var one = new byte[1];
        while (shift < 35)
        {
            var n = await s.ReadAsync(one.AsMemory(0, 1), ct).ConfigureAwait(false);
            if (n == 0) return -1; // EOF
            result |= (one[0] & 0x7F) << shift;
            if ((one[0] & 0x80) == 0) return result;
            shift += 7;
        }
        return -1; // varint 과대
    }
}
