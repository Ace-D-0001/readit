/* ══════════════════════════════════════════════════════════════════════════
   REAL FULL MUSIC PLAYER (Right Hand Side / RHS Column)
   - Real YouTube IFrame & Live Audio Player for 100% FULL Song Streaming
   - Search ANY Real Song or Artist (e.g. Coldplay, Hans Zimmer, Lofi Girl, Taylor Swift)
   - Real 24/7 Live Study Radio Streams
   - Rain & Ambient Sound Generator
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initRealMusicPlayer();
});

let ytPlayer = null;

function initRealMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Direct MP3 Audio Player for Live Radio Streams
    const mp3Audio = new Audio();
    mp3Audio.crossOrigin = 'anonymous';

    const ambientAudio = new Audio();
    ambientAudio.loop = true;

    // Elements
    const playBtn = document.getElementById('sp-play-btn');
    const playIcon = document.getElementById('sp-play-icon');
    const titleEl = document.getElementById('sp-title');
    const artistEl = document.getElementById('sp-artist');
    const coverEl = document.getElementById('sp-cover');
    const volumeSlider = document.getElementById('sp-volume');
    const searchInput = document.getElementById('sp-search-input');
    const searchResults = document.getElementById('sp-search-results');
    const eqBars = document.querySelectorAll('.sp-eq-bar');
    const rainBtn = document.getElementById('sp-rain-btn');
    const cafeBtn = document.getElementById('sp-cafe-btn');
    const ytContainer = document.getElementById('sp-yt-frame');

    // Preset Real 24/7 Live Radio & YouTube Tracks
    const realPresetTracks = [
        {
            title: "Lofi Hip Hop Radio — Beats to Relax/Study to",
            artist: "Lofi Girl (Live 24/7)",
            ytId: "jfKfPfyJRdk",
            cover: "https://i.ytimg.com/vi/jfKfPfyJRdk/hqdefault.jpg"
        },
        {
            title: "Synthwave Radio — Chill Beats",
            artist: "Lofi Girl Synthwave",
            ytId: "4xDzrJKXOOY",
            cover: "https://i.ytimg.com/vi/4xDzrJKXOOY/hqdefault.jpg"
        },
        {
            title: "Deep Focus Concentration Music",
            artist: "Alpha Waves Ambient",
            ytId: "WPni755-Krg",
            cover: "https://i.ytimg.com/vi/WPni755-Krg/hqdefault.jpg"
        },
        {
            title: "Peaceful Piano Study Music",
            artist: "Relaxing Classical",
            ytId: "1ZYbU870vMo",
            cover: "https://i.ytimg.com/vi/1ZYbU870vMo/hqdefault.jpg"
        }
    ];

    let currentTrack = realPresetTracks[0];
    let isPlaying = false;
    let isYtMode = true;

    // Load saved volume
    const savedVol = localStorage.getItem('study_player_volume') || 0.8;
    mp3Audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = mp3Audio.volume;

    // Load default YouTube track
    loadYtTrack(currentTrack.ytId, currentTrack.title, currentTrack.artist, currentTrack.cover);

    // ── Station Chips ────────────────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const query = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            if (query === 'lofi') searchRealSongs('Lofi Girl live beats');
            else if (query === 'focus') searchRealSongs('Deep focus study music ambient');
            else if (query === 'piano') searchRealSongs('Peaceful piano study music');
            else if (query === 'synthwave') searchRealSongs('Synthwave chillwave study');
            else if (query === 'jazz') searchRealSongs('Coffee shop jazz hop study');
        });
    });

    // ── Load YouTube Full Song ────────────────────────────────────────────────
    function loadYtTrack(ytId, title, artist, cover) {
        isYtMode = true;
        mp3Audio.pause();
        currentTrack = { ytId, title, artist, cover };

        titleEl.textContent = title;
        artistEl.textContent = artist;
        if (coverEl && cover) coverEl.src = cover;

        if (ytContainer) {
            ytContainer.innerHTML = `<iframe id="yt-player-iframe" src="https://www.youtube-nocookie.com/embed/${ytId}?autoplay=1&enablejsapi=1&origin=${encodeURIComponent(window.location.origin)}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen style="width: 100%; height: 160px; border-radius: 8px;"></iframe>`;
        }

        isPlaying = true;
        if (playIcon) playIcon.className = "bi bi-pause-fill";
        eqBars.forEach(bar => bar.classList.add('playing'));
    }

    // ── Play / Pause Toggle ──────────────────────────────────────────────────
    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isYtMode && ytContainer) {
                const iframe = document.getElementById('yt-player-iframe');
                if (isPlaying) {
                    if (iframe) iframe.contentWindow.postMessage('{"event":"command","func":"pauseVideo","args":""}', '*');
                    isPlaying = false;
                    if (playIcon) playIcon.className = "bi bi-play-fill";
                    eqBars.forEach(bar => bar.classList.remove('playing'));
                } else {
                    if (iframe) iframe.contentWindow.postMessage('{"event":"command","func":"playVideo","args":""}', '*');
                    isPlaying = true;
                    if (playIcon) playIcon.className = "bi bi-pause-fill";
                    eqBars.forEach(bar => bar.classList.add('playing'));
                }
            } else {
                if (isPlaying) {
                    mp3Audio.pause();
                    isPlaying = false;
                    if (playIcon) playIcon.className = "bi bi-play-fill";
                    eqBars.forEach(bar => bar.classList.remove('playing'));
                } else {
                    mp3Audio.play();
                    isPlaying = true;
                    if (playIcon) playIcon.className = "bi bi-pause-fill";
                    eqBars.forEach(bar => bar.classList.add('playing'));
                }
            }
        });
    }

    // ── Volume Slider ────────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            mp3Audio.volume = vol;
            localStorage.setItem('study_player_volume', vol);
            const iframe = document.getElementById('yt-player-iframe');
            if (iframe) {
                iframe.contentWindow.postMessage(`{"event":"command","func":"setVolume","args":[${vol * 100}]}`, '*');
            }
        });
    }

    // ── Real Song Search via Invidious / Piped / iTunes API ──────────────────
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
            // Fetch real song results using Invidious / Piped API for real full YouTube songs
            const pipedRes = await fetch(`https://pipedapi.kavin.rocks/search?q=${encodeURIComponent(query)}&filter=music_songs`);
            if (pipedRes.ok) {
                const pipedData = await pipedRes.json();
                if (pipedData.items && pipedData.items.length > 0) {
                    renderPipedResults(pipedData.items.slice(0, 6));
                    return;
                }
            }

            // Fallback: iTunes Search API
            const itunesRes = await fetch(`https://itunes.apple.com/search?term=${encodeURIComponent(query)}&entity=song&limit=6`);
            const itunesData = await itunesRes.json();
            if (itunesData.results && itunesData.results.length > 0) {
                renderItunesResults(itunesData.results);
            } else if (searchResults) {
                searchResults.innerHTML = `<div style="padding: 8px; font-size: 11px; color: #a1a1aa;">No results found.</div>`;
                searchResults.style.display = 'block';
            }
        } catch (err) {
            // Backup iTunes API search
            try {
                const fallback = await fetch(`https://itunes.apple.com/search?term=${encodeURIComponent(query)}&entity=song&limit=6`);
                const fallbackData = await fallback.json();
                if (fallbackData.results) renderItunesResults(fallbackData.results);
            } catch (e) {
                console.error('Search error:', e);
            }
        }
    }

    function renderPipedResults(items) {
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
                    <div class="sp-search-artist">${item.uploaderName || 'YouTube Music'}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 18px;"></i>
            `;
            div.addEventListener('click', () => {
                loadYtTrack(ytId, item.title, item.uploaderName || 'Full Song', item.thumbnail);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function renderItunesResults(tracks) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        tracks.forEach(t => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';
            div.innerHTML = `
                <img src="${t.artworkUrl60 || t.artworkUrl100}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${t.trackName}</div>
                    <div class="sp-search-artist">${t.artistName}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 18px;"></i>
            `;
            div.addEventListener('click', () => {
                // Play song via iTunes audio preview or search YouTube
                if (t.previewUrl) {
                    isYtMode = false;
                    if (ytContainer) ytContainer.innerHTML = '';
                    mp3Audio.src = t.previewUrl;
                    mp3Audio.play();
                    titleEl.textContent = t.trackName;
                    artistEl.textContent = t.artistName;
                    if (coverEl) coverEl.src = t.artworkUrl100 ? t.artworkUrl100.replace('100x100bb', '300x300bb') : t.artworkUrl60;
                    isPlaying = true;
                    if (playIcon) playIcon.className = "bi bi-pause-fill";
                    eqBars.forEach(bar => bar.classList.add('playing'));
                }
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
        filter.frequency.setValueAtTime(700, audioCtx.currentTime);

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
            loadYtTrack("4xDzrJKXOOY", "Synthwave Night Radio", "Lofi Girl 24/7", "https://i.ytimg.com/vi/4xDzrJKXOOY/hqdefault.jpg");
        });
    }
}
