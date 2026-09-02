/* ══════════════════════════════════════════════════════════════════════════
   SPOTIFY PC & MOBILE BOTTOM MUSIC PLAYER
   - Full-Length YouTube Songs & Playlists (via YouTube IFrame API)
   - HTML5 Audio fallback for preview streams
   - Spotify PC 3-column bottom bar + Spotify Mobile mini-player
   - Cross-tab synchronization via BroadcastChannel
   - Persistent playback state across seamless PJAX navigation
   ══════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {
    initStudyMusicPlayer();
});

let ytPlayer = null;
let ytReady = false;
let pendingYtVideoId = null;

// YouTube IFrame API Callback
window.onYouTubeIframeAPIReady = function () {
    const hostEl = document.getElementById('sp-yt-host');
    if (!hostEl) return;

    ytPlayer = new YT.Player('sp-yt-host', {
        height: '200',
        width: '200',
        playerVars: {
            autoplay: 0,
            controls: 0,
            disablekb: 1,
            fs: 0,
            modestbranding: 1,
            rel: 0
        },
        events: {
            onReady: (event) => {
                ytReady = true;
                const savedVol = parseFloat(localStorage.getItem('sp_vol') || 0.8);
                if (ytPlayer && ytPlayer.setVolume) ytPlayer.setVolume(savedVol * 100);
                if (pendingYtVideoId) {
                    ytPlayer.loadVideoById(pendingYtVideoId);
                    pendingYtVideoId = null;
                }
            },
            onStateChange: (event) => {
                if (window.studyPlayerInstance) {
                    if (event.data === YT.PlayerState.PLAYING) {
                        window.studyPlayerInstance.setPlayingState(true);
                    } else if (event.data === YT.PlayerState.PAUSED) {
                        window.studyPlayerInstance.setPlayingState(false);
                    } else if (event.data === YT.PlayerState.ENDED) {
                        window.studyPlayerInstance.handleTrackEnded();
                    }
                }
            }
        }
    });
};

function initStudyMusicPlayer() {
    const playerCard = document.getElementById('study-player');
    if (!playerCard) return;

    // Single Native HTML5 Audio Element (for fallback streams)
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
    const discIcon = document.querySelector('.sp-disc-icon');

    // Mobile & Drawer Elements
    const mobilePlayBtn = document.getElementById('sp-mobile-play-btn');
    const mobilePlayIcon = document.getElementById('sp-mobile-play-icon');
    const mobileNextBtn = document.getElementById('sp-mobile-next-btn');
    const mobileProgressFill = document.getElementById('sp-mobile-progress-fill');
    const searchDrawer = document.getElementById('sp-search-drawer');
    const searchToggleBtn = document.getElementById('sp-search-toggle-btn');
    const drawerCloseBtn = document.getElementById('sp-drawer-close-btn');

    let isPlaying = false;
    let isUserSeeking = false;
    let activeMode = 'yt'; // 'yt' or 'audio'
    let currentPlaylist = [];
    let currentIndex = 0;
    let lastNonMuteVol = 0.8;
    let ytProgressInterval = null;

    // Player Object
    const player = {
        play() {
            if (activeMode === 'yt') {
                if (ytReady && ytPlayer && ytPlayer.playVideo) {
                    ytPlayer.playVideo();
                    setPlayingState(true);
                } else if (currentPlaylist[currentIndex]?.youtubeId) {
                    pendingYtVideoId = currentPlaylist[currentIndex].youtubeId;
                }
            } else {
                audio.play().then(() => setPlayingState(true)).catch((err) => {
                    console.warn("Autoplay deferred:", err);
                    setPlayingState(false);
                    if (titleEl) titleEl.textContent = "▶ Tap Play to Start";
                });
            }
        },
        pause() {
            if (activeMode === 'yt') {
                if (ytReady && ytPlayer && ytPlayer.pauseVideo) {
                    ytPlayer.pauseVideo();
                }
            } else {
                audio.pause();
            }
            setPlayingState(false);
        },
        seekToPercent(pct) {
            if (activeMode === 'yt') {
                if (ytReady && ytPlayer && ytPlayer.getDuration) {
                    const dur = ytPlayer.getDuration() || 0;
                    ytPlayer.seekTo((pct / 100) * dur, true);
                }
            } else {
                if (audio.duration && !isNaN(audio.duration)) {
                    audio.currentTime = (pct / 100) * audio.duration;
                }
            }
        },
        setVolume(vol) {
            audio.volume = vol;
            if (ytReady && ytPlayer && ytPlayer.setVolume) {
                ytPlayer.setVolume(vol * 100);
            }
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

    // Expose instance for YT callbacks
    window.studyPlayerInstance = {
        setPlayingState: setPlayingState,
        handleTrackEnded: () => {
            if (currentPlaylist.length > 0) {
                currentIndex = (currentIndex + 1) % currentPlaylist.length;
                playPlaylistTrack(currentIndex, true);
            } else {
                setPlayingState(false);
            }
        }
    };

    // Play / Pause Handlers
    function setPlayingState(playing) {
        isPlaying = playing;
        if (playIcon) playIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        if (mobilePlayIcon) mobilePlayIcon.className = playing ? "bi bi-pause-fill" : "bi bi-play-fill";
        eqBars.forEach(bar => bar.classList.toggle('playing', playing));
        if (discIcon) discIcon.classList.toggle('spin-fast', playing);

        if (playing) {
            startProgressTracking();
        } else {
            stopProgressTracking();
        }
        saveSessionState();
    }

    function startProgressTracking() {
        stopProgressTracking();
        ytProgressInterval = setInterval(() => {
            if (isUserSeeking) return;

            let current = 0;
            let duration = 30;

            if (activeMode === 'yt') {
                if (ytReady && ytPlayer && ytPlayer.getCurrentTime) {
                    current = ytPlayer.getCurrentTime() || 0;
                    duration = ytPlayer.getDuration() || 0;
                }
            } else {
                current = audio.currentTime || 0;
                duration = audio.duration || 30;
            }

            if (duration > 0 && !isNaN(duration)) {
                const pct = (current / duration) * 100;
                if (progressBar) progressBar.value = pct;
                if (mobileProgressFill) mobileProgressFill.style.width = pct + '%';
                if (timeCurrent) timeCurrent.textContent = formatTime(current);
                if (timeTotal) timeTotal.textContent = formatTime(duration);
            } else {
                if (timeCurrent) timeCurrent.textContent = formatTime(current);
            }
        }, 300);
    }

    function stopProgressTracking() {
        if (ytProgressInterval) {
            clearInterval(ytProgressInterval);
            ytProgressInterval = null;
        }
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

    if (mobileNextBtn) {
        mobileNextBtn.addEventListener('click', () => {
            if (nextBtn) nextBtn.click();
        });
    }

    if (volBtn) {
        volBtn.addEventListener('click', () => {
            if (audio.volume > 0) {
                player.setVolume(0);
            } else {
                player.setVolume(lastNonMuteVol || 0.8);
            }
        });
    }

    // Volume Slider
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            const vol = parseFloat(e.target.value);
            player.setVolume(vol);
            if (bc) bc.postMessage({ type: 'VOLUME_CHANGE', volume: vol });
        });
    }

    // Timeline Seeking
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

    // Search Drawer Interactions
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

    // Native Audio events
    audio.addEventListener('play', () => setPlayingState(true));
    audio.addEventListener('pause', () => setPlayingState(false));
    audio.addEventListener('ended', () => {
        window.studyPlayerInstance.handleTrackEnded();
    });

    function playPlaylistTrack(idx, shouldAutoplay = true) {
        if (idx < 0 || idx >= currentPlaylist.length) return;
        const track = currentPlaylist[idx];

        updateUI(track.title, track.artist, track.cover);

        if (track.youtubeId) {
            // Full YouTube Track
            activeMode = 'yt';
            audio.pause();

            if (progressBar) progressBar.value = 0;
            if (mobileProgressFill) mobileProgressFill.style.width = '0%';
            if (timeCurrent) timeCurrent.textContent = "0:00";
            if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : "Full Song";

            if (ytReady && ytPlayer && ytPlayer.loadVideoById) {
                if (shouldAutoplay) ytPlayer.loadVideoById(track.youtubeId);
                else ytPlayer.cueVideoById(track.youtubeId);
            } else {
                pendingYtVideoId = track.youtubeId;
            }
        } else if (track.streamUrl) {
            // Native Audio Stream
            activeMode = 'audio';
            if (ytReady && ytPlayer && ytPlayer.pauseVideo) ytPlayer.pauseVideo();

            audio.src = track.streamUrl;
            if (progressBar) progressBar.value = 0;
            if (mobileProgressFill) mobileProgressFill.style.width = '0%';
            if (timeCurrent) timeCurrent.textContent = "0:00";
            if (timeTotal) timeTotal.textContent = track.duration ? formatTime(track.duration) : "0:30";

            if (shouldAutoplay) {
                player.play();
            }
        }

        if (shouldAutoplay) {
            setPlayingState(true);
        }
    }

    function updateUI(title, artist, cover) {
        if (titleEl) titleEl.textContent = title || "Study Track";
        if (artistEl) artistEl.textContent = artist || "Study Music";
        if (coverEl && cover) coverEl.src = cover;
    }

    function formatTime(secs) {
        if (isNaN(secs) || secs < 0) return "0:00";
        const m = Math.floor(secs / 60);
        const s = Math.floor(secs % 60);
        return `${m}:${s < 10 ? '0' : ''}${s}`;
    }

    // Restore Saved Volume
    const savedVol = parseFloat(localStorage.getItem('sp_vol') || 0.8);
    player.setVolume(savedVol);

    // Initial Playlist Loading
    restoreSessionStateOrLoadDefault();

    function saveSessionState() {
        if (currentPlaylist.length > 0 && currentPlaylist[currentIndex]) {
            const track = currentPlaylist[currentIndex];
            let curTime = 0;
            if (activeMode === 'yt' && ytReady && ytPlayer && ytPlayer.getCurrentTime) {
                curTime = ytPlayer.getCurrentTime() || 0;
            } else {
                curTime = audio.currentTime || 0;
            }

            const state = {
                track: track,
                playlist: currentPlaylist,
                index: currentIndex,
                currentTime: curTime,
                isPlaying: isPlaying,
                volume: audio.volume,
                mode: activeMode
            };
            sessionStorage.setItem('sp_state', JSON.stringify(state));
        }
    }

    async function restoreSessionStateOrLoadDefault() {
        try {
            const savedState = sessionStorage.getItem('sp_state');
            if (savedState) {
                const state = JSON.parse(savedState);
                if (state.track) {
                    currentPlaylist = state.playlist || [state.track];
                    currentIndex = state.index || 0;
                    activeMode = state.mode || (state.track.youtubeId ? 'yt' : 'audio');

                    const tr = state.track;
                    updateUI(tr.title, tr.artist, tr.cover);

                    if (activeMode === 'yt') {
                        if (ytReady && ytPlayer && ytPlayer.cueVideoById) {
                            ytPlayer.cueVideoById(tr.youtubeId, state.currentTime || 0);
                        } else {
                            pendingYtVideoId = tr.youtubeId;
                        }
                    } else if (tr.streamUrl) {
                        audio.src = tr.streamUrl;
                        if (state.currentTime > 0) audio.currentTime = state.currentTime;
                    }

                    if (state.isPlaying) player.play();
                    return;
                }
            }
        } catch (e) {
            console.warn("Could not restore session state:", e);
        }

        // Default initial load: Full YouTube Study Tracks
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
            console.warn("Failed loading initial study tracks:", err);
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
            }
        };
    }

    // Search Box Handling
    let searchDebounce = null;
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.trim();
            clearTimeout(searchDebounce);
            if (query.length < 2) {
                if (searchResults) searchResults.style.display = 'none';
                return;
            }
            searchDebounce = setTimeout(() => {
                performSearch(query);
            }, 300);
        });

        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                clearTimeout(searchDebounce);
                const query = e.target.value.trim();
                if (query.length >= 2) performSearch(query);
            }
        });
    }

    async function performSearch(query) {
        if (!searchResults) return;
        searchResults.style.display = 'block';
        searchResults.innerHTML = `<div style="padding: 16px; font-size: 12px; color: #a1a1aa; text-align: center;"><i class="bi bi-arrow-repeat spin-fast"></i> Searching YouTube & Music tracks…</div>`;

        try {
            const res = await fetch(`/api/music/search?q=${encodeURIComponent(query)}`);
            if (res.ok) {
                const tracks = await res.json();
                renderSearchResults(tracks);
                return;
            }
            renderSearchResults([]);
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
            noRes.style.padding = '16px';
            noRes.style.fontSize = '12px';
            noRes.style.color = '#a1a1aa';
            noRes.style.textAlign = 'center';
            noRes.innerHTML = `No tracks found. <br><span style="font-size: 10px; color: #71717a;">Tip: Paste any YouTube URL or search artist name</span>`;
            searchResults.appendChild(noRes);
            searchResults.style.display = 'block';
            return;
        }

        items.forEach((t, idx) => {
            const div = document.createElement('div');
            div.className = 'sp-search-item';

            const isYt = !!t.youtubeId;
            const badgeClass = isYt ? 'sp-badge-yt' : 'sp-badge-itunes';
            const badgeLabel = isYt ? 'Full Song (YouTube)' : '30s Preview';

            div.innerHTML = `
                <img src="${t.cover}" class="sp-search-art" alt="" onerror="this.src='https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=150'" />
                <div style="flex: 1; min-width: 0;">
                    <div class="sp-search-title">${escapeHtml(t.title || 'Unknown Title')}</div>
                    <div class="sp-search-artist">${escapeHtml(t.artist || 'Unknown Artist')}</div>
                </div>
                <span class="sp-source-badge ${badgeClass}">${badgeLabel}</span>
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
}
