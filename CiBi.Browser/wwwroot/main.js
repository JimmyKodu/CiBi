import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// 真实进度：hook fetch，对 _framework/ 的 .wasm/.js 用 XHR 加载（onprogress 给真实传输字节），按 已传输/总字节 计算
const bar = document.getElementById('splash-progress-bar');
const txt = document.getElementById('splash-progress-text');
const setP = v => {
    if (bar) bar.style.width = v + '%';
    if (txt) txt.textContent = Math.round(v) + '%';
};
setP(0);

const origFetch = window.fetch.bind(window);
const resLoaded = Object.create(null);
const resTotal = Object.create(null);
let byteProgress = false;
let simP = 0;

function updateProgress() {
    let total = 0, loaded = 0;
    for (const u in resTotal) { total += resTotal[u]; loaded += resLoaded[u] || 0; }
    if (total > 0) setP(Math.min(99, (loaded / total) * 100));
}

// fallback：dev server(WasmAppHost) 不返回 Content-Length 无法算字节进度时，用模拟减速给反馈；
// 线上(GitHub Pages 等)有 Content-Length 会切到真实字节进度
const simTimer = setInterval(() => {
    if (byteProgress) return;
    simP = Math.min(90, simP + (simP < 30 ? 4 : simP < 60 ? 2 : simP < 85 ? 0.8 : 0.3));
    setP(simP);
}, 220);

// 用 XHR 替代 fetch 加载 _framework 资源：onprogress 给真实传输字节（含 gzip 压缩后大小）；
// 完成后用流式 Response 包装解压后的 arraybuffer，兼容 WebAssembly streaming 编译
window.fetch = function (input, init) {
    const url = typeof input === 'string' ? input : (input && input.url) || '';
    const tracked = url.includes('/_framework/') && (url.endsWith('.wasm') || url.endsWith('.js'));
    if (!tracked) return origFetch(input, init);

    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.responseType = 'arraybuffer';
        xhr.onprogress = e => {
            if (e.lengthComputable) {
                byteProgress = true;
                resTotal[url] = e.total;
                resLoaded[url] = e.loaded;
                updateProgress();
            }
        };
        xhr.onload = () => {
            if (resTotal[url]) resLoaded[url] = resTotal[url];
            updateProgress();
            const headers = new Headers();
            xhr.getAllResponseHeaders().trim().split(/\r?\n/).forEach(line => {
                const idx = line.indexOf(':');
                if (idx > 0) headers.set(line.slice(0, idx).trim(), line.slice(idx + 1).trim());
            });
            const stream = new ReadableStream({
                start(ctrl) { ctrl.enqueue(new Uint8Array(xhr.response)); ctrl.close(); }
            });
            resolve(new Response(stream, { status: xhr.status, statusText: xhr.statusText, headers }));
        };
        xhr.onerror = () => reject(new TypeError('Network error loading ' + url));
        xhr.send();
    });
};

try {
    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    setP(99);

    const config = dotnetRuntime.getConfig();
    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
    setP(100);
} finally {
    clearInterval(simTimer);
    window.fetch = origFetch;
}
