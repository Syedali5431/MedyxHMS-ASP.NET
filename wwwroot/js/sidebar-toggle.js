// MedyxHMS — Sidebar toggle with localStorage persistence
// Desktop: collapses sidebar to icon-only. Mobile: toggles overlay (existing behavior unaffected).
(function () {
    'use strict';

    const STORAGE_KEY = 'medyx-sidebar-collapsed';
    const sidebar = document.getElementById('staff-sidebar');
    const toggleBtn = document.getElementById('sidebar-toggle-btn');

    if (!sidebar || !toggleBtn) return;

    // Desktop state from localStorage
    function isDesktop() { return window.innerWidth >= 992; }

    function applyState(collapsed) {
        if (collapsed) {
            sidebar.classList.add('sidebar-collapsed');
        } else {
            sidebar.classList.remove('sidebar-collapsed');
        }
    }

    function saveState(collapsed) {
        try { localStorage.setItem(STORAGE_KEY, String(collapsed)); } catch (_) { /* quota / private */ }
    }

    function updateIcon(collapsed) {
        var icon = toggleBtn.querySelector('i');
        if (!icon) return;
        if (collapsed) {
            icon.classList.replace('fa-bars', 'fa-chevron-right');
        } else {
            icon.classList.replace('fa-chevron-right', 'fa-bars');
        }
    }

    // Restore on load (desktop only)
    if (isDesktop()) {
        var saved = localStorage.getItem(STORAGE_KEY) === 'true';
        if (saved) {
            applyState(true);
            updateIcon(true);
        }
    }

    toggleBtn.addEventListener('click', function () {
        if (isDesktop()) {
            // Desktop: collapse/expand sidebar
            var collapsed = !sidebar.classList.contains('sidebar-collapsed');
            applyState(collapsed);
            saveState(collapsed);
            updateIcon(collapsed);
        }
        // Mobile behavior is handled by existing inline JS (show/overlay class toggle).
        // The existing listener on toggleBtn already handles mobile open/close via 'show' class.
        // This handler does nothing on mobile — it lets the existing mobile logic take over.
    });

    // Reset on resize: if switching from mobile to desktop, restore desktop state
    window.addEventListener('resize', function () {
        if (isDesktop()) {
            var saved = localStorage.getItem(STORAGE_KEY) === 'true';
            applyState(saved);
            updateIcon(saved);
        } else {
            // On mobile, remove collapsed class so overlay behavior works
            sidebar.classList.remove('sidebar-collapsed');
            // Reset icon to bars for mobile
            var icon = toggleBtn.querySelector('i');
            if (icon) {
                icon.classList.replace('fa-chevron-right', 'fa-bars');
            }
        }
    });
})();
