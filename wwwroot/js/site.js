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

// ── PJAX Seamless Navigation (Music NEVER stops when changing pages!) ──────────
document.addEventListener('click', async (e) => {
    const link = e.target.closest('a');
    if (!link) return;

    const href = link.getAttribute('href');
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || (href.startsWith('http') && !href.startsWith(window.location.origin))) {
        return;
    }
    if (link.target === '_blank' || link.hasAttribute('download')) return;

    e.preventDefault();
    try {
        const res = await fetch(href);
        const html = await res.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        const newMain = doc.querySelector('.app-main');
        const currentMain = document.querySelector('.app-main');

        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            document.title = doc.title;
            window.history.pushState(null, '', href);
            window.scrollTo(0, 0);
        } else {
            window.location.href = href;
        }
    } catch (err) {
        window.location.href = href;
    }
});

window.addEventListener('popstate', async () => {
    try {
        const res = await fetch(window.location.href);
        const html = await res.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const newMain = doc.querySelector('.app-main');
        const currentMain = document.querySelector('.app-main');
        if (newMain && currentMain) {
            currentMain.innerHTML = newMain.innerHTML;
            document.title = doc.title;
        }
    } catch (err) {
        window.location.reload();
    }
});
