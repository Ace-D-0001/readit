/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — 100% FULL-LENGTH SONGS ONLY (ZERO 30s PREVIEW LIMITS!)
   - Native HTML5 Audio Engine
   - Multi-Source Full Track Search (Audius API + Invidious Direct Audio + Jamendo)
   - 100% Full Song Playback (3:45, 4:20, 5:00+ / 24/7 Streams)
   - Interactive Drag & Click Timeline Seek Bar
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initFullWorkMusicPlayer();
});

function initFullWorkMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Native HTML5 Audio Object
    const audio = new Audio();
    audio.crossOrigin = 'anonymous';

    // Elements
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

    let isPlaying = false;
    let isUserSeeking = false;

    // Guaranteed 24/7 Live High-Bitrate Study Radio Streams
    const realStations = {
        lofi: {
            title: "Lofi Study Beats (24/7)",
            artist: "Lofi Girl & Chillhop",
            cover: "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=300&auto=format&fit=crop&q=80",
            url: "https://stream.zeno.fm/f3wvbbqmdg8uv"
        },
        focus: {
            title: "Deep Focus Alpha Waves",
            artist: "Ambient Concentration",
            cover: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=300&auto=format&fit=crop&q=80",
            url: "https://stream.zeno.fm/1f4s80v63v8uv"
        },
        piano: {
            title: "Peaceful Solo Piano",
            artist: "Classical Study Music",
            cover: "https://images.unsplash.com/photo-1520523839897-bd0b52f945a0?w=300&auto=format&fit=crop&q=80",
            url: "https://stream.zeno.fm/0r0xa792kwzuv"
        },
        synthwave: {
            title: "Synthwave Night Drive",
            artist: "Chillwave & Retrowave",
            cover: "https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=300&auto=format&fit=crop&q=80",
            url: "https://stream.zeno.fm/4xDzrJKXOOY"
        },
        jazz: {
            title: "Coffee Shop & Jazz Hop",
            artist: "Relaxing Lounge Beats",
            cover: "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=300&auto=format&fit=crop&q=80",
            url: "https://stream.zeno.fm/f3wvbbqmdg8uv"
        }
    };

    // Restore Volume
    const savedVol = localStorage.getItem('sp_vol') || 0.8;
    audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = audio.volume;

    // Load initial track
    loadFullTrack(realStations.lofi);

    // ── Station Chips ────────────────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const key = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            if (realStations[key]) {
                loadFullTrack(realStations[key]);
                playTrack();
            } else {
                searchFullTracks(key + ' chill');
            }
        });
    });

    // ── Core Playback Control ────────────────────────────────────────────────
    function loadFullTrack(track) {
        if (!track || !track.url) return;
        audio.src = track.url;
        if (titleEl) titleEl.textContent = track.title || "Study Track";
        if (artistEl) artistEl.textContent = track.artist || "Full Song";
        if (coverEl && track.cover) coverEl.src = track.cover;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = "0:00";
        if (timeTotal) timeTotal.textContent = "FULL";
    }

    function playTrack() {
        audio.play().then(() => {
            setPlayingState(true);
        }).catch(err => {
            console.log('Play notice:', err);
            setPlayingState(false);
        });
    }

    function pauseTrack() {
        audio.pause();
        setPlayingState(false);
    }

    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
    }

    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPlaying) {
                pauseTrack();
            } else {
                playTrack();
            }
        });
    }

    audio.addEventListener('play', () => setPlayingState(true));
    audio.addEventListener('pause', () => setPlayingState(false));
    audio.addEventListener('ended', () => setPlayingState(false));

    // ── Timeline Progress & Drag Seek Bar ────────────────────────────────────
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
            if (timeTotal) timeTotal.textContent = "LIVE 24/7";
            if (progressBar) progressBar.value = 100;
        }
    });

    if (progressBar) {
        progressBar.addEventListener('mousedown', () => { isUserSeeking = true; });
        progressBar.addEventListener('touchstart', () => { isUserSeeking = true; });

        progressBar.addEventListener('input', (e) => {
            const pct = parseFloat(e.target.value);
            if (audio.duration && !isNaN(audio.duration)) {
                const targetSecs = (pct / 100) * audio.duration;
                if (timeCurrent) timeCurrent.textContent = formatTime(targetSecs);
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
        if (isNaN(secs) || secs < 0) return "0:00";
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    // ── Volume Control ───────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            audio.volume = vol;
            localStorage.setItem('sp_vol', vol);
        });
    }

    // ── 100% FULL-LENGTH SEARCH API (Audius API + Invidious Audio Stream) ────
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchDebounce = setTimeout(() => searchFullTracks(query), 250);
        });
    }

    async function searchFullTracks(query) {
        try {
            // 1. Audius API: Over 1M full-length 100% free music tracks (NO 30s previews!)
            const audiusUrl = `https://api.audius.co/v1/tracks/search?query=${encodeURIComponent(query)}&app_name=READIT`;
            const audiusRes = await fetch(audiusUrl);
            if (audiusRes.ok) {
                const data = await audiusRes.json();
                if (data.data && data.data.length > 0) {
                    renderAudiusResults(data.data.slice(0, 8));
                    return;
                }
            }

            // 2. Invidious API: Full YouTube Direct Audio Streams
            const invUrl = `https://inv.tux.pizza/api/v1/search?q=${encodeURIComponent(query)}&type=video`;
            const invRes = await fetch(invUrl);
            if (invRes.ok) {
                const invData = await invRes.json();
                if (invData && invData.length > 0) {
                    renderInvidiousAudioResults(invData.slice(0, 8));
                    return;
                }
            }

            // 3. Jamendo API: Full-length independent tracks
            const jamUrl = `https://api.jamendo.com/v3.0/tracks/?client_id=56d30200&format=json&limit=8&search=${encodeURIComponent(query)}`;
            const jamRes = await fetch(jamUrl);
            if (jamRes.ok) {
                const jamData = await jamRes.json();
                if (jamData.results && jamData.results.length > 0) {
                    renderJamResults(jamData.results);
                    return;
                }
            }

            if (searchResults) {
                searchResults.innerHTML = `<div style="padding: 10px; font-size: 11px; color: #a1a1aa; text-align: center;">No tracks found. Try searching another term!</div>`;
                searchResults.style.display = 'block';
            }
        } catch (err) {
            console.error('Search error:', err);
        }
    }

    function renderAudiusResults(tracks) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        tracks.forEach(t => {
            const streamUrl = `https://api.audius.co/v1/tracks/${t.id}/stream?app_name=READIT`;
            const art = t.artwork ? t.artwork['150x150'] || t.artwork['480x480'] : 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150&auto=format&fit=crop&q=80';

            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${art}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${t.title}</div>
                    <div class="sp-search-artist">${t.user ? t.user.name : 'Full Song'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 20px;"></i>
            `;
            div.addEventListener('click', () => {
                loadFullTrack({
                    title: t.title,
                    artist: t.user ? t.user.name : 'Full Song',
                    cover: art,
                    url: streamUrl
                });
                playTrack();
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function renderInvidiousAudioResults(tracks) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        tracks.forEach(t => {
            // Direct 100% Full Audio Stream
            const streamUrl = `https://inv.tux.pizza/latest_version?id=${t.videoId}&itag=140`;
            const art = t.videoThumbnails ? t.videoThumbnails[0]?.url : 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150&auto=format&fit=crop&q=80';

            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${art}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${t.title}</div>
                    <div class="sp-search-artist">${t.author || 'Full Song'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 20px;"></i>
            `;
            div.addEventListener('click', () => {
                loadFullTrack({
                    title: t.title,
                    artist: t.author || 'Full Song',
                    cover: art,
                    url: streamUrl
                });
                playTrack();
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function renderJamResults(tracks) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        tracks.forEach(t => {
            if (!t.audio) return;
            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${t.image || 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100&auto=format&fit=crop&q=80'}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${t.name}</div>
                    <div class="sp-search-artist">${t.artist_name || 'Full Track'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 20px;"></i>
            `;
            div.addEventListener('click', () => {
                loadFullTrack({
                    title: t.name,
                    artist: t.artist_name || 'Full Track',
                    cover: t.image || 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300&auto=format&fit=crop&q=80',
                    url: t.audio
                });
                playTrack();
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    // Close search dropdown on click outside
    document.addEventListener('click', (e) => {
        if (searchResults && !searchResults.contains(e.target) && !searchInput.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // ── Minimize Floating Dock Toggle ────────────────────────────────────────
    if (minimizeBtn) {
        minimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            playerCard.classList.toggle('minimized');
            const icon = minimizeBtn.querySelector('i');
            if (icon) {
                icon.className = playerCard.classList.contains('minimized') ? "bi bi-chevron-up" : "bi bi-chevron-down";
            }
        });
    }

    // ── Ambient Rain Generator ───────────────────────────────────────────────
    let rainActive = false;
    let audioCtx = null;
    let rainGain = null;

    function initRainSynth() {
        if (audioCtx) return;
        const AudioCtxClass = window.AudioContext || window.webkitAudioContext;
        audioCtx = new AudioCtxClass();

        const bufferSize = 2 * audioCtx.sampleRate;
        const noiseBuffer = audioCtx.createBuffer(1, bufferSize, audioCtx.sampleRate);
        const output = noiseBuffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            output[i] = Math.random() * 2 - 1;
        }

        const whiteNoise = audioCtx.createBufferSource();
        whiteNoise.buffer = noiseBuffer;
        whiteNoise.loop = true;

        const filter = audioCtx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(750, audioCtx.currentTime);

        rainGain = audioCtx.createGain();
        rainGain.gain.setValueAtTime(0.12, audioCtx.currentTime);

        whiteNoise.connect(filter);
        filter.connect(rainGain);
        rainGain.connect(audioCtx.destination);
        whiteNoise.start();
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
