/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — RELIABLE AUDIO STREAMING ENGINE
   Uses multiple real audio sources that actually play:
   1. Audius API — direct MP3 streaming (indie music, remixes, lofi)
   2. Free Music Archive (Jamendo) — licensed full tracks
   3. Invidious proxy audio — YouTube audio extraction
   Sticky floating bottom-right dock, persists across pages.
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

    // DOM elements
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

    // ── Multiple Invidious instances to try (fallback chain) ──────────────
    const INVIDIOUS_INSTANCES = [
        'https://inv.tux.pizza',
        'https://invidious.fdn.fr',
        'https://yewtu.be',
        'https://vid.puffyan.us',
        'https://invidious.privacyredirect.com'
    ];

    // ── Restore volume ──────────────────────────────────────────────────────
    const savedVol = localStorage.getItem('sp_vol') || 0.8;
    audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = audio.volume;

    // ── Load default station on first visit ─────────────────────────────────
    loadStation('lofi');

    // ── Station chip handlers ───────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const key = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            loadStation(key);
        });
    });

    // ── Load a preset station ───────────────────────────────────────────────
    async function loadStation(key) {
        const stationQueries = {
            lofi: 'lofi hip hop chill beats',
            focus: 'deep focus study ambient',
            piano: 'peaceful piano relaxing',
            synthwave: 'synthwave chillwave retrowave',
            jazz: 'jazz hop coffee shop beats'
        };
        const query = stationQueries[key] || key;
        await searchAndPlay(query);
    }

    // ── Core: search for tracks and auto-play the first result ──────────────
    async function searchAndPlay(query) {
        updateUI('Searching…', query, null);
        setPlayingState(false);
        audio.pause();
        if (frameDiv) frameDiv.style.display = 'none';

        const tracks = await fetchTracks(query);
        if (tracks.length > 0) {
            currentPlaylist = tracks;
            currentIndex = 0;
            await playFromPlaylist(0);
        } else {
            updateUI('No tracks found', 'Try a different search', null);
        }
    }

    // ── Fetch tracks from multiple sources ──────────────────────────────────
    async function fetchTracks(query) {
        const results = [];

        // Launch all searches in parallel
        const [audiusRes, jamendoRes, invRes] = await Promise.allSettled([
            fetchAudiusTracks(query),
            fetchJamendoTracks(query),
            fetchInvidiousTracks(query)
        ]);

        if (audiusRes.status === 'fulfilled') results.push(...audiusRes.value);
        if (jamendoRes.status === 'fulfilled') results.push(...jamendoRes.value);
        if (invRes.status === 'fulfilled') results.push(...invRes.value);

        return results;
    }

    // ── Audius: direct streaming, works great ───────────────────────────────
    async function fetchAudiusTracks(query) {
        try {
            const res = await fetch(`https://api.audius.co/v1/tracks/search?query=${encodeURIComponent(query)}&app_name=READIT`);
            if (!res.ok) return [];
            const data = await res.json();
            if (!data.data) return [];
            return data.data.slice(0, 8).map(t => ({
                title: t.title,
                artist: t.user?.name || 'Unknown Artist',
                cover: t.artwork?.['480x480'] || t.artwork?.['150x150'] || null,
                url: `https://api.audius.co/v1/tracks/${t.id}/stream?app_name=READIT`,
                duration: t.duration || 0,
                source: 'Audius',
                badgeClass: 'sp-badge-audius'
            }));
        } catch { return []; }
    }

    // ── Jamendo: free licensed music, full streams ──────────────────────────
    async function fetchJamendoTracks(query) {
        try {
            const clientId = '5b6e5f02';
            const res = await fetch(`https://api.jamendo.com/v3.0/tracks/?client_id=${clientId}&format=json&limit=8&namesearch=${encodeURIComponent(query)}&include=musicinfo&audioformat=mp32`);
            if (!res.ok) return [];
            const data = await res.json();
            if (!data.results) return [];
            return data.results.map(t => ({
                title: t.name,
                artist: t.artist_name || 'Unknown',
                cover: t.album_image || t.image || null,
                url: t.audio,  // Jamendo gives direct MP3 URLs
                duration: parseInt(t.duration) || 0,
                source: 'Jamendo',
                badgeClass: 'sp-badge-jamendo'
            }));
        } catch { return []; }
    }

    // ── Invidious: extract audio stream URLs from YouTube ────────────────────
    async function fetchInvidiousTracks(query) {
        // First, search for video IDs
        let videos = [];
        for (const instance of INVIDIOUS_INSTANCES) {
            try {
                const res = await fetch(`${instance}/api/v1/search?q=${encodeURIComponent(query)}&type=video&sort_by=relevance`, { signal: AbortSignal.timeout(5000) });
                if (!res.ok) continue;
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    videos = data.slice(0, 6);
                    break;
                }
            } catch { continue; }
        }
        if (videos.length === 0) return [];

        // For each video, try to get the audio stream URL
        const tracks = [];
        for (const v of videos.slice(0, 4)) {
            const audioUrl = await getInvidiousAudioUrl(v.videoId);
            if (audioUrl) {
                tracks.push({
                    title: v.title,
                    artist: v.author || 'YouTube',
                    cover: v.videoThumbnails?.[0]?.url || null,
                    url: audioUrl,
                    duration: v.lengthSeconds || 0,
                    source: 'YouTube',
                    badgeClass: 'sp-badge-yt'
                });
            }
        }
        return tracks;
    }

    async function getInvidiousAudioUrl(videoId) {
        for (const instance of INVIDIOUS_INSTANCES) {
            try {
                const res = await fetch(`${instance}/api/v1/videos/${videoId}`, { signal: AbortSignal.timeout(5000) });
                if (!res.ok) continue;
                const data = await res.json();
                // Find the best audio-only adaptive format
                if (data.adaptiveFormats) {
                    const audioFormats = data.adaptiveFormats
                        .filter(f => f.type && f.type.startsWith('audio/'))
                        .sort((a, b) => (b.bitrate || 0) - (a.bitrate || 0));
                    if (audioFormats.length > 0) {
                        return audioFormats[0].url;
                    }
                }
                // Fallback to format streams
                if (data.formatStreams && data.formatStreams.length > 0) {
                    return data.formatStreams[0].url;
                }
            } catch { continue; }
        }
        return null;
    }

    // ── Playback from playlist ──────────────────────────────────────────────
    async function playFromPlaylist(index) {
        if (index < 0 || index >= currentPlaylist.length) return;
        currentIndex = index;
        const track = currentPlaylist[index];

        updateUI(track.title, track.artist, track.cover);
        if (frameDiv) frameDiv.style.display = 'none';

        audio.src = track.url;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = '0:00';
        if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : 'FULL';

        try {
            await audio.play();
            setPlayingState(true);
        } catch (e) {
            console.warn('Autoplay blocked, click play to start:', e);
            setPlayingState(false);
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title;
        if (artistEl) artistEl.textContent = artist;
        if (coverEl && cover) coverEl.src = cover;
    }

    // ── Play / Pause ────────────────────────────────────────────────────────
    function playTrack() {
        audio.play().then(() => setPlayingState(true)).catch(() => setPlayingState(false));
    }

    function pauseTrack() {
        audio.pause();
        setPlayingState(false);
    }

    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? 'bi bi-pause-fill' : 'bi bi-play-fill';
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
    }

    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPlaying) pauseTrack();
            else playTrack();
        });
    }

    audio.addEventListener('play', () => setPlayingState(true));
    audio.addEventListener('pause', () => setPlayingState(false));
    audio.addEventListener('ended', () => {
        // Auto-play next track in playlist
        if (currentPlaylist.length > 0 && currentIndex < currentPlaylist.length - 1) {
            playFromPlaylist(currentIndex + 1);
        } else {
            setPlayingState(false);
        }
    });

    audio.addEventListener('error', () => {
        console.warn('Track failed to load, trying next…');
        // Skip to next track on error
        if (currentPlaylist.length > 0 && currentIndex < currentPlaylist.length - 1) {
            playFromPlaylist(currentIndex + 1);
        }
    });

    // ── Timeline progress ───────────────────────────────────────────────────
    audio.addEventListener('timeupdate', () => {
        if (isUserSeeking) return;
        const current = audio.currentTime || 0;
        const duration = audio.duration || 0;

        if (duration > 0 && !isNaN(duration)) {
            const pct = (current / duration) * 100;
            if (progressBar) progressBar.value = pct;
            if (timeCurrent) timeCurrent.textContent = formatTime(current);
            if (timeTotal) timeTotal.textContent = formatTime(duration);
        } else {
            if (timeCurrent) timeCurrent.textContent = formatTime(current);
        }
    });

    if (progressBar) {
        progressBar.addEventListener('mousedown', () => { isUserSeeking = true; });
        progressBar.addEventListener('touchstart', () => { isUserSeeking = true; });

        progressBar.addEventListener('input', (e) => {
            const pct = parseFloat(e.target.value);
            if (audio.duration && !isNaN(audio.duration)) {
                if (timeCurrent) timeCurrent.textContent = formatTime((pct / 100) * audio.duration);
            }
        });

        const handleSeek = (e) => {
            const pct = parseFloat(e.target.value);
            if (audio.duration && !isNaN(audio.duration)) {
                audio.currentTime = (pct / 100) * audio.duration;
            }
            isUserSeeking = false;
        };
        progressBar.addEventListener('change', handleSeek);
        progressBar.addEventListener('mouseup', handleSeek);
        progressBar.addEventListener('touchend', handleSeek);
    }

    function formatTime(secs) {
        if (isNaN(secs) || secs < 0) return '0:00';
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    // ── Volume ──────────────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            audio.volume = vol;
            localStorage.setItem('sp_vol', vol);
        });
    }

    // ── Search UI ───────────────────────────────────────────────────────────
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchDebounce = setTimeout(() => runSearchUI(query), 350);
        });

        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = searchInput.value.trim();
                if (query.length >= 2) {
                    clearTimeout(searchDebounce);
                    if (searchResults) searchResults.style.display = 'none';
                    searchInput.value = '';
                    searchAndPlay(query);
                }
            }
        });
    }

    async function runSearchUI(query) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding: 16px; text-align: center; color: var(--accent); font-size: 12px;">
            <i class="bi bi-music-note-beamed" style="animation: spin 1s linear infinite;"></i> Searching…
        </div>`;
        searchResults.style.display = 'block';

        const tracks = await fetchTracks(query);

        if (tracks.length === 0) {
            searchResults.innerHTML = `<div style="padding: 16px; text-align: center; color: #71717a; font-size: 12px;">
                No results found. Try different keywords.
            </div>`;
            return;
        }

        searchResults.innerHTML = '';
        tracks.forEach((t, idx) => {
            const item = document.createElement('div');
            item.className = 'sp-search-item';
            const coverUrl = t.cover || 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100&auto=format&fit=crop&q=80';
            const durationStr = t.duration ? formatTime(t.duration) : '';

            item.innerHTML = `
                <img src="${coverUrl}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100&auto=format&fit=crop&q=80'" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${escapeHtml(t.title)}</div>
                    <div class="sp-search-artist">${escapeHtml(t.artist)}${durationStr ? ' · ' + durationStr : ''}</div>
                </div>
                <span class="sp-source-badge ${t.badgeClass}">${t.source}</span>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 18px; flex-shrink: 0; cursor: pointer;"></i>
            `;

            item.addEventListener('click', () => {
                currentPlaylist = tracks;
                playFromPlaylist(idx);
                searchResults.style.display = 'none';
                searchInput.value = '';
            });
            searchResults.appendChild(item);
        });
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // Close search results on outside click
    document.addEventListener('click', (e) => {
        if (searchResults && searchInput && !searchResults.contains(e.target) && !searchInput.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // ── Minimize toggle ─────────────────────────────────────────────────────
    if (minimizeBtn) {
        minimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            playerCard.classList.toggle('minimized');
            const icon = minimizeBtn.querySelector('i');
            if (icon) {
                icon.className = playerCard.classList.contains('minimized') ? 'bi bi-chevron-up' : 'bi bi-chevron-down';
            }
        });
    }

    // ── Ambient rain ────────────────────────────────────────────────────────
    let rainActive = false;
    let audioCtx = null;
    let rainGain = null;

    function initRainSynth() {
        if (audioCtx) return;
        const Ctx = window.AudioContext || window.webkitAudioContext;
        audioCtx = new Ctx();
        const bufferSize = 2 * audioCtx.sampleRate;
        const noiseBuffer = audioCtx.createBuffer(1, bufferSize, audioCtx.sampleRate);
        const output = noiseBuffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) output[i] = Math.random() * 2 - 1;

        const src = audioCtx.createBufferSource();
        src.buffer = noiseBuffer;
        src.loop = true;

        const filter = audioCtx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(750, audioCtx.currentTime);

        rainGain = audioCtx.createGain();
        rainGain.gain.setValueAtTime(0.12, audioCtx.currentTime);

        src.connect(filter);
        filter.connect(rainGain);
        rainGain.connect(audioCtx.destination);
        src.start();
    }

    if (rainBtn) {
        rainBtn.addEventListener('click', () => {
            rainActive = !rainActive;
            rainBtn.classList.toggle('active', rainActive);
            if (rainActive) {
                initRainSynth();
                if (audioCtx && audioCtx.state === 'suspended') audioCtx.resume();
                if (rainGain) rainGain.gain.setValueAtTime(0.15, audioCtx.currentTime);
            } else {
                if (rainGain && audioCtx) rainGain.gain.setValueAtTime(0, audioCtx.currentTime);
            }
        });
    }
}
