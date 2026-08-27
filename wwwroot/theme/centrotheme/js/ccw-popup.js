(function () {
    var SHOW_DELAY_MS = 2000;

    var overlay = document.getElementById("centro-ccw-popup");
    if (!overlay) return;

    var closeBtn = overlay.querySelector(".ccw-close");
    var modal = overlay.querySelector(".ccw-modal");
    var ctaBtn = overlay.querySelector(".ccw-cta");
    var lastFocused = null;

    function openPopup() {
        lastFocused = document.activeElement;

        overlay.hidden = false;

        requestAnimationFrame(function () {
            overlay.classList.add("ccw-visible");
        });

        if (ctaBtn) {
            ctaBtn.focus();
        }

        document.addEventListener("keydown", onKeydown);
    }

    function closePopup() {
        overlay.classList.remove("ccw-visible");

        document.removeEventListener("keydown", onKeydown);

        setTimeout(function () {
            overlay.hidden = true;
        }, 350);

        if (lastFocused && lastFocused.focus) {
            lastFocused.focus();
        }
    }

    function onKeydown(e) {
        if (e.key === "Escape") {
            closePopup();
            return;
        }

        if (e.key === "Tab") {
            var focusables = modal.querySelectorAll(
                'a[href], button:not([disabled])'
            );

            if (!focusables.length) return;

            var first = focusables[0];
            var last = focusables[focusables.length - 1];

            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    }

    closeBtn.addEventListener("click", closePopup);

    overlay.addEventListener("click", function (e) {
        if (e.target === overlay) {
            closePopup();
        }
    });

    window.addEventListener("DOMContentLoaded", function () {
        setTimeout(openPopup, SHOW_DELAY_MS);
    });

})();