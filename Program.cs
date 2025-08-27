using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<BookingStore>();
var app = builder.Build();

// ─────────────────────────────── 前端頁面 ───────────────────────────────
app.MapGet("/", async context =>
{
var html = """
<!doctype html>
<html lang="zh-Hant">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>DSCI-Lab 線上預約 (7天 × 自訂時段)</title>
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
  <h1>DSCI-Lab 量測線上預約</h1>
  <div class="muted">時段：08~12、13~17、18~22、22以後。若時段已被預約會在選單中標註「已預約：姓名」。可用姓名＋星期＋時段取消自己的預約。</div>

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
  // data: { detail:[{slot,name|null}], takenDetail:[{slot,name}], available:[...]}
  // ▼ 下拉選單「永遠列出所有時段」，已預約的加註（已預約：姓名）
  slotEl.innerHTML = data.detail.map(x => {
    const note = x.name ? `（已預約：${x.name}）` : '';
    return `<option value="${x.slot}">${x.slot}${note}</option>`;
  }).join('');

  takenDayEl.innerHTML = data.takenDetail.length
