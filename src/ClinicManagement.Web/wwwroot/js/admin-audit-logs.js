window.adminAuditLogs = {
    focus(targetId) {
        const target = document.getElementById(targetId);
        if (!target) return;
        target.focus({ preventScroll: true });
        target.scrollIntoView({ behavior: "instant", block: "nearest" });
    }
};
