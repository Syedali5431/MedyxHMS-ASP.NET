// MedyxHMS v2.0 — Modern Premium Sidebar
// Collapse/expand with localStorage, mobile drawer, tooltips, animations
(function () {
    'use strict';

    const STORAGE_KEY = 'medyx-sidebar-collapsed';
    const DROPDOWN_KEY = 'medyx-sidebar-dropdowns';
    const sidebar = document.getElementById('staff-sidebar');
    const toggleBtn = document.getElementById('sidebar-master-toggle');
    const overlay = document.getElementById('sidebar-overlay');

    if (!sidebar || !toggleBtn) return;

    const isMobile = () => window.innerWidth < 992;
    const isDesktop = () => !isMobile();

    // ── State ──
    function getCollapsed() {
        if (isMobile()) return true;
        try { return localStorage.getItem(STORAGE_KEY) === 'true'; } catch (_) { return false; }
    }
    function setCollapsed(collapsed) {
        try { localStorage.setItem(STORAGE_KEY, String(collapsed)); } catch (_) { }
    }

    function applyCollapsed(collapsed) {
        if (collapsed) {
            sidebar.classList.add('collapsed');
            sidebar.classList.remove('expanded');
        } else {
            sidebar.classList.remove('collapsed');
            sidebar.classList.add('expanded');
        }
    }

    function updateToggleIcon(collapsed) {
        const icon = toggleBtn.querySelector('i');
        if (!icon) return;
        icon.className = collapsed ? 'fas fa-chevron-right' : 'fas fa-bars';
    }

    // ── Mobile drawer ──
    function openMobile() {
        sidebar.classList.add('mobile-open');
        if (overlay) overlay.classList.add('show');
        document.body.style.overflow = 'hidden';
    }
    function closeMobile() {
        sidebar.classList.remove('mobile-open');
        if (overlay) overlay.classList.remove('show');
        document.body.style.overflow = '';
    }

    // ── Toggle handler ──
    toggleBtn.addEventListener('click', function () {
        if (isMobile()) {
            if (sidebar.classList.contains('mobile-open')) {
                closeMobile();
            } else {
                openMobile();
            }
            return;
        }
        const collapsed = !sidebar.classList.contains('collapsed');
        applyCollapsed(collapsed);
        updateToggleIcon(collapsed);
        setCollapsed(collapsed);
    });

    // ── Overlay click → close mobile ──
    if (overlay) {
        overlay.addEventListener('click', closeMobile);
    }

    // ── Escape key → close mobile ──
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar.classList.contains('mobile-open')) {
            closeMobile();
        }
    });

    // ── Hover expand on desktop when collapsed ──
    if (isDesktop()) {
        sidebar.addEventListener('mouseenter', function () {
            if (sidebar.classList.contains('collapsed')) {
                sidebar.classList.add('hover-expand');
            }
        });
        sidebar.addEventListener('mouseleave', function () {
            sidebar.classList.remove('hover-expand');
        });
    }

    // ── Dropdown state persistence ──
    function saveDropdowns() {
        const openDropdowns = [];
        sidebar.querySelectorAll('.staff-sidebar-link[aria-expanded="true"]').forEach(function (link) {
            const href = link.getAttribute('href') || link.getAttribute('data-bs-target');
            if (href) openDropdowns.push(href.replace('#', ''));
        });
        try { localStorage.setItem(DROPDOWN_KEY, JSON.stringify(openDropdowns)); } catch (_) { }
    }

    function restoreDropdowns() {
        try {
            const saved = JSON.parse(localStorage.getItem(DROPDOWN_KEY) || '[]');
            saved.forEach(function (id) {
                const link = sidebar.querySelector('[href="#' + id + '"]') || sidebar.querySelector('[data-bs-target="#' + id + '"]');
                if (link) {
                    link.classList.remove('collapsed');
                    link.setAttribute('aria-expanded', 'true');
                    const target = document.getElementById(id);
                    if (target) target.classList.add('show');
                }
            });
        } catch (_) { }
    }

    // Listen for Bootstrap collapse events to remember state
    sidebar.addEventListener('shown.bs.collapse', saveDropdowns);
    sidebar.addEventListener('hidden.bs.collapse', saveDropdowns);

    // ── Initialize ──
    const collapsed = getCollapsed();
    if (isDesktop()) {
        applyCollapsed(collapsed);
        updateToggleIcon(collapsed);
    } else {
        applyCollapsed(true); // mobile: always collapsed by default
        updateToggleIcon(true);
    }
    restoreDropdowns();

    // ── Resize handler ──
    let resizeTimeout;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(function () {
            if (isMobile()) {
                sidebar.classList.add('collapsed');
                sidebar.classList.remove('expanded');
                sidebar.classList.remove('mobile-open');
                if (overlay) overlay.classList.remove('show');
                document.body.style.overflow = '';
            } else {
                const saved = localStorage.getItem(STORAGE_KEY) === 'true';
                applyCollapsed(saved);
                updateToggleIcon(saved);
            }
        }, 150);
    });

    // ── Ripple effect on click ──
    sidebar.addEventListener('click', function (e) {
        const link = e.target.closest('.staff-sidebar-link, .staff-sidebar-sublink');
        if (!link) return;
        const ripple = document.createElement('span');
        ripple.className = 'sidebar-ripple';
        const rect = link.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = (e.clientX - rect.left - size / 2) + 'px';
        ripple.style.top = (e.clientY - rect.top - size / 2) + 'px';
        link.appendChild(ripple);
        setTimeout(function () { ripple.remove(); }, 600);
    });

})();
