window.adminSchedules = {
    focus(targetId) {
        const target = document.getElementById(targetId);
        if (!target) return;

        // The page calls this only after Blazor has rendered the new panel.
        target.focus({ preventScroll: true });
        target.scrollIntoView({
            behavior: "instant",
            block: target.matches("button") ? "nearest" : "start",
            inline: "nearest"
        });
    }
};
