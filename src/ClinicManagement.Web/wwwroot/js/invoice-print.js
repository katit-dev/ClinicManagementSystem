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

        let printed = false;

        const print = function () {
            if (printed) {
                return;
            }

            printed = true;

            printWindow.focus();
            printWindow.print();

            setTimeout(function () {
                URL.revokeObjectURL(url);
            }, 60000);
        };

        printWindow.addEventListener(
            "load",
            function () {
                setTimeout(print, 500);
            },
            { once: true }
        );

        // Fallback cho trường hợp PDF viewer
        // không phát sinh load event.
        setTimeout(print, 1500);
    }
};