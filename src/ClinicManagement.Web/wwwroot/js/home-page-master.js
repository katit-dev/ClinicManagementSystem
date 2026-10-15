window.clinicLayout = window.clinicLayout || {};
window.clinicLayout.confirmLogout = function () {
    const dialog = document.getElementById("logout-confirm-dialog");
    if (!dialog || dialog.open) return Promise.resolve(false);

    return new Promise(resolve => {
        dialog.returnValue = "cancel";
        dialog.addEventListener("close", () => resolve(dialog.returnValue === "confirm"), { once: true });
        dialog.showModal();
    });
};
