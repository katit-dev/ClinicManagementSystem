window.adminUsers = {
    focus(targetId, fallbackId) {
        const target = document.getElementById(targetId) || document.getElementById(fallbackId);
        if (!target) return;
        target.focus({ preventScroll: true });
        target.scrollIntoView({
            behavior: "instant",
            block: target.matches("button") ? "nearest" : "start",
            inline: "nearest"
        });
    }
};
