/**
 * Shared hub navigation: year stamp + analytics binding.
 * Active-link state is server-rendered via aria-current on Nav.astro's
 * links; there is no mobile menu (single-row nav, no hub-nav-toggle).
 */
(function () {
  function stampYear() {
    var el = document.getElementById('footer-year');
    if (el) el.textContent = String(new Date().getFullYear());
  }

  function init() {
    stampYear();
    bindAnalyticsEvents();
  }

  function bindAnalyticsEvents() {
    document.querySelectorAll('[data-analytics]').forEach(function (el) {
      el.addEventListener('click', function () {
        var name = el.getAttribute('data-analytics');
        if (window.plausible && name) window.plausible(name);
      });
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
