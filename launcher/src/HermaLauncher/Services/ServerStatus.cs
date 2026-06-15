using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace HermaLauncher.Services;

// Minecraft Server List Ping status JSON 의 파싱 결과(메인 화면 상태 pill 용). 모두 best-effort.
// Sample = players.sample[].name (접속자 닉네임 일부 — 서버가 채울 때만, 다수 시 잘릴 수 있음). 빈 리스트 가능(null 아님).
public sealed record ServerStatus(int? Players, int? MaxPlayers, string? Motd, IReadOnlyList<string> Sample)
{
    // status JSON → ServerStatus. 형식 불일치/빈 입력 = null(억지 추정 금지).
    public static ServerStatus? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            int? online = null, max = null;
            var sample = new List<string>();
            if (root.TryGetProperty("players", out var players) && players.ValueKind == JsonValueKind.Object)
            {
                if (players.TryGetProperty("online", out var o) && o.TryGetInt32(out var ov)) online = ov;
                if (players.TryGetProperty("max", out var m) && m.TryGetInt32(out var mv)) max = mv;
                if (players.TryGetProperty("sample", out var s) && s.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in s.EnumerateArray())
                    {
                        if (p.ValueKind == JsonValueKind.Object &&
                            p.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                        {
                            var nm = n.GetString();
                            if (!string.IsNullOrWhiteSpace(nm)) sample.Add(nm!.Trim());
                        }
                    }
                }
            }

            string? motd = root.TryGetProperty("description", out var d) ? ExtractMotd(d) : null;
            return new ServerStatus(online, max, motd, sample);
        }
        catch
        {
            return null;
        }
    }

    // description 은 문자열, {"text":...}, 또는 {"text":..,"extra":[...]} 형태일 수 있다(MC 텍스트 컴포넌트).
    private static string? ExtractMotd(JsonElement d)
    {
        if (d.ValueKind == JsonValueKind.String) return Clean(d.GetString());
        if (d.ValueKind != JsonValueKind.Object) return null;

        var sb = new StringBuilder();
        if (d.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
            sb.Append(t.GetString());
        if (d.TryGetProperty("extra", out var ex) && ex.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in ex.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.String)
                    sb.Append(part.GetString());
                else if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("text", out var pt) && pt.ValueKind == JsonValueKind.String)
                    sb.Append(pt.GetString());
            }
        }
        return Clean(sb.ToString());
    }

    private static string? Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var trimmed = s.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
