using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BookingStore>();
var app = builder.Build();

// ── 首頁（HTML） ─────────────────────────────────────────────────────────
app.MapGet("/", async context =>
{
    var html = """
<!doctype html>
<html lang="zh-Hant">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>DSCI-Lab 量測預約系統 </title>
<style>
  body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Noto Sans TC, sans-serif; padding:24px; max-width:760px; margin:auto; }
  h1 { font-size: 26px; margin-bottom: 12px; }
  form, .card { border:1px solid #e5e5e5; border-radius:12px; padding:16px; margin:12px 0; }
  label { display:block; margin:10px 0 6px; font-weight:600; }
  select, input[type=text] { width:100%; padding:10px; border-radius:10px; border:1px solid #ccc; }
  button { padding:10px 16px; border:0; border-radius:10px; cursor:pointer; }
  .primary { background:#111; color:#fff; }
  .danger  { background:#b00020; color:#fff; }
  .muted { color:#666; }
  .list { display:grid; gap:8px; }
  .success { background:#eaf7ea; border:1px solid #bbe6bb; padding:8px 12px; border-radius:8px; }
  .error { background:#fdecec; border:1px solid #f5b5b5; padding:8px 12px; border-radius:8px; }
  .pill { display:inline-block; border:1px solid #ddd; border-radius:999px; padding:2px 10px; margin:2px 6px 2px 0; }
  .row { display:flex; gap:8px; flex-wrap:wrap; }
</style>
</head>
<body>
  <h1>DSCI-Lab 量測預約系統</h1>
  <div class="muted">時段：08~12、13~17、18~22、22以後。已預約的時段會在選單標註「已預約：姓名」。可用姓名＋星期＋時段取消。</div>

  <form id="booking-form">
    <label for="day">選擇日期（星期）</label>
    <select id="day" required></select>

    <label for="slot">選擇時段</label>
    <select id="slot" required></select>
    <div id="takenDay" class="muted" style="margin-top:6px;"></div>

    <label for="name">姓名</label>
    <input id="name" type="text" required placeholder="王小明" />

    <div class="row" style="margin-top:12px;">
      <button class="primary" type="submit">送出預約</button>
      <button type="button" id="refresh">重新載入</button>
      <button type="button" id="cancel" class="danger">取消我的預約</button>
    </div>
    <div id="msg" style="margin-top:12px;"></div>
  </form>

  <div class="card">
    <strong>全週總覽</strong>
    <div id="weekly" class="list" style="margin-top:8px;"></div>
  </div>

<script>
const dayEl = document.getElementById('day');
const slotEl = document.getElementById('slot');
const msgEl = document.getElementById('msg');
const takenDayEl = document.getElementById('takenDay');
const weeklyEl = document.getElementById('weekly');
dayEl.addEventListener('change', async () => {
  await loadSlots();   // 切換星期時，自動更新對應的時段與已預約者
});
function flash(type, text){ msgEl.innerHTML = `<div class="${type}">${text}</div>`; }

async function loadDays() {
  const res = await fetch('/api/days');
  const days = await res.json();
  dayEl.innerHTML = days.map(d => `<option value="${d}">${d}</option>`).join('');
}

async function loadSlots() {
  const day = dayEl.value;
  const res = await fetch('/api/slots?day=' + encodeURIComponent(day));
  const data = await res.json();
  // data.detail = [{slot,name|null}], data.takenDetail = [{slot,name}]
  slotEl.innerHTML = data.detail.map(x => {
    const note = x.name ? `（已預約：${x.name}）` : '';
    return `<option value="${x.slot}">${x.slot}${note}</option>`;
  }).join('');
  takenDayEl.innerHTML = data.takenDetail.length
    ? '該天已被預約：' + data.takenDetail.map(x => `<span class="pill">${x.slot}（${x.name}）</span>`).join(' ')
    : '該天尚無已被預約的時段';
}

async function loadWeekly() {
  const res = await fetch('/api/weekly');
  const items = await res.json();
  weeklyEl.innerHTML = items.map(d => {
    const cells = d.slots.map(s => s.name
      ? `<span class="pill">${s.slot}（${s.name}）</span>`
      : `<span class="pill">${s.slot}（空）</span>`).join(' ');
    return `<div>${d.day}：${cells}</div>`;
  }).join('');
}

document.getElementById('refresh').addEventListener('click', async () => { await loadSlots(); });

document.getElementById('booking-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const body = { day: dayEl.value, slot: slotEl.value, name: document.getElementById('name').value.trim() };
  if(!body.name){ flash('error','請先輸入姓名'); return; }
  const res = await fetch('/api/book', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body) });
  const data = await res.json();
  if (res.ok) { flash('success', `✅ 預約成功：${data.name} — ${data.day} ${data.slot}`); await loadSlots(); await loadWeekly(); }
  else { flash('error', `❌ ${data.error || '發生錯誤'}`); await loadSlots(); await loadWeekly(); }
});

document.getElementById('cancel').addEventListener('click', async () => {
  const body = { day: dayEl.value, slot: slotEl.value, name: document.getElementById('name').value.trim() };
  if(!body.name){ flash('error','請先輸入姓名'); return; }
  const res = await fetch('/api/cancel', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body) });
  const data = await res.json();
  if (res.ok) { flash('success', `🗑️ 已取消：${data.name} — ${data.day} ${data.slot}`); await loadSlots(); await loadWeekly(); }
  else { flash('error', `❌ ${data.error || '取消失敗'}`); }
});

(async () => { await loadDays(); await loadSlots(); await loadWeekly(); })();
</script>
</body>
</html>
"""; // ✅ 這行一定要有（結束多行字串，再加分號）
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(html);
}); // ✅ 這行一定要有（收掉 MapGet 的大括號與括號）

