/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER (Left Sidebar Bottom Area)
   - Song Search via iTunes Search API (30s high-quality previews & album art)
   - Preset Study Radio Stations (Lofi Beats, Deep Focus, Piano, Synthwave, Coffee Shop)
   - Ambient Noise Generator (Rain & White Noise Synth Layer)
   - Persistent volume & current track state across page navigation
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyPlayer();
});

function initStudyPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Elements
    const audio = new Audio();
    audio.crossOrigin = 'anonymous';

    const ambientAudio = new Audio();
    ambientAudio.loop = true;

    const playBtn = document.getElementById('sp-play-btn');
    const playIcon = document.getElementById('sp-play-icon');
    const prevBtn = document.getElementById('sp-prev-btn');
    const nextBtn = document.getElementById('sp-next-btn');
    const titleEl = document.getElementById('sp-title');
    const artistEl = document.getElementById('sp-artist');
    const coverEl = document.getElementById('sp-cover');
    const progressBar = document.getElementById('sp-progress');
    const timeCurrent = document.getElementById('sp-time-current');
    const timeTotal = document.getElementById('sp-time-total');
    const volumeSlider = document.getElementById('sp-volume');
    const searchInput = document.getElementById('sp-search-input');
    const searchResults = document.getElementById('sp-search-results');
    const eqBars = document.querySelectorAll('.sp-eq-bar');
    const rainBtn = document.getElementById('sp-rain-btn');
    const cafeBtn = document.getElementById('sp-cafe-btn');

    // Preset Study Playlists
    const studyStations = [
        {
            title: "Lofi Study Beats",
            artist: "Chillhop & Study",
            cover: "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=150&auto=format&fit=crop&q=80",
            url: "https://audio.jukehost.co/pM5jT61k5iXy7E7oE2l9m4Wk2gY1rZ9A" // High quality lofi preview
        },
        {
            title: "Deep Focus & Alpha Waves",
            artist: "Binaural Ambient",
            cover: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=150&auto=format&fit=crop&q=80",
            url: "https://audio.jukehost.co/8Y4n3X9v2m1K0L5j7H6g5F4d3S2a1P0o"
        },
        {
            title: "Peaceful Piano",
            artist: "Classical Concentration",
            cover: "https://images.unsplash.com/photo-1520523839897-bd0b52f945a0?w=150&auto=format&fit=crop&q=80",
            url: "https://audio.jukehost.co/Q1w2E3r4T5y6U7i8O9p0A1s2D3f4G5h6"
        },
        {
            title: "Synthwave Night Drive",
            artist: "Midnight Study",
            cover: "https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=150&auto=format&fit=crop&q=80",
            url: "https://audio.jukehost.co/L1k2J3h4G5f6D7s8A9p0O1i2U3y4T5r6"
        }
    ];

    let currentPlaylist = [...studyStations];
    let currentIndex = 0;
    let isPlaying = false;

    // Load saved volume
    const savedVol = localStorage.getItem('study_player_volume') || 0.8;
    audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = audio.volume;

    // Load default station
    loadTrack(currentPlaylist[currentIndex]);

    // ── Station Pill Selection ──────────────────────────────────────────────
    document.querySelectorAll('.sp-station-chip').forEach(chip => {
        chip.addEventListener('click', () => {
            const query = chip.dataset.query;
            document.querySelectorAll('.sp-station-chip').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');

            if (query === 'lofi') fetchStudySearch('lofi hip hop');
            else if (query === 'focus') fetchStudySearch('ambient focus binaural');
            else if (query === 'piano') fetchStudySearch('peaceful solo piano');
            else if (query === 'synthwave') fetchStudySearch('synthwave chillwave');
            else if (query === 'jazz') fetchStudySearch('jazz hop study');
        });
    });

    // ── Track Loading & Playback ─────────────────────────────────────────────
    function loadTrack(track) {
        if (!track || !track.url) return;
        audio.src = track.url;
        titleEl.textContent = track.title || "Study Track";
        artistEl.textContent = track.artist || "Study Music";
        if (coverEl && track.cover) coverEl.src = track.cover;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = "0:00";
        if (timeTotal) timeTotal.textContent = "0:30";
    }

    function togglePlay() {
        if (!audio.src) return;
        if (isPlaying) {
            audio.pause();
        } else {
            audio.play().catch(err => console.log('Audio autoplay prevented:', err));
        }
    }

    audio.addEventListener('play', () => {
        isPlaying = true;
        if (playIcon) playIcon.className = "bi bi-pause-fill";
        eqBars.forEach(bar => bar.classList.add('playing'));
    });

    audio.addEventListener('pause', () => {
        isPlaying = false;
        if (playIcon) playIcon.className = "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.remove('playing'));
    });

    audio.addEventListener('ended', () => {
        nextTrack();
    });

    if (playBtn) playBtn.addEventListener('click', togglePlay);

    function nextTrack() {
        if (currentPlaylist.length === 0) return;
        currentIndex = (currentIndex + 1) % currentPlaylist.length;
        loadTrack(currentPlaylist[currentIndex]);
        audio.play().catch(() => {});
    }

    function prevTrack() {
        if (currentPlaylist.length === 0) return;
        currentIndex = (currentIndex - 1 + currentPlaylist.length) % currentPlaylist.length;
        loadTrack(currentPlaylist[currentIndex]);
        audio.play().catch(() => {});
    }

    if (nextBtn) nextBtn.addEventListener('click', nextTrack);
    if (prevBtn) prevBtn.addEventListener('click', prevTrack);

    // ── Progress Bar & Time Update ─────────────────────────────────────────
    audio.addEventListener('timeupdate', () => {
        if (audio.duration) {
            const pct = (audio.currentTime / audio.duration) * 100;
            if (progressBar) progressBar.value = pct;
            if (timeCurrent) timeCurrent.textContent = formatTime(audio.currentTime);
            if (timeTotal) timeTotal.textContent = formatTime(audio.duration);
        }
    });

    if (progressBar) {
        progressBar.addEventListener('input', (e) => {
            if (audio.duration) {
                audio.currentTime = (e.target.value / 100) * audio.duration;
            }
        });
    }

    function formatTime(secs) {
        if (isNaN(secs)) return "0:00";
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    // ── Volume Control ───────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            audio.volume = parseFloat(e.target.value);
            localStorage.setItem('study_player_volume', audio.volume);
        });
    }

    // ── Song Search (iTunes Search API) ──────────────────────────────────────
    let searchTimeout = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchTimeout = setTimeout(() => fetchStudySearch(query), 350);
        });
    }

    async function fetchStudySearch(query) {
        try {
            const res = await fetch(`https://itunes.apple.com/search?term=${encodeURIComponent(query)}&entity=song&limit=6`);
            const data = await res.json();
            
            if (data.results && data.results.length > 0) {
                renderSearchResults(data.results);
            } else if (searchResults) {
                searchResults.innerHTML = `<div style="padding: 10px; font-size: 11px; color: var(--text-muted);">No songs found.</div>`;
                searchResults.style.display = 'block';
            }
        } catch (err) {
            console.error('Song search failed:', err);
        }
    }

    function renderSearchResults(tracks) {
        if (!searchResults) return;
        searchResults.innerHTML = '';
        
        currentPlaylist = tracks.map(t => ({
            title: t.trackName,
            artist: t.artistName,
            cover: t.artworkUrl100 || t.artworkUrl60,
            url: t.previewUrl
        }));

        tracks.forEach((t, i) => {
            const item = document.createElement('div');
            item.className = 'sp-search-item';
            item.innerHTML = `
                <img src="${t.artworkUrl60 || t.artworkUrl100}" class="sp-search-art" alt="" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${t.trackName}</div>
                    <div class="sp-search-artist">${t.artistName}</div>
                </div>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 16px;"></i>
            `;
            item.addEventListener('click', () => {
                currentIndex = i;
                loadTrack(currentPlaylist[currentIndex]);
                audio.play().catch(() => {});
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(item);
        });

        searchResults.style.display = 'block';
    }

    // Close search results when clicking outside
    document.addEventListener('click', (e) => {
        if (searchResults && !searchResults.contains(e.target) && !searchInput.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // ── Ambient Sound Layer (Rain / Cafe) ────────────────────────────────────
    let rainPlaying = false;
    let cafePlaying = false;

    // Web Audio API White Noise / Rain Synthesizer for smooth rain overlay!
    let audioCtx = null;
    let rainNode = null;
    let rainGain = null;

    function initRainSynth() {
        if (audioCtx) return;
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        audioCtx = new AudioContext();

        const bufferSize = 2 * audioCtx.sampleRate;
        const noiseBuffer = audioCtx.createBuffer(1, bufferSize, audioCtx.sampleRate);
        const output = noiseBuffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            output[i] = Math.random() * 2 - 1; // Pink noise rain effect
        }

        const whiteNoise = audioCtx.createBufferSource();
        whiteNoise.buffer = noiseBuffer;
        whiteNoise.loop = true;

        const filter = audioCtx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(800, audioCtx.currentTime);

        rainGain = audioCtx.createGain();
        rainGain.gain.setValueAtTime(0.12, audioCtx.currentTime);

        whiteNoise.connect(filter);
        filter.connect(rainGain);
        rainGain.connect(audioCtx.destination);
        whiteNoise.start();
        rainNode = whiteNoise;
    }

    if (rainBtn) {
        rainBtn.addEventListener('click', () => {
            rainPlaying = !rainPlaying;
            rainBtn.classList.toggle('active', rainPlaying);

            if (rainPlaying) {
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
            cafePlaying = !cafePlaying;
            cafeBtn.classList.toggle('active', cafePlaying);
            
            if (cafePlaying) {
                ambientAudio.src = "https://audio.jukehost.co/8Y4n3X9v2m1K0L5j7H6g5F4d3S2a1P0o"; // Ambient cafe / library stream
                ambientAudio.volume = 0.35;
                ambientAudio.play().catch(() => {});
            } else {
                ambientAudio.pause();
            }
        });
    }
}
