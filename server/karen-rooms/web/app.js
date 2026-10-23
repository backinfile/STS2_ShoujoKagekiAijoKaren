"use strict";
const $ = id => document.getElementById(id);
let timer, busy = false, failures = 0, lastSuccess = 0, rows = new Map();
const duration = seconds => seconds < 60 ? `${Math.max(0, Math.floor(seconds))} 秒` : seconds < 3600 ? `${Math.floor(seconds / 60)} 分钟` : seconds < 86400 ? `${Math.floor(seconds / 3600)} 小时` : `${Math.floor(seconds / 86400)} 天`;
function state(kind, text) { $("status").className = `status ${kind}`; $("status-text").textContent = text; }
function render(data) {
  $("players").textContent = data.players;
  $("player-cap").textContent = `最多 ${data.max_players} 人 · 每房间 4 人`;
  $("room-count").textContent = data.rooms.length;
  $("room-cap").textContent = `最多 ${data.max_rooms} 个房间`;
  $("count-label").textContent = `/ ${data.rooms.length}`;
  $("uptime").textContent = duration(data.uptime_seconds);
  const alive = new Set();
  let position = 0;
  for (const room of data.rooms) {
    alive.add(room.id);
    let row = rows.get(room.id);
    if (!row) {
      row = document.createElement("tr");
      for (let i = 0; i < 5; i++) row.append(document.createElement("td"));
      row.cells[0].className = "room-code";
      row.cells[0].append(document.createElement("span"), document.createElement("b"));
      row.cells[1].className = "occupancy";
      row.cells[1].append(document.createElement("span"), document.createElement("small"));
      row.addEventListener("animationend", () => row.classList.remove("changed"));
      rows.set(room.id, row); row.classList.add("changed");
    }
    const code = String(room.display_code);
    row.cells[0].children[0].textContent = code.slice(0, -2);
    row.cells[0].children[1].textContent = code.slice(-2);
    row.cells[0].setAttribute("aria-label", `房间号末两位 ${code.slice(-2)}`);
    const signature = `${room.players}/${room.started}`;
    if (row.dataset.signature && row.dataset.signature !== signature) {
      row.classList.remove("changed"); requestAnimationFrame(() => row.classList.add("changed"));
    }
    row.dataset.signature = signature;
    row.cells[1].children[0].textContent = room.players;
    row.cells[1].children[1].textContent = ` / ${room.capacity}`;
    row.cells[2].className = `room-state ${room.started ? "" : "waiting"}`;
    row.cells[2].textContent = room.started ? "进行中" : "等待中";
    row.cells[3].textContent = duration(room.age_seconds);
    row.cells[4].textContent = room.idle_seconds < 5 ? "刚刚" : `${duration(room.idle_seconds)}前`;
    const list = $("room-list");
    if (list.children[position] !== row) list.insertBefore(row, list.children[position] || null);
    position++;
  }
  for (const [id, row] of rows) if (!alive.has(id)) { row.remove(); rows.delete(id); }
  $("empty").hidden = data.rooms.length !== 0;
  const monitor = data.monitor;
  const fresh = monitor && data.server_time - monitor.checked_at < 180;
  $("monitor").textContent = !fresh ? "健康检查：暂无最新结果" : monitor.ok ? "健康检查正常 · 每分钟检查" : "健康检查发现异常";
  state(fresh && !monitor.ok ? "stale" : "online", fresh && !monitor.ok ? "服务在线 · 监控告警" : "服务在线");
}
async function refresh() {
  clearTimeout(timer);
  if (busy || document.hidden) return;
  busy = true; $("refresh").disabled = true;
  const abort = new AbortController(); const timeout = setTimeout(() => abort.abort(), 6000);
  try {
    const response = await fetch("api/status", { cache: "no-store", signal: abort.signal });
    if (!response.ok) throw new Error("unavailable");
    const data = await response.json();
    if (data.service !== "karen-rooms" || !Array.isArray(data.rooms)) throw new Error("invalid");
    render(data); lastSuccess = Date.now(); failures = 0;
    $("updated").textContent = `更新于 ${new Date(lastSuccess).toLocaleTimeString("zh-CN", {hour12: false})}`;
    $("notice").hidden = true;
  } catch {
    failures++;
    state(lastSuccess ? "stale" : "offline", lastSuccess ? "更新中断 · 显示上次结果" : "暂时无法连接");
    $("notice").hidden = false;
    $("notice").textContent = lastSuccess ? "当前信息可能已过期，正在自动重试。" : "暂时无法获取状态，正在自动重试。";
  } finally {
    clearTimeout(timeout); busy = false; $("refresh").disabled = false;
    if (!document.hidden) timer = setTimeout(refresh, Math.min(30000, 2000 * 2 ** Math.min(failures, 4)));
  }
}
$("refresh").addEventListener("click", refresh);
document.addEventListener("visibilitychange", () => { clearTimeout(timer); if (!document.hidden) refresh(); });
refresh();
