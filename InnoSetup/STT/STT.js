// stt.js

// ======= UI elements =======
const statusEl = document.getElementById('status');
const warmupBtn = document.getElementById('warmup');
const applyMicBtn = document.getElementById('applyMic');
const micSelect = document.getElementById('micSelect');
const vuBar = document.querySelector('#vu > span');

// ======= Globals =======
const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
let rec = null;
let ws = null;
let lang = 'en-US';
let isPressed = false;

let mediaStream = null;
let selectedDeviceId = localStorage.getItem('maira.micDeviceId') || '';

let audioContext = null;
let analyser = null;
let sourceNode = null;
let vuTimer = null;

// ======= Helpers =======
function setStatus(text) {
    if (statusEl) statusEl.textContent = text;
}

function post(type, data = {}) {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify(Object.assign({ type }, data)));
    }
}

function wsUrl() {
    // Derive ws URL from current page location
    const { protocol, host } = window.location;
    const wsScheme = protocol === 'https:' ? 'wss:' : 'ws:';
    return `${wsScheme}//${host}/ws`;
}

// ======= WebSocket =======
function ensureWs() {
    if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) return;

    ws = new WebSocket(wsUrl());

    ws.onopen = () => {
        setStatus('Connected. Pick a mic and click Use Microphone.');
        refreshDevices();
    };

    ws.onclose = () => {
        setStatus('WebSocket disconnected. Reconnecting…');
        setTimeout(ensureWs, 1000);
    };

    ws.onmessage = (ev) => {
        try {
            const msg = JSON.parse(ev.data);
            if (msg.type === 'start') startRecognition();
            else if (msg.type === 'stop') stopRecognition();
            else if (msg.type === 'setlang' && msg.lang) lang = msg.lang;
        } catch {
            // ignore bad messages
        }
    };
}
ensureWs();

// ======= Mic selection / permissions =======
async function refreshDevices() {
    try {
        // Ensure labels are populated
        await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
        // ignore; user may not have granted yet
    }

    const devices = await navigator.mediaDevices.enumerateDevices();
    const mics = devices.filter(d => d.kind === 'audioinput');

    micSelect.innerHTML = '';
    for (const d of mics) {
        const opt = document.createElement('option');
        opt.value = d.deviceId;
        opt.text = d.label || ('Microphone ' + (micSelect.length + 1));
        if (selectedDeviceId && d.deviceId === selectedDeviceId) opt.selected = true;
        micSelect.appendChild(opt);
    }
    if (!selectedDeviceId && mics.length) selectedDeviceId = mics[ 0 ].deviceId;
}

navigator.mediaDevices.addEventListener('devicechange', refreshDevices);

async function applyMic() {
    selectedDeviceId = micSelect.value || '';
    localStorage.setItem('maira.micDeviceId', selectedDeviceId);

    stopMediaStream();

    try {
        const constraints = selectedDeviceId ? { audio: { deviceId: { exact: selectedDeviceId } } } : { audio: true };
        mediaStream = await navigator.mediaDevices.getUserMedia(constraints);
        startVuMeter(mediaStream);
        setStatus('Microphone selected. Click Enable Microphone once, then use push-to-talk.');
    } catch (e) {
        setStatus('Failed to get microphone: ' + e);
    }
}

function stopMediaStream() {
    if (mediaStream) {
        for (const t of mediaStream.getTracks()) t.stop();
        mediaStream = null;
    }
    stopVuMeter();
}

function startVuMeter(stream) {
    stopVuMeter();
    audioContext = new (window.AudioContext || window.webkitAudioContext)();
    analyser = audioContext.createAnalyser();
    analyser.fftSize = 512;

    sourceNode = audioContext.createMediaStreamSource(stream);
    sourceNode.connect(analyser);

    const dataArray = new Uint8Array(analyser.frequencyBinCount);

    vuTimer = setInterval(() => {
        analyser.getByteTimeDomainData(dataArray);
        let max = 0;
        for (let i = 0; i < dataArray.length; i++) {
            const v = Math.abs(dataArray[ i ] - 128) / 128;
            if (v > max) max = v;
        }
        vuBar.style.width = Math.min(100, Math.round(max * 100)) + '%';
    }, 60);
}

function stopVuMeter() {
    if (vuTimer) { clearInterval(vuTimer); vuTimer = null; }
    vuBar.style.width = '0%';
    try { sourceNode && sourceNode.disconnect(); } catch { }
    try { audioContext && audioContext.close(); } catch { }
    sourceNode = null; analyser = null; audioContext = null;
}

// ======= Speech Recognition =======
function createRecognizer() {
    if (!SR) { post('error', { message: 'SpeechRecognition not supported in this browser.' }); return null; }
    if (rec) try { rec.abort(); } catch { }

    rec = new SR();
    rec.lang = lang;
    rec.interimResults = true; // show partials
    rec.continuous = true;     // run until we stop()
    rec.maxAlternatives = 1;

    rec.onresult = (e) => {
        let interim = '', final = '';
        for (let i = e.resultIndex; i < e.results.length; i++) {
            const r = e.results[ i ];
            if (r.isFinal) final += r[ 0 ].transcript;
            else interim += r[ 0 ].transcript;
        }
        if (interim) post('stt', { interim });
        if (final) post('stt', { final });
    };

    rec.onerror = (ev) => post('error', { message: ev.error || 'unknown' });

    rec.onend = () => {
        post('end');
        if (isPressed) {
            setTimeout(() => { try { rec.start(); } catch { } }, 60);
        } else {
            setStatus('Stopped (awaiting next push-to-talk)…');
        }
    };

    return rec;
}

function startRecognition() {
    isPressed = true;
    if (!rec) createRecognizer();

    // Keep chosen device stream alive to steer SR to that input
    if (!mediaStream) {
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(s => { mediaStream = s; startVuMeter(s); })
            .catch(() => { });
    }

    try {
        rec.start(); // must be allowed once via warm-up button
        setStatus('Listening… (push-to-talk active)');
    } catch (e) {
        if (!String(e).includes('start')) {
            post('error', { message: e.message || 'start() failed' });
        }
    }
}

function stopRecognition() {
    isPressed = false;
    try { rec && rec.stop(); } catch { }
    setStatus('Stopping…');
}

// ======= Warm-up button =======
warmupBtn.addEventListener('click', async () => {
    try {
        await applyMic(); // will prompt for the selected device
        if (!rec) createRecognizer();

        // Touch SR once so later programmatic start works
        rec.start(); rec.stop();

        setStatus('Microphone enabled. Ready for push-to-talk.');
        warmupBtn.style.display = 'none';
    } catch (e) {
        setStatus('Mic enable failed: ' + e);
    }
});

// ======= ApplyMic button =======
applyMicBtn.addEventListener('click', applyMic);

// Initial device list when page loads (WS onopen also calls refresh)
document.addEventListener('DOMContentLoaded', refreshDevices);
