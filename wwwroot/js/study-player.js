/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — 100% FULL LENGTH SONGS (NO 30-SECOND LIMITS!)
   - Embedded YouTube Engine for Full Songs (3-5 mins / Full Albums / Streams)
   - Interactive Drag & Click Timeline Seek Bar
   - Non-stop PJAX continuous playback
   ══════════════════════════════════════════════════════════════════════════ */

let ytPlayer = null;
let ytApiReady = false;

// Dynamically inject official YouTube IFrame API script
if (!window.YT) {
    const tag = document.createElement('script');
    tag.src = "https://www.youtube.com/iframe_api";
    const firstScriptTag = document.getElementsByTagName('script')[0];
    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
}

window.onYouTubeIframeAPIReady = function() {
    ytApiReady = true;
    if (window.pendingYtVideoId) {
        initYtPlayer(window.pendingYtVideoId);
    }
};

document.addEventListener('DOMContentLoaded', () => {
    initFloatingStudyPlayer();
});

function initFloatingStudyPlayer() {
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
    const minimizeBtn = document.getElementById('sp-min-btn');

    // Direct Audio Element for 24/7 radio backup
    const streamAudio = new Audio();
    streamAudio.crossOrigin = 'anonymous';

    let isPlaying = false;
    let isYtMode = true;
    let isUserSeeking = false;
    let progressTimer = null;

    // Default Full-Length Preset Tracks (YouTube Video IDs)
    const realPresets = {
        lofi: {
            title: "Lofi Hip Hop Radio (24/7 Live)",
            artist: "Lofi Girl",
            ytId: "jfKfPfyJRdk",
            cover: "https://i.ytimg.com/vi/jfKfPfyJRdk/hqdefault.jpg"
        },
        focus: {
            title: "Deep Focus Ambient Music",
            artist: "Alpha Waves (Full)",
            ytId: "WPni755-Krg",
            cover: "https://i.ytimg.com/vi/WPni755-Krg/hqdefault.jpg"
        },
        piano: {
            title: "Peaceful Solo Piano Study",
            artist: "Relaxing Classical (Full)",
            ytId: "1ZYbU870vMo",
            cover: "https://i.ytimg.com/vi/1ZYbU870vMo/hqdefault.jpg"
        },
        synthwave: {
            title: "Synthwave Chill Beats",
            artist: "Lofi Synthwave (Full)",
            ytId: "4xDzrJKXOOY",
            cover: "https://i.ytimg.com/vi/4xDzrJKXOOY/hqdefault.jpg"
        },
        jazz: {
            title: "Coffee Shop & Jazz Hop",
            artist: "Chill Hop Music (Full)",
            ytId: "5qap5aO4i9A",
            cover: "https://i.ytimg.com/vi/5qap5aO4i9A/hqdefault.jpg"
        }
    };

    // Load initial track
    playFullYtTrack(realPresets.lofi.ytId, realPresets.lofi.title, realPresets.lofi.artist, realPresets.lofi.cover);

    // ── Station Chips ────────────────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const key = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            if (realPresets[key]) {
                playFullYtTrack(realPresets[key].ytId, realPresets[key].title, realPresets[key].artist, realPresets[key].cover);
            } else {
                searchRealFullSongs(key + ' full song');
            }
        });
    });

    // ── YouTube Engine (100% Full Song Playback) ─────────────────────────────
    function playFullYtTrack(ytId, title, artist, cover) {
        isYtMode = true;
        streamAudio.pause();

        if (titleEl) titleEl.textContent = title;
        if (artistEl) artistEl.textContent = artist;
        if (coverEl && cover) coverEl.src = cover;

        if (ytPlayer && typeof ytPlayer.loadVideoById === 'function') {
            ytPlayer.loadVideoById(ytId);
            setPlayingState(true);
        } else {
            window.pendingYtVideoId = ytId;
            initYtPlayer(ytId);
        }

        startTimelineLoop();
    }

    function initYtPlayer(ytId) {
        const targetDiv = document.getElementById('yt-player-element');
        if (!targetDiv) return;

        if (ytPlayer && typeof ytPlayer.destroy === 'function') {
            ytPlayer.destroy();
        }

        ytPlayer = new YT.Player('yt-player-element', {
            height: '140',
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
                'onReady': (e) => {
                    e.target.playVideo();
                    if (volumeSlider) e.target.setVolume(parseFloat(volumeSlider.value) * 100);
                    setPlayingState(true);
                    startTimelineLoop();
                },
                'onStateChange': (e) => {
                    if (e.data === YT.PlayerState.PLAYING) {
                        setPlayingState(true);
                    } else if (e.data === YT.PlayerState.PAUSED || e.data === YT.PlayerState.ENDED) {
                        setPlayingState(false);
                    }
                }
            }
        });
    }

    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
    }

    // ── Play / Pause Button ──────────────────────────────────────────────────
    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isYtMode && ytPlayer) {
                if (isPlaying) {
                    ytPlayer.pauseVideo();
                    setPlayingState(false);
                } else {
                    ytPlayer.playVideo();
                    setPlayingState(true);
                }
            } else {
                if (isPlaying) {
                    streamAudio.pause();
                    setPlayingState(false);
                } else {
                    streamAudio.play().then(() => setPlayingState(true));
                }
            }
        });
    }

    // ── Timeline Progress Loop & Interactive Seek Bar ────────────────────────
    function startTimelineLoop() {
        clearInterval(progressTimer);
        progressTimer = setInterval(updateTimeline, 250);
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
        }
    }

    // Interactive Drag Timeline Slider
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

        const handleSeek = (e) => {
            const pct = parseFloat(e.target.value);
            if (isYtMode && ytPlayer && typeof ytPlayer.getDuration === 'function') {
                const duration = ytPlayer.getDuration() || 0;
                if (duration > 0) {
                    ytPlayer.seekTo((pct / 100) * duration, true);
                }
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
            streamAudio.volume = vol;
            if (ytPlayer && typeof ytPlayer.setVolume === 'function') {
                ytPlayer.setVolume(vol * 100);
            }
        });
    }

    // ── Live YouTube Full Song Search (NO 30s LIMITS!) ────────────────────────
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchDebounce = setTimeout(() => searchRealFullSongs(query), 300);
        });
    }

    async function searchRealFullSongs(query) {
        try {
            // Piped API for real full YouTube songs
            const res = await fetch(`https://pipedapi.kavin.rocks/search?q=${encodeURIComponent(query)}&filter=music_songs`);
            if (res.ok) {
                const data = await res.json();
                if (data.items && data.items.length > 0) {
                    renderSearchItems(data.items.slice(0, 7));
                    return;
                }
            }

            // Fallback Invidious API
            const invRes = await fetch(`https://inv.tux.pizza/api/v1/search?q=${encodeURIComponent(query)}&type=video`);
            if (invRes.ok) {
                const invData = await invRes.json();
                if (invData && invData.length > 0) {
                    renderInvItems(invData.slice(0, 7));
                    return;
                }
            }
        } catch (err) {
            console.error('Search API fallback:', err);
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
                playFullYtTrack(ytId, item.title, item.uploaderName || 'Full Song', item.thumbnail);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function renderInvItems(items) {
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
                playFullYtTrack(item.videoId, item.title, item.author || 'Full Song', item.videoThumbnails[0]?.url);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    // Close search results when clicking outside
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
