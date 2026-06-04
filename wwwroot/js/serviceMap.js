window.ServiceMap = (() => {

    function applyTheme(dark) {
        document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
        localStorage.setItem('sm-theme', dark ? 'dark' : 'light');
    }

    function loadTheme() {
        const saved = localStorage.getItem('sm-theme');
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const dark = saved ? saved === 'dark' : prefersDark;
        applyTheme(dark);
        return dark;
    }

    let dragDotRef = null;

    function initDrag(dotNetRef) {
        dragDotRef = dotNetRef;
    }

    function enableNodeDrag(nodeId, el, canvasEl) {
        if (!el || !canvasEl) return;
        let dragging = false, ox = 0, oy = 0;

        el.addEventListener('mousedown', (e) => {
            if (e.button !== 0) return;
            if (e.target.tagName === 'BUTTON') return;

            dragging = true;
            const rect = canvasEl.getBoundingClientRect();
            const scale = getScale();

            ox = (e.clientX - rect.left + canvasEl.scrollLeft) / scale - parseFloat(el.style.left);
            oy = (e.clientY - rect.top + canvasEl.scrollTop) / scale - parseFloat(el.style.top);

            el.style.cursor = 'grabbing';
            el.style.zIndex = 9999;

            e.preventDefault();
            e.stopPropagation();
        });

        document.addEventListener('mousemove', (e) => {
            if (!dragging) return;

            const rect = canvasEl.getBoundingClientRect();
            const scale = getScale();

            const inner = canvasEl.querySelector('.st-canvas-inner') || canvasEl;
            const nodeW = el.offsetWidth || 155;
            const nodeH = el.offsetHeight || 95;
            const maxX = Math.max(0, (inner.scrollWidth || inner.offsetWidth || 1600) - nodeW);
            const maxY = Math.max(0, (inner.scrollHeight || inner.offsetHeight || 900) - nodeH);

            const rawX = (e.clientX - rect.left + canvasEl.scrollLeft) / scale - ox;
            const rawY = (e.clientY - rect.top + canvasEl.scrollTop) / scale - oy;
            const x = Math.min(Math.max(0, rawX), maxX);
            const y = Math.min(Math.max(0, rawY), maxY);

            el.style.left = x + 'px';
            el.style.top = y + 'px';

            if (dragDotRef) dragDotRef.invokeMethodAsync('OnNodeDragging', nodeId, x, y);
        });

        document.addEventListener('mouseup', () => {
            if (!dragging) return;

            dragging = false;
            el.style.cursor = '';
            el.style.zIndex = '';

            const x = parseFloat(el.style.left);
            const y = parseFloat(el.style.top);

            if (dragDotRef) dragDotRef.invokeMethodAsync('OnNodeDropped', nodeId, x, y);
        });
    }

    let currentScale = 1.0;
    let panX = 0;
    let panY = 0;
    let isPanning = false;
    let panStartX = 0;
    let panStartY = 0;

    function getScale() {
        return currentScale;
    }

    function initZoomPan(canvasEl) {
        if (!canvasEl) return;

        canvasEl.addEventListener('wheel', (e) => {
            e.preventDefault();

            const delta = e.deltaY > 0 ? 0.9 : 1.1;
            currentScale = Math.min(Math.max(currentScale * delta, 0.3), 3.0);

            applyTransform(canvasEl);
        }, { passive: false });

        canvasEl.addEventListener('mousedown', (e) => {
            if (e.button === 1 || (e.button === 0 && e.altKey)) {
                isPanning = true;
                panStartX = e.clientX - panX;
                panStartY = e.clientY - panY;
                canvasEl.style.cursor = 'grabbing';
                e.preventDefault();
            }
        });

        document.addEventListener('mousemove', (e) => {
            if (!isPanning) return;

            panX = e.clientX - panStartX;
            panY = e.clientY - panStartY;

            applyTransform(canvasEl);
        });

        document.addEventListener('mouseup', () => {
            if (!isPanning) return;

            isPanning = false;
            canvasEl.style.cursor = '';
        });
    }

    function applyTransform(canvasEl) {
        const inner = canvasEl.querySelector('.st-canvas-inner');
        if (inner) {
            inner.style.transform = `translate(${panX}px, ${panY}px) scale(${currentScale})`;
        }
    }

    function resetZoom(canvasEl) {
        currentScale = 1.0;
        panX = 0;
        panY = 0;
        applyTransform(canvasEl);
    }

    function zoomIn(canvasEl) {
        currentScale = Math.min(currentScale * 1.2, 3.0);
        applyTransform(canvasEl);
    }

    function zoomOut(canvasEl) {
        currentScale = Math.max(currentScale * 0.8, 0.3);
        applyTransform(canvasEl);
    }

    function esc(s) {
        return (s || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function svgToBase64(svgStr) {
        const bytes = new TextEncoder().encode(svgStr);
        let binary = '';
        bytes.forEach(b => binary += String.fromCharCode(b));
        return btoa(binary);
    }

    function getStatusFromNode(el) {
        if (el.classList.contains('active')) return 'Active';
        if (el.classList.contains('down')) return 'Down';
        return 'Active';
    }

    function buildExportSVG() {
        const canvasInner = document.getElementById('st-canvas-inner');
        const svgEl = document.getElementById('st-svg');

        if (!canvasInner || !svgEl) {
            alert('Önce proje veya Tüm Servisler ekranını aç, sonra export al.');
            return null;
        }

        const nodes = Array.from(canvasInner.querySelectorAll('.st-node:not(.ghost)'));

        if (nodes.length === 0) {
            alert('Export için haritada en az bir servis olmalı.');
            return null;
        }

        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';

        const bg = isDark ? '#1a1f2e' : '#f6f8fb';
        const cardBg = isDark ? '#1e2537' : '#ffffff';
        const text1 = isDark ? '#e2e8f0' : '#111827';
        const text2 = isDark ? '#94a3b8' : '#6b7280';
        const border = isDark ? '#2d3a52' : '#d9e2ef';

        const nodeW = 155;
        const nodeH = 95;
        const pad = 60;

        let minX = Infinity;
        let minY = Infinity;
        let maxX = 0;
        let maxY = 0;

        nodes.forEach(el => {
            const x = parseFloat(el.style.left) || 0;
            const y = parseFloat(el.style.top) || 0;

            minX = Math.min(minX, x);
            minY = Math.min(minY, y);
            maxX = Math.max(maxX, x + nodeW);
            maxY = Math.max(maxY, y + nodeH);
        });

        const vbX = Math.max(0, minX - pad);
        const vbY = Math.max(0, minY - pad);
        const W = Math.max(maxX - vbX + pad, 900);
        const H = Math.max(maxY - vbY + pad, 500);

        const statusColor = s =>
            s === 'Active' ? '#27ae60' : '#e74c3c';

        const techColor = t => ({
            REST: '#4f86c6',
            SOAP: '#8e44ad',
            MQ: '#e67e22',
            JAR: '#7f8c8d',
            gRPC: '#16a085'
        })[t] || '#7f8c8d';

        let svg = `
<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="${vbX} ${vbY} ${W} ${H}">
<defs>
    <marker id="e-rest" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
        <polygon points="0 0,10 3.5,0 7" fill="#4f86c6"/>
    </marker>
    <marker id="e-soap" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
        <polygon points="0 0,10 3.5,0 7" fill="#8e44ad"/>
    </marker>
    <marker id="e-mq" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
        <polygon points="0 0,10 3.5,0 7" fill="#e67e22"/>
    </marker>
    <marker id="e-def" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
        <polygon points="0 0,10 3.5,0 7" fill="#7f8c8d"/>
    </marker>
</defs>
<rect x="${vbX}" y="${vbY}" width="${W}" height="${H}" fill="${bg}"/>
`;

        const paths = Array.from(svgEl.querySelectorAll('path'));

        paths.forEach(p => {
            const d = p.getAttribute('d') || '';
            const stroke = p.getAttribute('stroke') || '#7f8c8d';
            const sw = p.getAttribute('stroke-width') || '1.5';
            const op = p.getAttribute('opacity') || '1';
            const marker = p.getAttribute('marker-end') || '';

            let exportMarker = 'url(#e-def)';

            if (marker.includes('arr-rest')) exportMarker = 'url(#e-rest)';
            else if (marker.includes('arr-soap')) exportMarker = 'url(#e-soap)';
            else if (marker.includes('arr-mq')) exportMarker = 'url(#e-mq)';

            svg += `<path d="${d}" fill="none" stroke="${stroke}" stroke-width="${sw}" opacity="${op}" marker-end="${exportMarker}"/>`;
        });

        nodes.forEach(el => {
            const x = parseFloat(el.style.left) || 0;
            const y = parseFloat(el.style.top) || 0;

            const name = el.querySelector('.st-node-name')?.textContent?.trim() || '';
            const team = el.querySelector('.st-node-team')?.textContent?.trim() || '';
            const tech = el.querySelector('.st-node-tech')?.textContent?.trim() || 'REST';

            const status = getStatusFromNode(el);
            const sc = statusColor(status);
            const tc = techColor(tech);

            const safeName = name.length > 22 ? name.substring(0, 21) + '…' : name;
            const safeTeam = team.length > 24 ? team.substring(0, 23) + '…' : team;

            svg += `
<rect x="${x + 3}" y="${y + 5}" width="${nodeW}" height="${nodeH}" rx="10" fill="#000000" fill-opacity="0.10"/>
<rect x="${x}" y="${y}" width="${nodeW}" height="${nodeH}" rx="10" fill="${cardBg}" stroke="${border}" stroke-width="1"/>
<rect x="${x}" y="${y}" width="${nodeW}" height="5" rx="10" fill="${sc}"/>
<rect x="${x}" y="${y + 3}" width="${nodeW}" height="2" fill="${sc}"/>

<rect x="${x + 8}" y="${y + 13}" width="42" height="17" rx="5" fill="${tc}" fill-opacity="0.16"/>
<text x="${x + 29}" y="${y + 25}" font-size="9" font-family="Arial, sans-serif" fill="${tc}" text-anchor="middle" font-weight="700">${esc(tech)}</text>

<circle cx="${x + nodeW - 15}" cy="${y + 21}" r="6" fill="${sc}"/>

<text x="${x + 9}" y="${y + 51}" font-size="12" font-family="Arial, sans-serif" fill="${text1}" font-weight="700">${esc(safeName)}</text>
<text x="${x + 9}" y="${y + 70}" font-size="10" font-family="Arial, sans-serif" fill="${text2}">${esc(safeTeam)}</text>
`;
        });

        svg += '</svg>';

        return { svg, W, H };
    }

    function exportSVG() {
        const result = buildExportSVG();
        if (!result) return;

        const blob = new Blob([result.svg], { type: 'image/svg+xml;charset=utf-8' });
        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = 'servicemap.svg';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function exportPNG() {
        const result = buildExportSVG();
        if (!result) return;

        const scale = 2;
        const img = new Image();

        img.onload = () => {
            const canvas = document.createElement('canvas');
            canvas.width = result.W * scale;
            canvas.height = result.H * scale;

            const ctx = canvas.getContext('2d');
            ctx.scale(scale, scale);
            ctx.drawImage(img, 0, 0);

            const a = document.createElement('a');
            a.href = canvas.toDataURL('image/png');
            a.download = 'servicemap.png';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        };

        img.onerror = () => {
            alert('PNG oluşturulamadı. SVG içeriği tarayıcı tarafından çizilemedi.');
        };

        img.src = 'data:image/svg+xml;base64,' + svgToBase64(result.svg);
    }

    function updateMinimap(minimapEl, nodes) {
        if (!minimapEl) return;
        nodes = Array.isArray(nodes) ? nodes : [];

        const mm = minimapEl;
        const mmW = mm.clientWidth || 160;
        const mmH = mm.clientHeight || 90;

        const canvasW = 1400;
        const canvasH = 700;

        const scaleX = mmW / canvasW;
        const scaleY = mmH / canvasH;

        const ctx = mm.getContext('2d');
        ctx.clearRect(0, 0, mmW, mmH);

        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';

        ctx.fillStyle = isDark ? '#1a1f2e' : '#f0f2f5';
        ctx.fillRect(0, 0, mmW, mmH);

        nodes.forEach(n => {
            const x = n.positionX * scaleX;
            const y = n.positionY * scaleY;
            const w = 155 * scaleX;
            const h = 95 * scaleY;

            ctx.fillStyle =
                n.status === 'Active' ? '#27ae60' : '#e74c3c';

            ctx.fillRect(x, y, w, h);
        });
    }

    return {
        applyTheme,
        loadTheme,
        initDrag,
        enableNodeDrag,
        initZoomPan,
        resetZoom,
        zoomIn,
        zoomOut,
        exportSVG,
        exportPNG,
        updateMinimap
    };
})();