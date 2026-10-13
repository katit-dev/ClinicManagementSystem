window.adminReports = {
    focus(targetId) {
        const target = document.getElementById(targetId);
        if (!target) return;
        target.focus({ preventScroll: true });
        target.scrollIntoView({ behavior: "instant", block: "nearest", inline: "nearest" });
    },
    async download(fileName, streamReference) {
        const buffer = await streamReference.arrayBuffer();
        const blob = new Blob([buffer], {
            type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");
        try {
            anchor.href = url;
            anchor.download = fileName;
            document.body.appendChild(anchor);
            anchor.click();
        } finally {
            anchor.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    }
};
