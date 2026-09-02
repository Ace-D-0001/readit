// Asynchronous Vote handler (NO page reloads!)
async function asyncVote(btn, targetId, targetType, direction, event) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    
    const voteCol = btn.closest('.post-vote-col') || btn.closest('.comment-actions');
    if (!voteCol) return;

    const upBtn = voteCol.querySelector('.vote-arrow.up');
    const downBtn = voteCol.querySelector('.vote-arrow.down');
    const countEl = voteCol.querySelector('.vote-count');

    try {
        const response = await fetch('/Posts/VoteApi', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                targetId: parseInt(targetId),
                targetType: targetType,
                direction: parseInt(direction)
            })
        });

        const res = await response.json();
        if (res.success) {
            if (countEl) {
                countEl.textContent = res.score;
                if (res.userVote === 1) {
                    countEl.classList.add('text-accent');
                } else {
                    countEl.classList.remove('text-accent');
                }
            }
            if (upBtn) {
                if (res.userVote === 1) {
                    upBtn.classList.add('upvoted');
                } else {
                    upBtn.classList.remove('upvoted');
                }
            }
            if (downBtn) {
                if (res.userVote === -1) {
                    downBtn.classList.add('downvoted');
                } else {
                    downBtn.classList.remove('downvoted');
                }
            }
        }
    } catch (err) {
        console.error('Vote failed:', err);
    }
}

// ── ReadIt Animated Brand Mascot & Page Navigation Orchestrator ─────────
function triggerPageChangeAnimation() {
    const loader = document.getElementById('readit-top-loader');
    const mascot = document.getElementById('nav-mascot');
    const rightEye = document.getElementById('nav-eye-right');

    if (loader) {
        loader.classList.remove('finished');
        loader.classList.add('loading');
    }

    if (mascot) {
        mascot.classList.remove('page-jumping');
        void mascot.offsetWidth; // Force DOM reflow to re-trigger keyframe
        mascot.classList.add('page-jumping');
    }

    if (rightEye) {
        rightEye.classList.add('winking');
        setTimeout(() => {
            rightEye.classList.remove('winking');
        }, 750);
    }
}

function completePageChangeAnimation() {
    const loader = document.getElementById('readit-top-loader');
    const mascot = document.getElementById('nav-mascot');

    if (loader) {
        loader.classList.add('finished');
        setTimeout(() => {
            loader.classList.remove('loading', 'finished');
        }, 450);
    }

    if (mascot) {
        setTimeout(() => {
            mascot.classList.remove('page-jumping');
        }, 1250);
    }
}

// First-time site arrival & page load splash handling (2.0s duration)
document.addEventListener('DOMContentLoaded', () => {
    const splash = document.getElementById('readit-page-transition');
    const splashEye = document.getElementById('splash-eye-right');

    if (splash) {
        // Crisp 2.0s welcoming mascot choreography:
        // 0.0s: Pop in & float
        // 0.5s: Playful eye wink
        // 1.0s: Eyes open with orange glow
        // 1.4s: Second gentle wink & flutter
        // 2.0s: Smooth fade into the app
        if (splashEye) {
            setTimeout(() => splashEye.classList.add('winking'), 500);
            setTimeout(() => splashEye.classList.remove('winking'), 1000);
            setTimeout(() => splashEye.classList.add('winking'), 1400);
            setTimeout(() => splashEye.classList.remove('winking'), 1800);
        }

        setTimeout(() => {
            splash.classList.add('hidden-splash');
            triggerPageChangeAnimation();
        }, 2000);
    }

    // Interactive Header Mascot hover wink
    const brandLogo = document.querySelector('.brand-logo-link');
    if (brandLogo) {
        brandLogo.addEventListener('mouseenter', () => {
            const eye = document.getElementById('nav-eye-right');
            if (eye && !eye.classList.contains('winking')) {
                eye.classList.add('winking');
                setTimeout(() => eye.classList.remove('winking'), 700);
            }
        });
    }
});

// ── PJAX Seamless Navigation (Music NEVER stops when changing pages!) ──────────
document.addEventListener('click', async (e) => {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || (href.startsWith('http') && !href.startsWith(window.location.origin))) {
        return;
    }
    if (link.target === '_blank' || link.hasAttribute('download') || link.hasAttribute('data-no-pjax') || link.classList.contains('no-pjax')) {
        return;
    }

    // Do NOT intercept auth, login, logout, or admin state changes
    if (href.startsWith('/Account') || href.startsWith('/Admin') || href.includes('Logout')) {
        return; // Allow normal browser navigation
    }

    e.preventDefault();
    triggerPageChangeAnimation();

    try {
        // Enforce transition delay so the mascot cheer & jump is lavish and clear!
        const [res] = await Promise.all([
            fetch(href),
            new Promise(resolve => setTimeout(resolve, 900))
        ]);

        const html = await res.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        if (res.redirected && (res.url.includes('/Account') || res.url.includes('/Admin'))) {
            window.location.href = res.url;
            return;
        }

        const newMain = doc.querySelector('.app-main');
        const currentMain = document.querySelector('.app-main');

        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            document.title = doc.title;
            window.history.pushState(null, '', href);
            window.scrollTo(0, 0);

            // Update Right Aside if present
            const newAside = doc.querySelector('.app-aside');
            const currentAside = document.querySelector('.app-aside');
            if (newAside && currentAside) {
                currentAside.innerHTML = newAside.innerHTML;
            }

            completePageChangeAnimation();
        } else {
            window.location.href = href;
        }
    } catch (err) {
        window.location.href = href;
    }
});

window.addEventListener('popstate', async () => {
    triggerPageChangeAnimation();
    try {
        const [res] = await Promise.all([
            fetch(window.location.href),
            new Promise(resolve => setTimeout(resolve, 900))
        ]);
        const html = await res.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const newMain = doc.querySelector('.app-main');
        const currentMain = document.querySelector('.app-main');
        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            document.title = doc.title;
            completePageChangeAnimation();
        }
    } catch (err) {
        window.location.reload();
    }
});


