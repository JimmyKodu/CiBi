import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// 真实进度：hook fetch，对 _framework/ 的 .wasm/.js 用 XHR 加载（onprogress 给真实传输字节），按 已传输/总字节 计算
const bar = document.getElementById('splash-progress-bar');
const txt = document.getElementById('splash-progress-text');
const file = document.getElementById('splash-progress-file');
const setP = v => {
    if (bar) bar.style.width = v + '%';
    if (txt) txt.textContent = Math.round(v) + '%';
};
// 显示当前正在下载/加载的文件名（去掉哈希段，如 dotnet.native.xn6kila1sr.wasm → dotnet.native.wasm）
const setFile = name => { if (file) file.textContent = name; };
setP(0);
setFile('加载资源…');

const origFetch = window.fetch.bind(window);
const resLoaded = Object.create(null);
const resTotal = Object.create(null);
let byteP = 0;            // 字节级进度（仅统计到带 Content-Length 的资源）
let simP = 0;             // 模拟进度兜底：资源走缓存/无 Content-Length 时保证条持续生长
let trustedTotal = false; // 大文件(>3MB)已被字节跟踪，真实进度可信，停用模拟
let phaseDone = false;    // 下载+初始化结束，进度交给收尾阶段

let dispP = 0;
// 100ms UI 心跳：条/百分比平滑逼近目标进度，杜绝瞬时跳满或长时间静止
// （暖缓存时字节比例会瞬间到 99%，直接设宽度会让条看起来从头到尾没动过）
const uiTimer = setInterval(() => {
    const t = Math.max(simP, byteP);
    if (t > dispP) dispP = Math.min(t, Math.max(dispP + 0.2, dispP + (t - dispP) * 0.1));
    setP(dispP);
}, 100);

function updateProgress() {
    let total = 0, loaded = 0;
    for (const u in resTotal) { total += resTotal[u]; loaded += resLoaded[u] || 0; }
    if (total > 0) {
        if (total > 3 * 1024 * 1024) trustedTotal = true;
        byteP = Math.min(96, (loaded / total) * 100);
    }
}

// 模拟减速：仅在字节进度不可信（大文件走缓存/流式无 Content-Length）时推进，避免进度条假死；
// 线上(GitHub Pages 等)大文件被字节跟踪后自动停用
const simTimer = setInterval(() => {
    if (phaseDone || trustedTotal) return;
    simP = Math.min(92, simP + (simP < 30 ? 4 : simP < 60 ? 2 : simP < 85 ? 0.8 : 0.3));
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
            setFile(url.split('/').pop().replace(/\.[a-z0-9]{8,}\.(wasm|js)$/i, '.$1'));
            if (e.lengthComputable) {
                resTotal[url] = e.total;
                resLoaded[url] = e.loaded;
                updateProgress();
            }
        };
        xhr.onload = () => {
            // 无 Content-Length（缓存命中/流式响应）的资源：完成后按真实字节补记，条按文件完成粒度推进
            const real = (xhr.response && xhr.response.byteLength) || 0;
            if (resTotal[url]) resLoaded[url] = resTotal[url];
            else if (real > 0) { resTotal[url] = real; resLoaded[url] = real; }
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

// 进入“启动应用”阶段：runMain 会长时间阻塞主线程，JS 驱动的填充/心跳会冻结；
// 轨道上的扫光是纯 CSS 合成器动画，不依赖 JS，阻塞期间依然流动
const markStartupPhase = () => { if (file) file.classList.add('pulse'); };

try {
    setFile('初始化运行时…');
    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    phaseDone = true;
    clearInterval(simTimer);
    clearInterval(uiTimer);

    const config = dotnetRuntime.getConfig();
    setP(99);
    setFile('启动应用…');
    markStartupPhase();
    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
    setP(100);
} finally {
    clearInterval(simTimer);
    clearInterval(uiTimer);
    window.fetch = origFetch;
}
