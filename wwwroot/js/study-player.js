/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — FULL SONG STREAMING ENGINE
   Primary: Backend proxy → YouTube audio (all famous songs work!)
   Secondary: Audius (indie/electronic) + Jamendo (licensed tracks)
   All audio plays via native HTML5 <audio> — full length, seekable.
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyMusicPlayer();
});

function initStudyMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    const audio = new Audio();
    audio.crossOrigin = 'anonymous';
    audio.preload = 'auto';

    // DOM refs
    const playBtn = document.getElementById('sp-play-btn');
    const playIcon = document.getElementById('sp-play-icon');
    const titleEl = document.getElementById('sp-title');
    const artistEl = document.getElementById('sp-artist');
    const coverEl = document.getElementById('sp-cover');
    const volumeSlider = document.getElementById('sp-volume');
    const progressBar = document.getElementById('sp-progress');
    const timeCurrent = document.getElementById('sp-time-current');
    const timeTotal = document.getElementById('sp-time-total');
    const searchInput = document.getElementById('sp-search-input');
    const searchResults = document.getElementById('sp-search-results');
    const eqBars = document.querySelectorAll('.sp-eq-bar');
    const rainBtn = document.getElementById('sp-rain-btn');
    const minimizeBtn = document.getElementById('sp-min-btn');
    const frameDiv = document.getElementById('sp-yt-frame');

    let isPlaying = false;
    let isUserSeeking = false;
    let currentPlaylist = [];
    let currentIndex = 0;

    // ── Volume restore ──────────────────────────────────────────────────────
    const savedVol = localStorage.getItem('sp_vol') || 0.8;
    audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = audio.volume;

    // ── Default station ─────────────────────────────────────────────────────
    loadStation('lofi');

    // ── Station chips ───────────────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const key = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            loadStation(key);
        });
    });

    async function loadStation(key) {
        const queries = {
            lofi: 'lofi hip hop chill beats to study',
            focus: 'deep focus study music ambient',
            piano: 'relaxing piano music study',
            synthwave: 'synthwave chill retrowave',
            jazz: 'jazz lofi coffee shop'
        };
        await searchAndPlay(queries[key] || key);
    }

    // ── Core: search + auto-play first result ───────────────────────────────
    async function searchAndPlay(query) {
        updateUI('Searching…', query, null);
        setPlayingState(false);
        audio.pause();
        if (frameDiv) frameDiv.style.display = 'none';

        const tracks = await fetchAllTracks(query);
        if (tracks.length > 0) {
            currentPlaylist = tracks;
            currentIndex = 0;
            await playFromPlaylist(0);
        } else {
            updateUI('No tracks found', 'Try a different search', null);
        }
    }

    // ── Fetch from ALL sources in parallel ──────────────────────────────────
    async function fetchAllTracks(query) {
        const allResults = [];

        const [proxyRes, audiusRes, jamendoRes] = await Promise.allSettled([
            fetchFromProxy(query),
            fetchFromAudius(query),
            fetchFromJamendo(query)
        ]);

        // Proxy results (YouTube) go first — these have the famous songs
        if (proxyRes.status === 'fulfilled') allResults.push(...proxyRes.value);
        if (audiusRes.status === 'fulfilled') allResults.push(...audiusRes.value);
        if (jamendoRes.status === 'fulfilled') allResults.push(...jamendoRes.value);

        return allResults;
    }

    // ── PRIMARY: Our backend proxy (YouTube via Invidious, server-side) ────
    async function fetchFromProxy(query) {
        try {
            const res = await fetch(`/api/music/search?q=${encodeURIComponent(query)}`);
            if (!res.ok) return [];
            const data = await res.json();
            return data.map(t => ({
                title: t.title,
                artist: t.artist,
                cover: t.cover,
                url: t.streamUrl,   // /api/music/stream/{videoId}
                duration: t.duration || 0,
                source: 'YouTube',
                badgeClass: 'sp-badge-yt'
            }));
        } catch { return []; }
    }

    // ── Audius (indie, electronic, lofi) ─────────────────────────────────────
    async function fetchFromAudius(query) {
        try {
            const res = await fetch(`https://api.audius.co/v1/tracks/search?query=${encodeURIComponent(query)}&app_name=READIT`);
            if (!res.ok) return [];
            const data = await res.json();
            if (!data.data) return [];
            return data.data.slice(0, 5).map(t => ({
                title: t.title,
                artist: t.user?.name || 'Unknown',
                cover: t.artwork?.['480x480'] || t.artwork?.['150x150'] || null,
                url: `https://api.audius.co/v1/tracks/${t.id}/stream?app_name=READIT`,
                duration: t.duration || 0,
                source: 'Audius',
                badgeClass: 'sp-badge-audius'
            }));
        } catch { return []; }
    }

    // ── Jamendo (free licensed music) ────────────────────────────────────────
    async function fetchFromJamendo(query) {
        try {
            const res = await fetch(`https://api.jamendo.com/v3.0/tracks/?client_id=5b6e5f02&format=json&limit=5&namesearch=${encodeURIComponent(query)}&include=musicinfo&audioformat=mp32`);
            if (!res.ok) return [];
            const data = await res.json();
            if (!data.results) return [];
            return data.results.map(t => ({
                title: t.name,
                artist: t.artist_name || 'Unknown',
                cover: t.album_image || t.image || null,
                url: t.audio,
                duration: parseInt(t.duration) || 0,
                source: 'Jamendo',
                badgeClass: 'sp-badge-jamendo'
            }));
        } catch { return []; }
    }

    // ── Play from playlist ──────────────────────────────────────────────────
    async function playFromPlaylist(index) {
        if (index < 0 || index >= currentPlaylist.length) return;
        currentIndex = index;
        const track = currentPlaylist[index];

        updateUI(track.title, track.artist, track.cover);
        if (frameDiv) frameDiv.style.display = 'none';

        audio.src = track.url;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = '0:00';
        if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : '—';

        try {
            await audio.play();
            setPlayingState(true);
        } catch (e) {
            console.warn('Autoplay blocked:', e);
            setPlayingState(false);
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title;
        if (artistEl) artistEl.textContent = artist;
        if (coverEl && cover) coverEl.src = cover;
    }

    // ── Play / Pause ────────────────────────────────────────────────────────
    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPlaying) { audio.pause(); setPlayingState(false); }
            else { audio.play().then(() => setPlayingState(true)).catch(() => {}); }
        });
    }

    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? 'bi bi-pause-fill' : 'bi bi-play-fill';
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
    }

    audio.addEventListener('play', () => setPlayingState(true));
    audio.addEventListener('pause', () => setPlayingState(false));
    audio.addEventListener('ended', () => {
        if (currentPlaylist.length > 0 && currentIndex < currentPlaylist.length - 1) {
            playFromPlaylist(currentIndex + 1);
        } else {
            setPlayingState(false);
        }
    });
    audio.addEventListener('error', () => {
        console.warn('Track failed, skipping to next…');
        if (currentPlaylist.length > 0 && currentIndex < currentPlaylist.length - 1) {
            playFromPlaylist(currentIndex + 1);
        }
    });

    // ── Progress bar ────────────────────────────────────────────────────────
    audio.addEventListener('timeupdate', () => {
        if (isUserSeeking) return;
        const cur = audio.currentTime || 0;
        const dur = audio.duration || 0;
        if (dur > 0 && !isNaN(dur)) {
            if (progressBar) progressBar.value = (cur / dur) * 100;
            if (timeCurrent) timeCurrent.textContent = formatTime(cur);
            if (timeTotal) timeTotal.textContent = formatTime(dur);
        } else {
            if (timeCurrent) timeCurrent.textContent = formatTime(cur);
        }
    });

    if (progressBar) {
        progressBar.addEventListener('mousedown', () => { isUserSeeking = true; });
        progressBar.addEventListener('touchstart', () => { isUserSeeking = true; });
        progressBar.addEventListener('input', (e) => {
            const pct = parseFloat(e.target.value);
            if (audio.duration && !isNaN(audio.duration))
                if (timeCurrent) timeCurrent.textContent = formatTime((pct / 100) * audio.duration);
        });
        const seekEnd = (e) => {
            const pct = parseFloat(e.target.value);
            if (audio.duration && !isNaN(audio.duration))
                audio.currentTime = (pct / 100) * audio.duration;
            isUserSeeking = false;
        };
        progressBar.addEventListener('change', seekEnd);
        progressBar.addEventListener('mouseup', seekEnd);
        progressBar.addEventListener('touchend', seekEnd);
    }

    function formatTime(s) {
        if (isNaN(s) || s < 0) return '0:00';
        const m = Math.floor(s / 60);
        const sec = Math.floor(s % 60);
        return `${m}:${sec < 10 ? '0' : ''}${sec}`;
    }

    // ── Volume ──────────────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const v = parseFloat(e.target.value);
            audio.volume = v;
            localStorage.setItem('sp_vol', v);
        });
    }

    // ── Search UI ───────────────────────────────────────────────────────────
    let debounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(debounce);
            const q = e.target.value.trim();
            if (q.length < 2) { if (searchResults) searchResults.style.display = 'none'; return; }
            debounce = setTimeout(() => showSearchResults(q), 350);
        });
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                const q = searchInput.value.trim();
                if (q.length >= 2) {
                    clearTimeout(debounce);
                    if (searchResults) searchResults.style.display = 'none';
                    searchInput.value = '';
                    searchAndPlay(q);
                }
            }
        });
    }

    async function showSearchResults(query) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding:16px;text-align:center;color:var(--accent);font-size:12px">
            <i class="bi bi-music-note-beamed" style="animation:spin 1s linear infinite"></i> Searching…</div>`;
        searchResults.style.display = 'block';

        const tracks = await fetchAllTracks(query);
        if (tracks.length === 0) {
            searchResults.innerHTML = `<div style="padding:16px;text-align:center;color:#71717a;font-size:12px">No results. Try different keywords.</div>`;
            return;
        }

        searchResults.innerHTML = '';
        tracks.forEach((t, i) => {
            const el = document.createElement('div');
            el.className = 'sp-search-item';
            const img = t.cover || 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100&auto=format&fit=crop&q=80';
            const dur = t.duration ? formatTime(t.duration) : '';

            el.innerHTML = `
                <img src="${img}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100'" />
                <div style="flex:1;min-width:0">
                    <div class="sp-search-title">${esc(t.title)}</div>
                    <div class="sp-search-artist">${esc(t.artist)}${dur ? ' · ' + dur : ''}</div>
                </div>
                <span class="sp-source-badge ${t.badgeClass}">${t.source}</span>
                <i class="bi bi-play-circle-fill" style="color:var(--accent);font-size:18px;flex-shrink:0;cursor:pointer"></i>`;

            el.addEventListener('click', () => {
                currentPlaylist = tracks;
                playFromPlaylist(i);
                searchResults.style.display = 'none';
                searchInput.value = '';
            });
            searchResults.appendChild(el);
        });
    }

    function esc(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    document.addEventListener('click', (e) => {
        if (searchResults && searchInput && !searchResults.contains(e.target) && !searchInput.contains(e.target))
            searchResults.style.display = 'none';
    });

    // ── Minimize ────────────────────────────────────────────────────────────
    if (minimizeBtn) {
        minimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            playerCard.classList.toggle('minimized');
            const ic = minimizeBtn.querySelector('i');
            if (ic) ic.className = playerCard.classList.contains('minimized') ? 'bi bi-chevron-up' : 'bi bi-chevron-down';
        });
    }

    // ── Rain ambient ────────────────────────────────────────────────────────
    let rainOn = false, actx = null, rGain = null;
    function mkRain() {
        if (actx) return;
        const C = window.AudioContext || window.webkitAudioContext;
        actx = new C();
        const buf = actx.createBuffer(1, 2 * actx.sampleRate, actx.sampleRate);
        const d = buf.getChannelData(0);
        for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
        const src = actx.createBufferSource(); src.buffer = buf; src.loop = true;
        const flt = actx.createBiquadFilter(); flt.type = 'lowpass'; flt.frequency.setValueAtTime(750, actx.currentTime);
        rGain = actx.createGain(); rGain.gain.setValueAtTime(0.12, actx.currentTime);
        src.connect(flt); flt.connect(rGain); rGain.connect(actx.destination); src.start();
    }
    if (rainBtn) {
        rainBtn.addEventListener('click', () => {
            rainOn = !rainOn;
            rainBtn.classList.toggle('active', rainOn);
            if (rainOn) { mkRain(); if (actx?.state === 'suspended') actx.resume(); if (rGain) rGain.gain.setValueAtTime(0.15, actx.currentTime); }
            else { if (rGain && actx) rGain.gain.setValueAtTime(0, actx.currentTime); }
        });
    }
}
