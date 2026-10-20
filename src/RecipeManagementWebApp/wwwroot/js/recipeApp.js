window.recipeApp = {
    confirmLeave: function (message) {
        return confirm(message);
    },

    wakeLock: null,
    wakeLockActive: false,

    requestWakeLock: async function () {
        this.wakeLockActive = true;
        try {
            if ('wakeLock' in navigator) {
                this.wakeLock = await navigator.wakeLock.request('screen');
            }
        } catch {
            // Silent degradation (spec 020 FR-004) -- unsupported or denied, cook mode continues regardless.
        }
    },

    releaseWakeLock: function () {
        this.wakeLockActive = false;
        try {
            this.wakeLock?.release();
        } catch {
            // ignore
        }
        this.wakeLock = null;
    },

    playTimerAlert: function () {
        try {
            if (navigator.vibrate) {
                navigator.vibrate([300, 100, 300, 100, 300]);
            }
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            if (AudioCtx) {
                const ctx = new AudioCtx();
                const osc = ctx.createOscillator();
                osc.frequency.value = 880;
                osc.connect(ctx.destination);
                osc.start();
                setTimeout(() => osc.stop(), 600);
            }
        } catch {
            // Visual-only fallback is handled in the UI itself.
        }
    }
};

document.addEventListener('visibilitychange', async () => {
    if (document.visibilityState === 'visible' && window.recipeApp.wakeLockActive) {
        await window.recipeApp.requestWakeLock();
    }
});
