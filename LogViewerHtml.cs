namespace LogBattery;

internal static class LogViewerHtml
{
    // JS derives the base path from window.location.pathname — no server-side templating needed.
    internal static string GetHtml() => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Log Viewer</title>
            <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0d1117; color: #c9d1d9; }

                .header { background: #161b22; padding: 12px 20px; border-bottom: 1px solid #30363d; display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
                .header h1 { font-size: 18px; font-weight: 600; color: #58a6ff; white-space: nowrap; }
                .controls { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; flex: 1; }
                select, input[type=text], input[type=date] { padding: 6px 10px; border-radius: 6px; border: 1px solid #30363d; background: #21262d; color: #c9d1d9; font-size: 13px; }
                input[type=date]::-webkit-calendar-picker-indicator { filter: invert(0.7); }
                button { padding: 6px 12px; border-radius: 6px; border: 1px solid #30363d; background: #21262d; color: #c9d1d9; font-size: 13px; cursor: pointer; font-weight: 500; white-space: nowrap; }
                button.primary { background: #238636; border-color: #238636; }
                button.primary:hover { background: #2ea043; }
                button.active { background: #1f6feb; border-color: #1f6feb; }
                button:hover { border-color: #8b949e; }

                .stats-bar { background: #161b22; padding: 8px 20px; border-bottom: 1px solid #21262d; display: flex; gap: 16px; align-items: center; font-size: 13px; flex-wrap: wrap; }
                .stat { display: flex; align-items: center; gap: 6px; }
                .stat-badge { padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 700; }
                .stat-inf .stat-badge { background: #0d3321; color: #3fb950; }
                .stat-wrn .stat-badge { background: #3d2e00; color: #d29922; }
                .stat-err .stat-badge { background: #490202; color: #f85149; }
                .stat-ftl .stat-badge { background: #3d0000; color: #ff7b72; }
                .stat-count { color: #8b949e; font-family: 'SF Mono', Consolas, monospace; }
                .total-count { margin-left: auto; color: #8b949e; }

                .container { overflow-x: auto; }
                table { width: 100%; border-collapse: collapse; font-size: 13px; }
                th { text-align: left; padding: 10px 12px; background: #161b22; color: #8b949e; position: sticky; top: 0; border-bottom: 1px solid #30363d; font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; white-space: nowrap; z-index: 1; }
                td { padding: 7px 12px; border-bottom: 1px solid #1c2128; vertical-align: middle; }
                tr.log-row:hover > td { background: #161b22; cursor: pointer; }
                tr.has-exception > td:first-child { border-left: 3px solid #f85149; padding-left: 9px; }

                .time { color: #8b949e; white-space: nowrap; font-family: 'SF Mono', Consolas, monospace; font-size: 12px; }
                .badge { display: inline-block; padding: 2px 7px; border-radius: 4px; font-size: 11px; font-weight: 600; font-family: 'SF Mono', Consolas, monospace; }
                .level-Information { background: #0d3321; color: #3fb950; }
                .level-Warning     { background: #3d2e00; color: #d29922; }
                .level-Error       { background: #490202; color: #f85149; }
                .level-Fatal       { background: #3d0000; color: #ff7b72; }
                .level-Debug       { background: #1c2333; color: #8b949e; }

                .method-get    { background: #0c2d6b; color: #79c0ff; }
                .method-post   { background: #0d3321; color: #3fb950; }
                .method-put    { background: #3d2e00; color: #d29922; }
                .method-delete { background: #490202; color: #f85149; }
                .method-patch  { background: #2d1a5e; color: #d2a8ff; }
                .status-2xx { background: #0d3321; color: #3fb950; }
                .status-3xx { background: #0c2d6b; color: #79c0ff; }
                .status-4xx { background: #3d2e00; color: #d29922; }
                .status-5xx { background: #490202; color: #f85149; }
                .method-status { display: flex; gap: 4px; align-items: center; white-space: nowrap; }

                .path-cell   { font-family: 'SF Mono', Consolas, monospace; font-size: 12px; word-break: break-word; }
                .app-message { color: #8b949e; }
                .msg-preview { color: #6e7681; font-size: 11px; margin-top: 3px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 600px; }
                .duration            { color: #8b949e; font-family: 'SF Mono', Consolas, monospace; font-size: 12px; white-space: nowrap; }
                .duration.slow       { color: #d29922; }
                .duration.very-slow  { color: #f85149; }

                .expand-btn { background: none; border: 1px solid #30363d; padding: 2px 8px; border-radius: 4px; color: #8b949e; font-size: 11px; cursor: pointer; }
                .expand-btn:hover { border-color: #8b949e; color: #c9d1d9; }

                .detail-row > td { background: #0a0d12; padding: 0; }
                .detail-panel { padding: 12px 16px 16px 52px; border-top: 1px solid #21262d; }
                .detail-props { display: flex; flex-direction: column; gap: 3px; margin-bottom: 12px; }
                .detail-prop { background: #161b22; border: 1px solid #30363d; border-radius: 6px; padding: 7px 12px; display: flex; gap: 16px; min-width: 0; }
                .detail-prop.is-long { cursor: pointer; }
                .detail-prop.is-long:hover { border-color: #484f58; }
                .detail-prop-label { color: #8b949e; font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; white-space: nowrap; width: 140px; flex-shrink: 0; padding-top: 1px; }
                .detail-prop-value { color: #c9d1d9; font-family: 'SF Mono', Consolas, monospace; font-size: 12px; word-break: break-all; white-space: pre-wrap; flex: 1; min-width: 0; overflow: hidden; max-height: 4.2em; line-height: 1.4; }
                .detail-prop-value.expanded { max-height: none; overflow-y: visible; }
                .detail-prop-toggle { color: #6e7681; font-size: 10px; white-space: nowrap; flex-shrink: 0; align-self: flex-end; padding-bottom: 1px; user-select: none; }
                .detail-section { margin-top: 10px; }
                .detail-section-title { color: #8b949e; font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 6px; }
                .detail-message { background: #161b22; border: 1px solid #30363d; border-radius: 6px; padding: 10px 12px; font-family: 'SF Mono', Consolas, monospace; font-size: 12px; color: #c9d1d9; word-break: break-word; white-space: pre-wrap; }
                .detail-exception { background: #1a0000; border: 1px solid #490202; border-radius: 6px; padding: 10px 12px; font-family: 'SF Mono', Consolas, monospace; font-size: 11px; color: #f85149; word-break: break-word; white-space: pre-wrap; max-height: 300px; overflow-y: auto; }

                .timeline-title { color: #8b949e; font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; margin: 12px 0 6px; display: flex; align-items: center; gap: 8px; }
                .sub-table { width: 100%; border-collapse: collapse; border: 1px solid #30363d; border-radius: 6px; overflow: hidden; font-size: 12px; }
                .sub-table th { text-align: left; padding: 6px 10px; background: #0a0d12; color: #8b949e; font-size: 10px; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #30363d; white-space: nowrap; }
                .sub-table td { padding: 5px 10px; border-bottom: 1px solid #1c2128; vertical-align: middle; }
                .sub-table tr:last-child td, .sub-table tr:nth-last-child(2):has(+ tr[style*="display:none"]) td { border-bottom: none; }
                .sub-row { cursor: pointer; }
                .sub-row:hover > td { background: #161b22; }
                .sub-row.has-exception > td:first-child { border-left: 3px solid #f85149; padding-left: 7px; }
                .sub-table .time { font-size: 11px; color: #8b949e; white-space: nowrap; font-family: 'SF Mono', Consolas, monospace; }
                .sub-detail-row > td { background: #080b0f; padding: 0; border-bottom: 1px solid #30363d; }
                .sub-detail-row .detail-panel { padding: 10px 12px 12px 20px; }

                .empty { text-align: center; padding: 48px; color: #8b949e; }

                .pagination-bar { background: #161b22; padding: 8px 20px; border-bottom: 1px solid #21262d; display: flex; gap: 8px; align-items: center; font-size: 13px; }
                .pagination-bar button { padding: 4px 10px; border-radius: 4px; border: 1px solid #30363d; background: #21262d; color: #c9d1d9; font-size: 14px; cursor: pointer; min-width: 32px; }
                .pagination-bar button:hover:not(:disabled) { border-color: #8b949e; }
                .pagination-bar button:disabled { opacity: 0.4; cursor: default; }
                .pagination-bar #pageInfo { color: #8b949e; font-family: 'SF Mono', Consolas, monospace; font-size: 12px; min-width: 120px; text-align: center; }
                .pagination-bar select { padding: 4px 8px; border-radius: 4px; border: 1px solid #30363d; background: #21262d; color: #c9d1d9; font-size: 12px; }
            </style>
        </head>
        <body>
            <div class="header">
                <h1>Log Viewer</h1>
                <div class="controls">
                    <select id="fileSelect" onchange="loadLogs(1)"></select>
                    <select id="levelFilter" onchange="loadLogs(1)">
                        <option value="">All Levels</option>
                        <option value="Information">Info</option>
                        <option value="Warning">Warning</option>
                        <option value="Error">Error</option>
                        <option value="Fatal">Fatal</option>
                    </select>
                    <input type="text"  id="searchInput" placeholder="Search..." style="width:150px;">
                    <input type="date"  id="dateFrom" onchange="applyClientFilters()">
                    <input type="date"  id="dateTo"   onchange="applyClientFilters()">
                    <button class="primary" onclick="loadLogs()">Refresh</button>
                    <button id="autoBtn" onclick="toggleAuto()">Auto: OFF</button>
                </div>
            </div>
            <div class="stats-bar">
                <div class="stat stat-inf"><span class="stat-badge">INF</span><span class="stat-count" id="cntInf">0</span></div>
                <div class="stat stat-wrn"><span class="stat-badge">WRN</span><span class="stat-count" id="cntWrn">0</span></div>
                <div class="stat stat-err"><span class="stat-badge">ERR</span><span class="stat-count" id="cntErr">0</span></div>
                <div class="stat stat-ftl"><span class="stat-badge">FTL</span><span class="stat-count" id="cntFtl">0</span></div>
                <span class="total-count" id="totalCount">0 entries</span>
            </div>
            <div class="pagination-bar" id="paginationBar">
                <button id="btnFirst" onclick="goToPage(1)">&#171;</button>
                <button id="btnPrev"  onclick="goToPage(currentPage-1)">&#8249;</button>
                <span id="pageInfo">Page 1 of 1</span>
                <button id="btnNext"  onclick="goToPage(currentPage+1)">&#8250;</button>
                <button id="btnLast"  onclick="goToPage(totalPages)">&#187;</button>
                <select id="pageSizeSelect" onchange="changePageSize()">
                    <option value="50">50 / page</option>
                    <option value="100" selected>100 / page</option>
                    <option value="200">200 / page</option>
                    <option value="500">500 / page</option>
                </select>
            </div>
            <div class="container">
                <table>
                    <thead>
                        <tr>
                            <th style="width:165px">Timestamp</th>
                            <th style="width:90px">Level</th>
                            <th style="width:130px">Method / Status</th>
                            <th>Path / Message</th>
                            <th style="width:90px">Duration</th>
                            <th style="width:44px"></th>
                        </tr>
                    </thead>
                    <tbody id="logBody"></tbody>
                </table>
                <div class="empty" id="empty" style="display:none;">No log entries found</div>
            </div>
            <script>
                // Derive API base from current page URL — works regardless of mount path.
                const BASE = window.location.pathname.replace(/\/$/, '');

                let allLogs = [], traceMap = new Map(), autoInterval = null, searchTimeout = null;
                let currentPage = 1, totalPages = 1, currentPageSize = 100;

                async function loadFiles() {
                    const res   = await fetch(BASE + '/api/files');
                    const files = await res.json();
                    const sel   = document.getElementById('fileSelect');
                    sel.innerHTML = files.map((f, i) =>
                        `<option value="${f.name}" ${i===0?'selected':''}>${f.date} (${(f.size/1024).toFixed(1)} KB)</option>`
                    ).join('') || '<option>No logs</option>';
                }

                async function loadLogs(page) {
                    if (page != null) currentPage = page;
                    const params = new URLSearchParams({
                        file:     document.getElementById('fileSelect').value  || '',
                        level:    document.getElementById('levelFilter').value || '',
                        search:   document.getElementById('searchInput').value || '',
                        page:     String(currentPage),
                        pageSize: String(currentPageSize)
                    });
                    try {
                        const data = await (await fetch(BASE + '/api/entries?' + params)).json();
                        allLogs     = data.entries || [];
                        currentPage = data.page    || 1;
                        totalPages  = data.totalPages || 1;
                        updatePagination(data.totalCount || 0);
                    } catch { allLogs = []; }
                    buildTraceMap();
                    applyClientFilters();
                }

                function goToPage(p) {
                    if (p < 1 || p > totalPages) return;
                    loadLogs(p);
                }

                function changePageSize() {
                    currentPageSize = parseInt(document.getElementById('pageSizeSelect').value) || 100;
                    currentPage = 1;
                    loadLogs(1);
                }

                function updatePagination(totalCount) {
                    document.getElementById('pageInfo').textContent = `Page ${currentPage} of ${totalPages}`;
                    document.getElementById('btnFirst').disabled = currentPage <= 1;
                    document.getElementById('btnPrev').disabled  = currentPage <= 1;
                    document.getElementById('btnNext').disabled  = currentPage >= totalPages;
                    document.getElementById('btnLast').disabled  = currentPage >= totalPages;
                }

                // The HTTP request summary from RequestLoggingMiddleware is the only entry
                // that has requestMethod + statusCode + elapsed together.
                // Other entries get RequestPath enriched from context but are NOT the summary.
                function isHttpSummary(l) {
                    return !!(l.requestMethod && l.requestPath && l.statusCode != null && l.elapsed != null);
                }

                function buildTraceMap() {
                    traceMap.clear();
                    allLogs.forEach(l => {
                        if (!l.traceId) return;
                        if (!traceMap.has(l.traceId)) traceMap.set(l.traceId, { parent: null, children: [] });
                        const g = traceMap.get(l.traceId);
                        if (isHttpSummary(l)) g.parent = l; else g.children.push(l);
                    });
                }

                // An entry is a child if its traceId has an HTTP summary parent
                function isChild(l) {
                    if (!l.traceId || isHttpSummary(l)) return false;
                    const g = traceMap.get(l.traceId);
                    return !!(g && g.parent);
                }

                function applyClientFilters() {
                    const dateFrom = document.getElementById('dateFrom').value;
                    const dateTo   = document.getElementById('dateTo').value;

                    let filtered = allLogs;
                    if (dateFrom) filtered = filtered.filter(l => l.timestamp >= dateFrom);
                    if (dateTo)   filtered = filtered.filter(l => l.timestamp.substring(0,10) <= dateTo);

                    updateStats(filtered);
                    renderTable(filtered);
                }

                function renderTable(logs) {
                    const body  = document.getElementById('logBody');
                    const empty = document.getElementById('empty');
                    // Hide entries that are grouped under a parent HTTP row
                    const visible = logs.filter(l => !isChild(l));
                    if (!visible.length) { body.innerHTML = ''; empty.style.display = 'block'; return; }
                    empty.style.display = 'none';
                    body.innerHTML = visible.map(buildRow).join('');
                }

                function buildRow(l, i) {
                    const exClass    = l.exception ? ' has-exception' : '';
                    const levelClass = 'badge level-' + (l.level || 'Information');

                    const methodBadge = l.requestMethod
                        ? `<span class="badge method-${l.requestMethod.toLowerCase()}">${esc(l.requestMethod)}</span>` : '';

                    let statusClass = '';
                    if (l.statusCode) {
                        const s = l.statusCode;
                        statusClass = s < 300 ? 'status-2xx' : s < 400 ? 'status-3xx' : s < 500 ? 'status-4xx' : 'status-5xx';
                    }
                    const statusBadge = l.statusCode
                        ? `<span class="badge ${statusClass}">${l.statusCode}</span>` : '';

                    let pathMsg;
                    if (l.requestPath) {
                        const preview = l.message
                            ? `<div class="msg-preview">${esc(trunc(l.message, 140))}</div>` : '';
                        pathMsg = `<div class="path-cell">${esc(l.requestPath)}${preview}</div>`;
                    } else {
                        pathMsg = `<span class="path-cell app-message">${esc(trunc(l.message || '', 160))}</span>`;
                    }

                    let durCell = '';
                    if (l.elapsed != null) {
                        const ms  = l.elapsed;
                        const cls = ms > 2000 ? ' very-slow' : ms > 500 ? ' slow' : '';
                        durCell = `<span class="duration${cls}">${ms.toFixed(1)} ms</span>`;
                    }

                    return `
                    <tr class="log-row${exClass}" onclick="toggleRow(${i})" data-idx="${i}">
                        <td class="time">${esc(l.timestamp)}</td>
                        <td><span class="${levelClass}">${esc(l.level)}</span></td>
                        <td><div class="method-status">${methodBadge}${statusBadge}</div></td>
                        <td>${pathMsg}</td>
                        <td>${durCell}</td>
                        <td><button class="expand-btn" onclick="event.stopPropagation();toggleRow(${i})">+</button></td>
                    </tr>
                    <tr class="detail-row" id="detail-${i}" style="display:none">
                        <td colspan="6">${buildDetail(l, i)}</td>
                    </tr>`;
                }

                function buildDetail(l, rowIdx) {
                    const items = [];
                    if (l.machineName)    items.push(['Machine',   l.machineName]);
                    if (l.threadId!=null) items.push(['Thread ID', l.threadId]);
                    if (l.requestMethod)  items.push(['Method',    l.requestMethod]);
                    if (l.requestPath)    items.push(['Path',      l.requestPath]);
                    if (l.statusCode)     items.push(['Status',    l.statusCode]);
                    if (l.elapsed!=null)  items.push(['Elapsed',   l.elapsed.toFixed(4)+' ms']);
                    if (l.level)          items.push(['Level',     l.level]);
                    if (l.traceId)        items.push(['Trace ID',  l.traceId]);
                    if (l.properties)     Object.entries(l.properties).forEach(([k,v]) => items.push([k, v]));

                    let html = '<div class="detail-panel">';

                    // Request timeline — grouped children, each expandable
                    if (l.requestPath && l.traceId) {
                        const group = traceMap.get(l.traceId);
                        const children = (group ? group.children : [])
                            .slice().sort((a, b) => a.timestamp.localeCompare(b.timestamp));
                        if (children.length) {
                            const countBadge = `<span class="badge level-Information">${children.length}</span>`;
                            html += `<div class="timeline-title">Request Timeline ${countBadge}</div>
                            <table class="sub-table">
                                <thead><tr><th>Time</th><th>Level</th><th>Message</th><th style="width:36px"></th></tr></thead>
                                <tbody>${children.map((e, j) => buildSubRow(e, `${rowIdx}-${j}`)).join('')}</tbody>
                            </table>`;
                        }
                    }

                    if (items.length) html += `<div class="detail-props">${buildProps(items)}</div>`;
                    if (l.message)   html += `<div class="detail-section"><div class="detail-section-title">Message</div><div class="detail-message">${esc(l.message)}</div></div>`;
                    if (l.exception) html += `<div class="detail-section"><div class="detail-section-title">Exception</div><div class="detail-exception">${esc(l.exception)}</div></div>`;
                    html += '</div>';
                    return html;
                }

                // Reusable props renderer (used in both main detail and sub-detail)
                function buildProps(items) {
                    return items.map(([k,v]) => {
                        const str = String(v ?? '');
                        const long = str.length > 80 || str.includes('\n');
                        return long
                            ? `<div class="detail-prop is-long" onclick="toggleProp(this)">
                                <span class="detail-prop-label">${esc(k)}</span>
                                <span class="detail-prop-value">${esc(str)}</span>
                                <span class="detail-prop-toggle">▼ more</span>
                               </div>`
                            : `<div class="detail-prop">
                                <span class="detail-prop-label">${esc(k)}</span>
                                <span class="detail-prop-value expanded">${esc(str)}</span>
                               </div>`;
                    }).join('');
                }

                function buildSubRow(e, id) {
                    const time = e.timestamp ? e.timestamp.substring(11, 23) : '';
                    const levelClass = 'badge level-' + (e.level || 'Information');
                    const exClass = e.exception ? ' has-exception' : '';
                    return `
                    <tr class="sub-row${exClass}" onclick="toggleSubRow('sd-${id}')">
                        <td class="time">${esc(time)}</td>
                        <td><span class="${levelClass}">${esc(e.level)}</span></td>
                        <td class="path-cell app-message">${esc(trunc(e.message || '', 200))}</td>
                        <td><button class="expand-btn" onclick="event.stopPropagation();toggleSubRow('sd-${id}')">+</button></td>
                    </tr>
                    <tr id="sd-${id}" class="sub-detail-row" style="display:none">
                        <td colspan="4">${buildSubDetail(e)}</td>
                    </tr>`;
                }

                function buildSubDetail(e) {
                    const items = [];
                    if (e.machineName)    items.push(['Machine',   e.machineName]);
                    if (e.threadId!=null) items.push(['Thread ID', e.threadId]);
                    if (e.level)          items.push(['Level',     e.level]);
                    if (e.traceId)        items.push(['Trace ID',  e.traceId]);
                    if (e.properties)     Object.entries(e.properties).forEach(([k,v]) => items.push([k, v]));
                    let html = '<div class="detail-panel">';
                    if (items.length) html += `<div class="detail-props">${buildProps(items)}</div>`;
                    if (e.message)   html += `<div class="detail-section"><div class="detail-section-title">Message</div><div class="detail-message">${esc(e.message)}</div></div>`;
                    if (e.exception) html += `<div class="detail-section"><div class="detail-section-title">Exception</div><div class="detail-exception">${esc(e.exception)}</div></div>`;
                    html += '</div>';
                    return html;
                }

                function toggleSubRow(id) {
                    const row = document.getElementById(id);
                    if (!row) return;
                    const open = row.style.display === 'none';
                    row.style.display = open ? '' : 'none';
                    const btn = row.previousElementSibling && row.previousElementSibling.querySelector('.expand-btn');
                    if (btn) btn.textContent = open ? '−' : '+';
                }

                function toggleProp(el) {
                    const val = el.querySelector('.detail-prop-value');
                    const tog = el.querySelector('.detail-prop-toggle');
                    const expanded = val.classList.toggle('expanded');
                    tog.textContent = expanded ? '▲ less' : '▼ more';
                }

                function toggleRow(i) {
                    const row = document.getElementById('detail-' + i);
                    if (!row) return;
                    const btn  = document.querySelector(`[data-idx="${i}"] .expand-btn`);
                    const open = row.style.display === 'none';
                    row.style.display = open ? '' : 'none';
                    if (btn) btn.textContent = open ? '−' : '+';
                }

                function updateStats(logs) {
                    let inf=0, wrn=0, err=0, ftl=0;
                    logs.forEach(l => {
                        if      (l.level==='Information') inf++;
                        else if (l.level==='Warning')     wrn++;
                        else if (l.level==='Error')       err++;
                        else if (l.level==='Fatal')       ftl++;
                    });
                    document.getElementById('cntInf').textContent = inf;
                    document.getElementById('cntWrn').textContent = wrn;
                    document.getElementById('cntErr').textContent = err;
                    document.getElementById('cntFtl').textContent = ftl;
                    const rows = logs.filter(l => !isChild(l)).length;
                    const grouped = logs.length - rows;
                    document.getElementById('totalCount').textContent =
                        grouped > 0 ? `${rows} rows · ${logs.length} events` : `${rows} entries`;
                }

                function toggleAuto() {
                    const btn = document.getElementById('autoBtn');
                    if (autoInterval) {
                        clearInterval(autoInterval); autoInterval = null;
                        btn.textContent = 'Auto: OFF'; btn.classList.remove('active');
                    } else {
                        autoInterval = setInterval(loadLogs, 5000);
                        btn.textContent = 'Auto: ON'; btn.classList.add('active');
                    }
                }

                function esc(s) {
                    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
                }

                function trunc(s, n) {
                    s = String(s).replace(/\r?\n/g, ' ');
                    return s.length > n ? s.substring(0, n) + '…' : s;
                }

                document.getElementById('searchInput').addEventListener('keyup', () => {
                    clearTimeout(searchTimeout);
                    searchTimeout = setTimeout(() => loadLogs(1), 300);
                });

                loadFiles().then(loadLogs);
            </script>
        </body>
        </html>
        """;
}
