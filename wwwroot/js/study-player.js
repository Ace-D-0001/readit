/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — ITUNES 30s PREVIEWS ONLY
   - Single HTML5 Audio Element for playback
   - Fast iTunes search for 30s previews
   - SessionStorage state persistence across page reloads
   - BroadcastChannel UI state synchronization
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyMusicPlayer();
});

function initStudyMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Single Native HTML5 Audio Element
    const audio = new Audio();
    audio.preload = 'auto';

    // BroadcastChannel for cross-tab UI state synchronization
    const bc = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('study_player_channel') : null;

    // DOM Elements
    const playBtn = document.getElementById('sp-play-btn');
    const playIcon = document.getElementById('sp-play-icon');
    const prevBtn = document.getElementById('sp-prev-btn');
    const nextBtn = document.getElementById('sp-next-btn');
    const volBtn = document.getElementById('sp-vol-btn');
    const volIcon = document.getElementById('sp-vol-icon');
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

    // Spotify Bottom Bar & Mobile Controls
    const mobilePlayBtn = document.getElementById('sp-mobile-play-btn');
    const mobilePlayIcon = document.getElementById('sp-mobile-play-icon');
    const mobileNextBtn = document.getElementById('sp-mobile-next-btn');
    const mobileProgressFill = document.getElementById('sp-mobile-progress-fill');
    const searchDrawer = document.getElementById('sp-search-drawer');
    const searchToggleBtn = document.getElementById('sp-search-toggle-btn');
    const drawerCloseBtn = document.getElementById('sp-drawer-close-btn');

    let isPlaying = false;
    let isUserSeeking = false;
    let currentPlaylist = [];
    let currentIndex = 0;
    let lastNonMuteVol = 0.8;

    // Player Object
    const player = {
        play() {
            audio.play().then(() => setPlayingState(true)).catch((err) => {
                console.warn("Autoplay deferred:", err);
                setPlayingState(false);
                if (titleEl) titleEl.textContent = "▶ Tap Play to Start";
            });
        },
        pause() {
            audio.pause();
            setPlayingState(false);
        },
        seekToPercent(pct) {
            if (audio.duration && !isNaN(audio.duration)) {
                audio.currentTime = (pct / 100) * audio.duration;
            }
        },
        setVolume(vol) {
            audio.volume = vol;
            if (volumeSlider) volumeSlider.value = vol;
            if (volIcon) {
                if (vol === 0) volIcon.className = "bi bi-volume-mute-fill";
                else if (vol < 0.5) volIcon.className = "bi bi-volume-down-fill";
                else volIcon.className = "bi bi-volume-up-fill";
            }
            if (vol > 0) lastNonMuteVol = vol;
            localStorage.setItem('sp_vol', vol);
        }
    };

    if (volBtn) {
        volBtn.addEventListener('click', () => {
            if (audio.volume > 0) {
                player.setVolume(0);
            } else {
                player.setVolume(lastNonMuteVol || 0.8);
            }
        });
    }

    // Restore Volume
    const savedVol = parseFloat(localStorage.getItem('sp_vol') || 0.8);
    player.setVolume(savedVol);

    // Restore Session State or Load Initial Tracks
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
                    if (state.currentTime > 0) audio.currentTime = state.currentTime;
                    if (state.isPlaying) player.play();
                    return;
                }
            }
        } catch (e) {
            console.warn("Could not restore session state:", e);
        }

        // Default initial load
        await loadInitialPlaylist();
    }

    async function loadInitialPlaylist() {
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
            console.warn("Failed loading initial iTunes tracks:", err);
        }
    }

    window.addEventListener('beforeunload', () => {
        saveSessionState();
    });

    // BroadcastChannel Tab Synchronization
    if (bc) {
        bc.onmessage = (event) => {
            const data = event.data;
            if (!data) return;
            if (data.type === 'VOLUME_CHANGE') {
                player.setVolume(data.volume);
            } else if (data.type === 'TOGGLE_MINIMIZE') {
                playerCard.classList.toggle('minimized', data.minimized);
                if (minimizeBtn) {
                    const icon = minimizeBtn.querySelector('i');
                    if (icon) icon.className = data.minimized ? "bi bi-chevron-up" : "bi bi-chevron-down";
                }
            }
        };
    }

    // Play / Pause / Navigation Handlers
    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        if (mobilePlayIcon) mobilePlayIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
        if (discIcon) discIcon.classList.toggle('spin-fast', playing);
        saveSessionState();
    }

    if (playBtn) {
        playBtn.addEventListener('click', () => {
            if (isPlaying) player.pause();
            else player.play();
        });
    }

    if (mobilePlayBtn) {
        mobilePlayBtn.addEventListener('click', () => {
            if (isPlaying) player.pause();
            else player.play();
        });
    }

    if (mobileNextBtn) {
        mobileNextBtn.addEventListener('click', () => {
            if (nextBtn) nextBtn.click();
        });
    }

    if (searchToggleBtn && searchDrawer) {
        searchToggleBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            searchDrawer.classList.toggle('open');
            if (searchDrawer.classList.contains('open') && searchInput) {
                setTimeout(() => searchInput.focus(), 150);
            }
        });
    }

    if (drawerCloseBtn && searchDrawer) {
        drawerCloseBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            searchDrawer.classList.remove('open');
        });
    }

    document.addEventListener('click', (e) => {
        if (searchDrawer && searchDrawer.classList.contains('open')) {
            if (!searchDrawer.contains(e.target) && !searchToggleBtn.contains(e.target)) {
                searchDrawer.classList.remove('open');
            }
        }
    });

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

    // Audio Events
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

    function playPlaylistTrack(idx, shouldAutoplay = true) {
        if (idx < 0 || idx >= currentPlaylist.length) return;
        const track = currentPlaylist[idx];

        updateUI(track.title, track.artist, track.cover);

        audio.src = track.url || track.streamUrl;
        if (progressBar) progressBar.value = 0;
        if (timeCurrent) timeCurrent.textContent = "0:00";
        if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : "0:30";

        if (shouldAutoplay) {
            player.play();
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title || "iTunes Song";
        if (artistEl) artistEl.textContent = artist || "iTunes Artist";
        if (coverEl && cover) coverEl.src = cover;
    }

    // Timeline Progress Slider
    audio.addEventListener('timeupdate', () => {
        if (isUserSeeking) return;
        const current = audio.currentTime || 0;
        const duration = audio.duration || 30;

        if (duration > 0 && !isNaN(duration)) {
            const pct = (current / duration) * 100;
            if (progressBar) progressBar.value = pct;
            if (mobileProgressFill) mobileProgressFill.style.width = pct + '%';
            if (timeCurrent) timeCurrent.textContent = formatTime(current);
            if (timeTotal) timeTotal.textContent = formatTime(duration);
        } else {
            if (timeCurrent) timeCurrent.textContent = formatTime(current);
        }
    });

    if (progressBar) {
        progressBar.addEventListener('mousedown', () => { isUserSeeking = true; });
        progressBar.addEventListener('touchstart', () => { isUserSeeking = true; });

        const handleSeek = (e) => {
            const pct = parseFloat(e.target.value);
            player.seekToPercent(pct);
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

    // Volume Control
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            player.setVolume(vol);
            if (bc) bc.postMessage({ type: 'VOLUME_CHANGE', volume: vol });
        });
    }

    // Search iTunes Previews
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }

            searchDebounce = setTimeout(() => executeITunesSearch(query), 250);
        });
    }

    async function executeITunesSearch(query) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: var(--accent); text-align: center;"><i class="bi bi-search spin"></i> Searching iTunes…</div>`;
        searchResults.style.display = 'block';

        try {
            const res = await fetch(`/api/music/itunes?q=${encodeURIComponent(query)}`);
            if (res.ok) {
                const tracks = await res.json();
                renderSearchResults(tracks);
                return;
            }
            renderSearchResults([]);
        } catch (err) {
            console.error("Search error:", err);
            searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: #a1a1aa; text-align: center;">Error searching iTunes. Please try again.</div>`;
        }
    }

    function renderSearchResults(items) {
        if (!searchResults) return;
        searchResults.innerHTML = '';

        if (items.length === 0) {
            const noRes = document.createElement('div');
            noRes.style.padding = '12px';
            noRes.style.fontSize = '11px';
            noRes.style.color = '#a1a1aa';
            noRes.style.textAlign = 'center';
            noRes.textContent = "No iTunes previews found for that search.";
            searchResults.appendChild(noRes);
            searchResults.style.display = 'block';
            return;
        }

        items.forEach((t, idx) => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';

            div.innerHTML = `
                <img src="${t.cover}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150'" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${escapeHtml(t.title || 'Unknown Title')}</div>
                    <div class="sp-search-artist">${escapeHtml(t.artist || 'Unknown Artist')} · 0:30</div>
                </div>
                <span class="sp-source-badge sp-badge-itunes">30s Preview</span>
                <i class="bi bi-play-circle-fill" style="color: var(--accent); font-size: 22px; flex-shrink: 0;"></i>
            `;

            div.addEventListener('click', () => {
                currentPlaylist = items;
                currentIndex = idx;
                playPlaylistTrack(idx, true);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
                if (searchDrawer) searchDrawer.classList.remove('open');
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
