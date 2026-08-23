/* ══════════════════════════════════════════════════════════════════════════
   STUDY MUSIC PLAYER — SOUNDCLOUD WIDGET & NATIVE AUDIO DUAL ENGINE
   - Seamless control of Native HTML5 Audio and SoundCloud Embedded Widget
   - Unified playback abstraction (play, pause, seekTo, setVolume)
   - Zero-overlap playback source switching
   - SessionStorage persistence across full page reloads
   - BroadcastChannel UI state synchronization
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyMusicPlayer();
});

function initStudyMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Native HTML5 Audio Element for Jamendo / Direct MP3s / iTunes
    const audio = new Audio();
    audio.preload = 'auto';

    // SoundCloud Widget API Instance & Readiness Queue
    const scFrame = document.getElementById('sc-widget-frame');
    let scWidget = null;
    let isScReady = false;
    let pendingScTrack = null;

    if (scFrame && typeof SC !== 'undefined' && SC.Widget) {
        try {
            scWidget = SC.Widget(scFrame);
            scWidget.bind(SC.Widget.Events.READY, () => {
                isScReady = true;
                const vol = parseFloat(localStorage.getItem('sp_vol') || 0.8);
                try { scWidget.setVolume(vol * 100); } catch(e){}
                if (pendingScTrack) {
                    const trackToPlay = pendingScTrack;
                    pendingScTrack = null;
                    loadAndPlaySoundCloudTrack(trackToPlay);
                }
            });

            scWidget.bind(SC.Widget.Events.PLAY, () => {
                if (activeSource === 'soundcloud') setPlayingState(true);
            });

            scWidget.bind(SC.Widget.Events.PAUSE, () => {
                if (activeSource === 'soundcloud') setPlayingState(false);
            });

            scWidget.bind(SC.Widget.Events.FINISH, () => {
                if (activeSource === 'soundcloud' && currentPlaylist.length > 0) {
                    currentIndex = (currentIndex + 1) % currentPlaylist.length;
                    playPlaylistTrack(currentIndex, true);
                }
            });

            scWidget.bind(SC.Widget.Events.PLAY_PROGRESS, (data) => {
                if (activeSource === 'soundcloud' && !isUserSeeking) {
                    const currentSecs = data.currentPosition / 1000;
                    const totalSecs = (data.currentPosition / (data.relativePosition || 1)) / 1000;
                    if (totalSecs > 0) {
                        const pct = (currentSecs / totalSecs) * 100;
                        if (progressBar) progressBar.value = pct;
                        if (timeCurrent) timeCurrent.textContent = formatTime(currentSecs);
                        if (timeTotal) timeTotal.textContent = formatTime(totalSecs);
                    }
                }
            });
        } catch (err) {
            console.warn("SoundCloud Widget API init exception:", err);
        }
    }

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
    let activeSource = 'native'; // 'native' or 'soundcloud'

    // Unified Player Abstraction
    const player = {
        play() {
            if (activeSource === 'soundcloud') {
                if (audio) audio.pause();
                if (scWidget && isScReady) scWidget.play();
                setPlayingState(true);
            } else {
                if (scWidget && isScReady) try { scWidget.pause(); } catch(e){}
                audio.play().then(() => setPlayingState(true)).catch((err) => {
                    console.warn("Autoplay deferred:", err);
                    setPlayingState(false);
                    if (titleEl) titleEl.textContent = "▶ Tap Play to Start";
                });
            }
        },
        pause() {
            if (activeSource === 'soundcloud') {
                if (scWidget && isScReady) scWidget.pause();
            }
            audio.pause();
            setPlayingState(false);
        },
        seekToPercent(pct) {
            if (activeSource === 'soundcloud' && scWidget && isScReady) {
                scWidget.getDuration((durMs) => {
                    if (durMs > 0) scWidget.seekTo((pct / 100) * durMs);
                });
            } else if (audio.duration && !isNaN(audio.duration)) {
                audio.currentTime = (pct / 100) * audio.duration;
            }
        },
        setVolume(vol) { // vol parameter is 0 to 1
            audio.volume = vol;
            if (volumeSlider) volumeSlider.value = vol;
            if (scWidget && isScReady) {
                try { scWidget.setVolume(vol * 100); } catch(e){}
            }
            localStorage.setItem('sp_vol', vol);
        }
    };

    // Restore Volume
    const savedVol = parseFloat(localStorage.getItem('sp_vol') || 0.8);
    player.setVolume(savedVol);

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
                activeSource: activeSource,
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

                    if (tr.isSoundCloud || tr.source === 'SoundCloud') {
                        activeSource = 'soundcloud';
                        if (isScReady) {
                            scWidget.load(tr.streamUrl, { auto_play: state.isPlaying, show_artwork: false });
                        } else {
                            pendingScTrack = tr;
                        }
                    } else {
                        activeSource = 'native';
                        audio.src = tr.url || tr.streamUrl;
                        if (state.currentTime > 0) audio.currentTime = state.currentTime;
                        if (state.isPlaying) player.play();
                    }
                    return;
                }
            }
        } catch (e) {
            console.warn("Could not restore session state:", e);
        }

        // Default initial load
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

    // ── Play / Pause / Navigation Handlers ──────────────────────────────────
    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
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

    // Native Audio Events
    audio.addEventListener('play', () => { if (activeSource === 'native') setPlayingState(true); });
    audio.addEventListener('pause', () => { if (activeSource === 'native') setPlayingState(false); });
    audio.addEventListener('ended', () => {
        if (activeSource === 'native' && currentPlaylist.length > 0) {
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

        if (track.isSoundCloud || track.source === 'SoundCloud') {
            // Stop native audio completely so sound NEVER overlaps
            audio.pause();
            audio.src = '';
            activeSource = 'soundcloud';

            loadAndPlaySoundCloudTrack(track, shouldAutoplay);
        } else {
            // Pause SoundCloud Widget completely so sound NEVER overlaps
            if (scWidget && isScReady) {
                try { scWidget.pause(); } catch(e){}
            }
            activeSource = 'native';

            audio.src = track.url || track.streamUrl;
            if (progressBar) progressBar.value = 0;
            if (timeCurrent) timeCurrent.textContent = "0:00";
            if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : "--:--";

            if (shouldAutoplay) {
                player.play();
            }
        }
    }

    function loadAndPlaySoundCloudTrack(track, shouldAutoplay = true) {
        if (!isScReady || !scWidget) {
            pendingScTrack = track;
            return;
        }

        try {
            scWidget.load(track.streamUrl, {
                auto_play: shouldAutoplay,
                show_artwork: false,
                callback: () => {
                    if (shouldAutoplay) setPlayingState(true);
                }
            });
        } catch (e) {
            console.warn("Error loading SoundCloud track into widget:", e);
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title || "Full Study Song";
        if (artistEl) artistEl.textContent = artist || "Full Track";
        if (coverEl && cover) coverEl.src = cover;
    }

    // ── Timeline Progress Slider ─────────────────────────────────────────────
    audio.addEventListener('timeupdate', () => {
        if (isUserSeeking || activeSource !== 'native') return;
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

    // ── Volume Control ───────────────────────────────────────────────────────
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            player.setVolume(vol);
            if (bc) bc.postMessage({ type: 'VOLUME_CHANGE', volume: vol });
        });
    }

    // ── Direct Multi-Source Search (SoundCloud + Jamendo + iTunes) ───────────
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchDebounce);
            const query = e.target.value.trim();
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }

            // Auto-detect if input is a direct SoundCloud URL
            if (query.includes('soundcloud.com/')) {
                resolveAndPlaySoundCloudUrl(query);
                return;
            }

            searchDebounce = setTimeout(() => executeMultiSearch(query), 250);
        });
    }

    async function resolveAndPlaySoundCloudUrl(scUrl) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: var(--accent); text-align: center;"><i class="bi bi-music-note-beamed spin"></i> Resolving SoundCloud link…</div>`;
        searchResults.style.display = 'block';

        try {
            const res = await fetch(`/api/music/soundcloud-oembed?url=${encodeURIComponent(scUrl)}`);
            if (res.ok) {
                const track = await res.json();
                currentPlaylist.unshift(track);
                currentIndex = 0;
                playPlaylistTrack(0, true);
                searchResults.style.display = 'none';
                if (searchInput) searchInput.value = '';
                return;
            }
        } catch (e) {}

        searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: #ef4444; text-align: center;">Could not load SoundCloud URL. Check link and try again.</div>`;
    }

    async function executeMultiSearch(query) {
        if (!searchResults) return;
        searchResults.innerHTML = `<div style="padding: 12px; font-size: 11px; color: var(--accent); text-align: center;"><i class="bi bi-search spin"></i> Searching music…</div>`;
        searchResults.style.display = 'block';

        try {
            // Parallel search across Jamendo, SoundCloud, and iTunes
            const [jamendoRes, scRes, itunesRes] = await Promise.allSettled([
                fetch(`/api/music/jamendo?q=${encodeURIComponent(query)}`),
                fetch(`/api/music/soundcloud?q=${encodeURIComponent(query)}`),
                fetch(`/api/music/itunes?q=${encodeURIComponent(query)}`)
            ]);

            const tracks = [];

            // 1. Jamendo Full Songs (100% Full Length)
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

            // 2. SoundCloud Tracks
            if (scRes.status === 'fulfilled' && scRes.value.ok) {
                const scData = await scRes.value.json();
                if (Array.isArray(scData)) {
                    scData.forEach(p => {
                        const pTitle = (p.title || '').toLowerCase();
                        if (!tracks.some(tr => (tr.title || '').toLowerCase() === pTitle)) {
                            tracks.push(p);
                        }
                    });
                }
            }

            // 3. iTunes Commercial Song Lookup Fallback
            if (itunesRes.status === 'fulfilled' && itunesRes.value.ok) {
                const itunesData = await itunesRes.value.json();
                if (Array.isArray(itunesData)) {
                    itunesData.forEach(p => {
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
            const noRes = document.createElement('div');
            noRes.style.padding = '12px';
            noRes.style.fontSize = '11px';
            noRes.style.color = '#a1a1aa';
            noRes.style.textAlign = 'center';
            noRes.textContent = "No songs found for that search. Try another query like 'lofi', 'chill', 'piano', or an artist!";
            searchResults.appendChild(noRes);
            searchResults.style.display = 'block';
            return;
        }

        items.forEach((t, idx) => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';

            const durText = t.duration ? formatTime(t.duration) : 'Full Song';

            div.innerHTML = `
                <img src="${t.cover}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150'" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${escapeHtml(t.title || 'Unknown Title')}</div>
                    <div class="sp-search-artist">${escapeHtml(t.artist || 'Unknown Artist')} · ${durText}</div>
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
