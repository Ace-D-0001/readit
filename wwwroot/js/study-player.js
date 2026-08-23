/* ══════════════════════════════════════════════════════════════════════════
   PREMIUM STUDY MUSIC PLAYER — FULL SONGS & ELEGANT UI
   - Default Initial State: Plays 100% FULL-LENGTH Songs (3-5 minutes long)
   - Multi-Source Search: Jamendo Full MP3s (Primary) + iTunes Previews (Secondary)
   - Station Chips Removed per User Request
   - SessionStorage state persistence across full page reloads
   - BroadcastChannel UI state synchronization across multiple tabs
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyMusicPlayer();
});

function initStudyMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Native HTML5 Audio Object (No crossOrigin restriction)
    const audio = new Audio();
    audio.preload = 'auto';

    // BroadcastChannel for cross-tab UI state synchronization
    const bc = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('study_player_channel') : null;

    // DOM Elements
    const playBtn = document.getElementById('sp-play-btn');
    const playIcon = document.getElementById('sp-play-icon');
    const prevBtn = document.getElementById('sp-prev-btn');
    const nextBtn = document.getElementById('sp-next-btn');
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
    const minimizeBtn = document.getElementById('sp-min-btn');
    const discIcon = document.querySelector('.sp-disc-icon');

    let isPlaying = false;
    let isUserSeeking = false;
    let currentPlaylist = [];
    let currentIndex = 0;

    // Restore Volume
    const savedVol = localStorage.getItem('sp_vol') || 0.8;
    audio.volume = parseFloat(savedVol);
    if (volumeSlider) volumeSlider.value = audio.volume;

    // ── Restore Session State or Load Initial Full Tracks ────────────────────
    restoreSessionStateOrLoadDefault();

    function saveSessionState() {
        if (currentPlaylist.length > 0 && currentPlaylist[currentIndex]) {
            const track = currentPlaylist[currentIndex];
            const state = {
                track: track,
                playlist: currentPlaylist,
                index: currentIndex,
                currentTime: audio.currentTime || 0,
                isPlaying: isPlaying,
                volume: audio.volume,
                minimized: playerCard.classList.contains('minimized')
            };
            sessionStorage.setItem('sp_state', JSON.stringify(state));
        }
    }

    async function restoreSessionStateOrLoadDefault() {
        try {
            const savedState = sessionStorage.getItem('sp_state');
            if (savedState) {
                const state = JSON.parse(savedState);
                if (state.minimized) {
                    playerCard.classList.add('minimized');
                    if (minimizeBtn) {
                        const icon = minimizeBtn.querySelector('i');
                        if (icon) icon.className = "bi bi-chevron-up";
                    }
                }
                if (state.track && (state.track.url || state.track.streamUrl)) {
                    currentPlaylist = state.playlist || [state.track];
                    currentIndex = state.index || 0;

                    const tr = state.track;
                    updateUI(tr.title, tr.artist, tr.cover);
                    audio.src = tr.url || tr.streamUrl;

                    if (state.currentTime > 0) {
                        audio.currentTime = state.currentTime;
                    }

                    if (state.isPlaying) {
                        playTrack();
                    }
                    return;
                }
            }
        } catch (e) {
            console.warn("Could not restore session state:", e);
        }

        // Fetch initial full-length study tracks (3-5 mins long)
        await loadInitialFullPlaylist();
    }

    async function loadInitialFullPlaylist() {
        try {
            const res = await fetch('/api/music/lofi-full');
            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    currentPlaylist = data;
                    currentIndex = 0;
                    playPlaylistTrack(0, false);
                    return;
                }
            }
        } catch (err) {
            console.warn("Failed loading full playlist:", err);
        }
    }

    window.addEventListener('beforeunload', () => {
        saveSessionState();
    });

    // ── BroadcastChannel Tab Synchronization ────────────────────────────────
    if (bc) {
        bc.onmessage = (event) => {
            const data = event.data;
            if (!data) return;
            if (data.type === 'VOLUME_CHANGE') {
                audio.volume = data.volume;
                if (volumeSlider) volumeSlider.value = data.volume;
            } else if (data.type === 'TOGGLE_MINIMIZE') {
                playerCard.classList.toggle('minimized', data.minimized);
                if (minimizeBtn) {
                    const icon = minimizeBtn.querySelector('i');
                    if (icon) icon.className = data.minimized ? "bi bi-chevron-up" : "bi bi-chevron-down";
                }
            }
        };
    }

    // ── Play / Pause / Playlist Navigation ──────────────────────────────────
    function playTrack() {
        audio.play().then(() => setPlayingState(true)).catch((err) => {
            console.warn("Autoplay deferred until user interaction:", err);
            setPlayingState(false);
            if (titleEl) titleEl.textContent = "▶ Tap Play to Start";
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
        if (discIcon) discIcon.classList.toggle('spin-fast', playing);
        saveSessionState();
    }

    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPlaying) pauseTrack();
            else playTrack();
        });
    }

    if (prevBtn) {
        prevBtn.addEventListener('click', () => {
            if (currentPlaylist.length > 0) {
                currentIndex = (currentIndex - 1 + currentPlaylist.length) % currentPlaylist.length;
                playPlaylistTrack(currentIndex, true);
            }
        });
    }

    if (nextBtn) {
        nextBtn.addEventListener('click', () => {
            if (currentPlaylist.length > 0) {
                currentIndex = (currentIndex + 1) % currentPlaylist.length;
                playPlaylistTrack(currentIndex, true);
            }
        });
    }

    audio.addEventListener('play', () => setPlayingState(true));
    audio.addEventListener('pause', () => setPlayingState(false));
    audio.addEventListener('ended', () => {
        if (currentPlaylist.length > 0) {
            currentIndex = (currentIndex + 1) % currentPlaylist.length;
            playPlaylistTrack(currentIndex, true);
        } else {
            setPlayingState(false);
        }
    });

    audio.addEventListener('error', (e) => {
        console.warn("Track load error, advancing...", e);
        if (currentPlaylist.length > 0 && currentIndex < currentPlaylist.length - 1) {
            currentIndex++;
            playPlaylistTrack(currentIndex, true);
        }
    });

    function playPlaylistTrack(idx, shouldAutoplay = true) {
        if (idx < 0 || idx >= currentPlaylist.length) return;
        const track = currentPlaylist[idx];

        updateUI(track.title, track.artist, track.cover);
        audio.src = track.url || track.streamUrl;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = "0:00";
        if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : "--:--";

        if (shouldAutoplay) {
            playTrack();
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title || "Full Study Track";
        if (artistEl) artistEl.textContent = artist || "Full Song";
        if (coverEl && cover) coverEl.src = cover;
    }

    // ── Timeline Progress Slider ─────────────────────────────────────────────
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
            if (bc) bc.postMessage({ type: 'VOLUME_CHANGE', volume: vol });
        });
    }

    // ── Multi-Source Search (Jamendo Full Songs First, iTunes Previews Second)
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchDebounce = setTimeout(() => executeMultiSearch(query), 300);
        });
    }

    async function executeMultiSearch(query) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: var(--accent); text-align: center;"><i class="bi bi-search spin"></i> Searching full songs & previews…</div>`;
        searchResults.style.display = 'block';

        try {
            // Jamendo (Full Songs) first, iTunes (30s Previews) second
            const [jamendoRes, itunesRes] = await Promise.allSettled([
                fetch(`/api/music/jamendo?q=${encodeURIComponent(query)}`),
                fetch(`/api/music/itunes?q=${encodeURIComponent(query)}`)
            ]);

            const tracks = [];

            // 1. Jamendo Full-Length MP3 Songs
            if (jamendoRes.status === 'fulfilled' && jamendoRes.value.ok) {
                const jamendoData = await jamendoRes.value.json();
                if (Array.isArray(jamendoData)) {
                    jamendoData.forEach(p => {
                        const pTitle = (p.title || '').toLowerCase();
                        if (!tracks.some(tr => (tr.title || '').toLowerCase() === pTitle)) {
                            tracks.push(p);
                        }
                    });
                }
            }

            // 2. iTunes 30-second Previews
            if (itunesRes.status === 'fulfilled' && itunesRes.value.ok) {
                const iTunesData = await itunesRes.value.json();
                if (Array.isArray(iTunesData)) {
                    iTunesData.forEach(p => {
                        const pTitle = (p.title || '').toLowerCase();
                        if (!tracks.some(tr => (tr.title || '').toLowerCase() === pTitle)) {
                            tracks.push(p);
                        }
                    });
                }
            }

            renderSearchResults(tracks);
        } catch (err) {
            console.error("Search error:", err);
            searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: #a1a1aa; text-align: center;">Error searching music. Please try again.</div>`;
        }
    }

    function renderSearchResults(items) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        if (items.length === 0) {
            searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: #a1a1aa; text-align: center;">No songs found. Try another search!</div>`;
            searchResults.style.display = 'block';
            return;
        }

        items.forEach((t, idx) => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';

            const durText = t.duration ? formatTime(t.duration) : '';

            div.innerHTML = `
                <img src="${t.cover}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150'" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${escapeHtml(t.title || 'Unknown Title')}</div>
                    <div class="sp-search-artist">${escapeHtml(t.artist || 'Unknown Artist')}${durText ? ' · ' + durText : ''}</div>
                </div>
                <span class="sp-source-badge ${t.badgeClass || 'sp-badge-jamendo'}">${t.source || 'Full Song'}</span>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 22px; flex-shrink: 0;"></i>
            `;

            div.addEventListener('click', () => {
                currentPlaylist = items;
                currentIndex = idx;
                playPlaylistTrack(idx, true);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
            });
            searchResults.appendChild(div);
        });

        searchResults.style.display = 'block';
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str || '';
        return div.innerHTML;
    }

    document.addEventListener('click', (e) => {
        if (searchResults && !searchResults.contains(e.target) && !searchInput.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    if (minimizeBtn) {
        minimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            playerCard.classList.toggle('minimized');
            const isMin = playerCard.classList.contains('minimized');
            const icon = minimizeBtn.querySelector('i');
            if (icon) {
                icon.className = isMin ? "bi bi-chevron-up" : "bi bi-chevron-down";
            }
            if (bc) bc.postMessage({ type: 'TOGGLE_MINIMIZE', minimized: isMin });
            saveSessionState();
        });
    }
}
