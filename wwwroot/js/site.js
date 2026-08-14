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