// ── 常數與 API ───────────────────────────────────────────────────────────
string[] DAYS = new[] { "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
string[] SLOTS = new[] { "08~12", "13~17", "18~22", "22以後" };

app.MapGet("/api/days", () => DAYS);

app.MapGet("/api/slots", (string day, BookingStore store) =>
{
    store.MaybePurge(TimeSpan.FromDays(7));
    var detail = store.SLOTS.Select(s =>
    {
        var name = store.QueryByDay(day).FirstOrDefault(b => b.Slot == s)?.Name;
        return new { slot = s, name = name };
    }).ToList();

    var takenDetail = detail.Where(x => x.name != null).ToList();
    var available = detail.Where(x => x.name == null).Select(x => x.slot).ToList();

    return Results.Ok(new { detail, takenDetail, available });
});

app.MapPost("/api/book", (BookingDto dto, BookingStore store) =>
{
    store.MaybePurge(TimeSpan.FromDays(7));
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "姓名必填" });
    if (string.IsNullOrWhiteSpace(dto.Day) || string.IsNullOrWhiteSpace(dto.Slot)) return Results.BadRequest(new { error = "請選擇星期與時段" });
    if (!store.SLOTS.Contains(dto.Slot)) return Results.BadRequest(new { error = "時段不在可預約清單中" });
    if (!store.DAYS.Contains(dto.Day)) return Results.BadRequest(new { error = "星期不在可預約清單中" });
    if (store.Exists(dto.Day, dto.Slot)) return Results.BadRequest(new { error = $"該時段已有人，請換一個：{dto.Day} {dto.Slot}" });

    var booking = new Booking { Id = Guid.NewGuid().ToString("N"), Day = dto.Day, Slot = dto.Slot, Name = dto.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
    store.Add(booking);
    return Results.Ok(booking);
});

app.MapPost("/api/cancel", (BookingDto dto, BookingStore store) =>
{
    store.MaybePurge(TimeSpan.FromDays(7));
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "姓名必填" });
    if (string.IsNullOrWhiteSpace(dto.Day) || string.IsNullOrWhiteSpace(dto.Slot)) return Results.BadRequest(new { error = "請選擇星期與時段" });

    var ok = store.Remove(dto.Day, dto.Slot, dto.Name);
    if (!ok) return Results.BadRequest(new { error = "找不到對應的預約，或姓名不符" });
    return Results.Ok(new { day = dto.Day, slot = dto.Slot, name = dto.Name.Trim() });
});

app.MapGet("/api/weekly", (BookingStore store) =>
{
    store.MaybePurge(TimeSpan.FromDays(7));
    return store.DAYS.Select(d => new {
        day = d,
        slots = store.SLOTS.Select(s =>
        {
            var name = store.QueryByDay(d).FirstOrDefault(b => b.Slot == s)?.Name;
            return new { slot = s, name = name };
        }).ToList()
    }).ToList();
});

app.Run(); // ✅ 這行也要在最後

// ── 型別與儲存 ───────────────────────────────────────────────────────────
record Booking
{
    public string Id { get; set; } = default!;
    public string Day { get; set; } = default!;
    public string Slot { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}

record BookingDto(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("slot")] string Slot,
    [property: JsonPropertyName("name")] string Name);

class BookingStore
{
    private readonly string _path;
    private readonly object _lock = new();
    private List<Booking> _cache = new();
    private DateTimeOffset _lastPurge = DateTimeOffset.MinValue;

    public string[] DAYS { get; }
    public string[] SLOTS { get; }

    public BookingStore(IHostEnvironment env)
    {
        DAYS = new[] { "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
        SLOTS = new[] { "08~12", "13~17", "18~22", "22以後" };

        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "bookings.json");

        if (File.Exists(_path))
        {
            try
            {
                var json = File.ReadAllText(_path);
                var data = JsonSerializer.Deserialize<List<Booking>>(json);
                if (data != null) _cache = data;
            }
            catch { _cache = new List<Booking>(); }
        }

        PurgeOlderThan(TimeSpan.FromDays(7)); // 啟動先清一次
    }

    public IEnumerable<Booking> QueryByDay(string day)
    {
        lock (_lock) return _cache.Where(b => b.Day == day).ToList();
    }

    public bool Exists(string day, string slot)
    {
        lock (_lock) return _cache.Any(b => b.Day == day && b.Slot == slot);
    }

    public void Add(Booking b)
    {
        lock (_lock)
        {
            _cache.Add(b);
            Save();
        }
    }

    public bool Remove(string day, string slot, string name)
    {
        name = name.Trim();
        lock (_lock)
        {
            var idx = _cache.FindIndex(b =>
                b.Day == day &&
                b.Slot == slot &&
                string.Equals(b.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _cache.RemoveAt(idx);
                Save();
                return true;
            }
            return false;
        }
    }

    public void MaybePurge(TimeSpan olderThan)
    {
        if (DateTimeOffset.UtcNow - _lastPurge < TimeSpan.FromHours(1)) return;
        PurgeOlderThan(olderThan);
    }

    public void PurgeOlderThan(TimeSpan olderThan)
    {
        lock (_lock)
        {
            var cutoff = DateTimeOffset.UtcNow - olderThan;
            int before = _cache.Count;
            _cache = _cache.Where(b => b.CreatedAt >= cutoff).ToList();
            if (_cache.Count != before) Save();
            _lastPurge = DateTimeOffset.UtcNow;
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
