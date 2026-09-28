window.invoicePrint = {
    printPdf: function (pdfBytes) {
        const blob = new Blob(
            [new Uint8Array(pdfBytes)],
            { type: "application/pdf" }
        );

        const url = URL.createObjectURL(blob);

        const printWindow = window.open(
            url,
            "_blank"
        );

        if (!printWindow) {
            URL.revokeObjectURL(url);
            return;
        }
    }
};