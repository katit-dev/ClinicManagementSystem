window.adminServices = {
    focus(targetId) {
        const target = document.getElementById(targetId);
        if (!target) return;
        target.focus({ preventScroll: true });
        target.scrollIntoView({
            behavior: "instant",
            block: target.matches("button") ? "nearest" : "start",
            inline: "nearest"
        });
    }
};
