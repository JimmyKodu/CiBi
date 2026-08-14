import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// 启动进度条：加载期间减速增长，create()/runMain() 完成后跳满，给用户明确的加载反馈
const bar = document.getElementById('splash-progress-bar');
const txt = document.getElementById('splash-progress-text');
let p = 0;
const setP = v => {
    p = v;
    if (bar) bar.style.width = v + '%';
    if (txt) txt.textContent = Math.round(v) + '%';
};
setP(2);
const timer = setInterval(() => {
    // 减速增长，上限 92%（剩余 8% 给运行时初始化，避免假死在 100%）
    setP(Math.min(92, p + (p < 30 ? 4 : p < 60 ? 2 : p < 85 ? 0.8 : 0.3)));
}, 220);

try {
    const dotnetRuntime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    setP(95);

    const config = dotnetRuntime.getConfig();

    await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
    setP(100);
} finally {
    clearInterval(timer);
}
