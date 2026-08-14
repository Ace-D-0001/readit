/* ══════════════════════════════════════════════════════════════════════════
   REAL FULL MUSIC PLAYER (Right Hand Side / RHS Column)
   - 100% FULL-LENGTH SONGS (No 30-second limits!)
   - Interactive Time Progress Bar & Seek Slider (Drag to jump to any part of song!)
   - Real YouTube IFrame API & Piped/Invidious Search
   - Rain Sound Layer Generator
   ══════════════════════════════════════════════════════════════════════════ */

let ytPlayer = null;
let ytApiReady = false;

// Load YouTube IFrame API script dynamically
if (!window.YT) {
    const tag = document.createElement('script');
    tag.src = "https://www.youtube.com/iframe_api";
    const firstScriptTag = document.getElementsByTagName('script')[0];
    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
}

window.onYouTubeIframeAPIReady = function() {
    ytApiReady = true;
    if (window.pendingYtVideoId) {
        createYtPlayer(window.pendingYtVideoId);
    }
};

document.addEventListener('DOMContentLoaded', () => {
    initRealMusicPlayer();
});

function initRealMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

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
    const cafeBtn = document.getElementById('sp-cafe-btn');

    // Audio fallback for streams
    const mp3Audio = new Audio();
    mp3Audio.crossOrigin = 'anonymous';

    let isPlaying = false;
    let isYtMode = true;
    let updateTimer = null;
    let isUserSeeking = false;

    // Preset Tracks
    const presets = {
        lofi:      { ytId: "jfKfPfyJRdk", title: "Lofi Hip Hop Radio (24/7)", artist: "Lofi Girl" },
        focus:     { ytId: "WPni755-Krg", title: "Deep Focus Ambient Waves", artist: "Alpha Waves" },
        piano:     { ytId: "1ZYbU870vMo", title: "Peaceful Solo Piano Study", artist: "Relaxing Classical" },
        synthwave: { ytId: "4xDzrJKXOOY", title: "Synthwave Chillwave Radio", artist: "Lofi Girl Synth" },
        jazz:      { ytId: "5qap5aO4i9A", title: "Lofi Cafe & Jazz Hop", artist: "Chill Study Beats" }
    };

    // Load initial track
    playYtSong(presets.lofi.ytId, presets.lofi.title, presets.lofi.artist);

    // ── Station Chips ────────────────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const key = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            if (presets[key]) {
                playYtSong(presets[key].ytId, presets[key].title, presets[key].artist);
            } else {
                searchRealSongs(key + ' full song study');
            }
        });
    });

    // ── YouTube Player Core Engine ───────────────────────────────────────────
    function playYtSong(ytId, title, artist, thumbnail) {
        isYtMode = true;
        mp3Audio.pause();

        if (titleEl) titleEl.textContent = title;
        if (artistEl) artistEl.textContent = artist;
        if (coverEl && thumbnail) coverEl.src = thumbnail;

        if (ytPlayer && typeof ytPlayer.loadVideoById === 'function') {
            ytPlayer.loadVideoById(ytId);
            isPlaying = true;
            updatePlayUi(true);
        } else {
            window.pendingYtVideoId = ytId;
            createYtPlayer(ytId);
        }

        startProgressLoop();
    }

    function createYtPlayer(ytId) {
        const frameDiv = document.getElementById('yt-player-element');
        if (!frameDiv) return;

        if (ytPlayer && typeof ytPlayer.destroy === 'function') {
            ytPlayer.destroy();
        }

        ytPlayer = new YT.Player('yt-player-element', {
            height: '160',
            width: '100%',
            videoId: ytId,
            playerVars: {
                'autoplay': 1,
                'controls': 1,
                'modestbranding': 1,
                'rel': 0,
                'origin': window.location.origin
            },
            events: {
                'onReady': onPlayerReady,
                'onStateChange': onPlayerStateChange
            }
        });
    }

    function onPlayerReady(event) {
        event.target.playVideo();
        if (volumeSlider) {
            event.target.setVolume(parseFloat(volumeSlider.value) * 100);
        }
        isPlaying = true;
        updatePlayUi(true);
        startProgressLoop();
    }

    function onPlayerStateChange(event) {
        if (event.data === YT.PlayerState.PLAYING) {
            isPlaying = true;
            updatePlayUi(true);
        } else if (event.data === YT.PlayerState.PAUSED || event.data === YT.PlayerState.ENDED) {
            isPlaying = false;
            updatePlayUi(false);
        }
    }

    // ── Real Progress Slider & Seek Control ───────────────────────────────────
    function startProgressLoop() {
        clearInterval(updateTimer);
        updateTimer = setInterval(updateTimeline, 300);
    }

    function updateTimeline() {
        if (isUserSeeking) return;

        if (isYtMode && ytPlayer && typeof ytPlayer.getCurrentTime === 'function') {
            const current = ytPlayer.getCurrentTime() || 0;
            const duration = ytPlayer.getDuration() || 0;

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
        } else if (!isYtMode && mp3Audio) {
            const current = mp3Audio.currentTime || 0;
            const duration = mp3Audio.duration || 0;
            if (duration > 0 && !isNaN(duration)) {
                const pct = (current / duration) * 100;
                if (progressBar) progressBar.value = pct;
                if (timeCurrent) timeCurrent.textContent = formatTime(current);
                if (timeTotal) timeTotal.textContent = formatTime(duration);
            }
        }
    }

    // User dragging or clicking progress bar to SEEK!
    if (progressBar) {
        progressBar.addEventListener('mousedown', () => { isUserSeeking = true; });
        progressBar.addEventListener('touchstart', () => { isUserSeeking = true; });

        progressBar.addEventListener('input', (e) => {
            const pct = parseFloat(e.target.value);
            if (isYtMode && ytPlayer && typeof ytPlayer.getDuration === 'function') {
                const duration = ytPlayer.getDuration() || 0;
                if (duration > 0) {
                    const targetSecs = (pct / 100) * duration;
                    if (timeCurrent) timeCurrent.textContent = formatTime(targetSecs);
                }
            }
        });

        const performSeek = (e) => {
            const pct = parseFloat(e.target.value);
            if (isYtMode && ytPlayer && typeof ytPlayer.getDuration === 'function') {
                const duration = ytPlayer.getDuration() || 0;
                if (duration > 0) {
                    const targetSecs = (pct / 100) * duration;
                    ytPlayer.seekTo(targetSecs, true);
                }
            } else if (!isYtMode && mp3Audio.duration) {
                mp3Audio.currentTime = (pct / 100) * mp3Audio.duration;
            }
            isUserSeeking = false;
        };

        progressBar.addEventListener('change', performSeek);
        progressBar.addEventListener('mouseup', performSeek);
        progressBar.addEventListener('touchend', performSeek);
    }

    function formatTime(secs) {
        if (isNaN(secs) || secs < 0) return "0:00";
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    // ── Play / Pause Button ──────────────────────────────────────────────────
    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isYtMode && ytPlayer) {
                if (isPlaying) {
                    ytPlayer.pauseVideo();
                    isPlaying = false;
                    updatePlayUi(false);
                } else {
                    ytPlayer.playVideo();
                    isPlaying = true;
                    updatePlayUi(true);
                }
            } else {
                if (isPlaying) {
                    mp3Audio.pause();
                    isPlaying = false;
                    updatePlayUi(false);
                } else {
                    mp3Audio.play();
                    isPlaying = true;
                    updatePlayUi(true);
                }
            }
        });
    }

    function updatePlayUi(playing) {
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
    }

    // ── Volume Control ───────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            mp3Audio.volume = vol;
            if (ytPlayer && typeof ytPlayer.setVolume === 'function') {
                ytPlayer.setVolume(vol * 100);
            }
        });
    }

    // ── Live YouTube Song Search (Full Songs) ────────────────────────────────
    let searchTimeout = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchTimeout = setTimeout(() => searchRealSongs(query), 300);
        });
    }

    async function searchRealSongs(query) {
        try {
            // Piped / Invidious API for real YouTube song search
            const pipedRes = await fetch(`https://pipedapi.kavin.rocks/search?q=${encodeURIComponent(query)}&filter=music_songs`);
            if (pipedRes.ok) {
                const pipedData = await pipedRes.json();
                if (pipedData.items && pipedData.items.length > 0) {
                    renderSearchItems(pipedData.items.slice(0, 7));
                    return;
                }
            }

            // Fallback Invidious Instance
            const invRes = await fetch(`https://inv.tux.pizza/api/v1/search?q=${encodeURIComponent(query)}&type=video`);
            if (invRes.ok) {
                const invData = await invRes.json();
                if (invData && invData.length > 0) {
                    renderInvResults(invData.slice(0, 7));
                    return;
                }
            }
        } catch (err) {
            console.error('Search API notice:', err);
        }
    }

    function renderSearchItems(items) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        items.forEach(item => {
            const ytId = item.url ? item.url.replace('/watch?v=', '') : '';
            if (!ytId) return;

            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${item.thumbnail}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${item.title}</div>
                    <div class="sp-search-artist">${item.uploaderName || 'Full Song'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 18px;"></i>
            `;
            div.addEventListener('click', () => {
                playYtSong(ytId, item.title, item.uploaderName || 'Full Song', item.thumbnail);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function renderInvResults(items) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        items.forEach(item => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${item.videoThumbnails ? item.videoThumbnails[0]?.url : ''}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${item.title}</div>
                    <div class="sp-search-artist">${item.author || 'Full Song'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 18px;"></i>
            `;
            div.addEventListener('click', () => {
                playYtSong(item.videoId, item.title, item.author || 'Full Song', item.videoThumbnails[0]?.url);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    // Close search dropdown when clicking outside
    document.addEventListener('click', (e) => {
        if (searchResults && !searchResults.contains(e.target) && !searchInput.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // ── Rain Ambient Sound Layer ─────────────────────────────────────────────
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

    if (cafeBtn) {
        cafeBtn.addEventListener('click', () => {
            playYtSong("4xDzrJKXOOY", "Synthwave Chillwave Radio (24/7)", "Lofi Girl");
        });
    }
}
