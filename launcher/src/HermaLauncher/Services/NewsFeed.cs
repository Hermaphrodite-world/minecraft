using System.Collections.Generic;
using System.Text.Json;

namespace HermaLauncher.Services;

// 운영자 원격 공지/점검(news.json) 모델 + 파싱(순수 — 단위 테스트 가능).
// 스키마 예:
//   { "maintenance": { "active": true, "message": "22시까지 점검" },
//     "items": [ { "id": "2026-06-15-1", "title": "새 모드 추가", "body": "...", "urgent": false } ] }
public sealed record NewsItem(string Id, string Title, string? Body, bool Urgent);

public sealed record MaintenanceInfo(bool Active, string? Message);

public sealed record NewsFeed(MaintenanceInfo? Maintenance, IReadOnlyList<NewsItem> Items)
{
    public NewsItem? Latest => Items.Count > 0 ? Items[0] : null;

    // news.json → NewsFeed. 형식 불일치/빈 입력 = null. 필수(id·title) 없는 item 은 건너뜀(부분 손상 내성).
    public static NewsFeed? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            MaintenanceInfo? maint = null;
            if (root.TryGetProperty("maintenance", out var m) && m.ValueKind == JsonValueKind.Object)
            {
                var active = m.TryGetProperty("active", out var a) && a.ValueKind == JsonValueKind.True;
                var msg = m.TryGetProperty("message", out var mm) && mm.ValueKind == JsonValueKind.String
                    ? mm.GetString() : null;
                maint = new MaintenanceInfo(active, msg);
            }

            var items = new List<NewsItem>();
            if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in arr.EnumerateArray())
                {
                    if (it.ValueKind != JsonValueKind.Object) continue;
                    var id = it.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null;
                    var title = it.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title)) continue; // 필수 누락 → 건너뜀
                    var body = it.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() : null;
                    var urgent = it.TryGetProperty("urgent", out var u) && u.ValueKind == JsonValueKind.True;
                    items.Add(new NewsItem(id!, title!, body, urgent));
                }
            }
            return new NewsFeed(maint, items);
        }
        catch
        {
            return null;
        }
    }
}
