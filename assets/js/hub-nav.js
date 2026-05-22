/**
 * Shared hub navigation: mobile toggle + active section highlight.
 * Expects #hub-navbar, #hub-nav-toggle, #hub-mobile-nav in the DOM.
 * Set <body data-hub="home|portfolio|shop|game|content"> on each page.
 */
(function () {
  function highlightActive() {
    var hub = document.body.getAttribute('data-hub');
    if (!hub) return;
    document.querySelectorAll('[data-hub-link]').forEach(function (el) {
      if (el.getAttribute('data-hub-link') === hub) {
        el.classList.add('hub-link-active');
        el.setAttribute('aria-current', 'page');
      }
    });
  }

  function bindMobile() {
    var btn = document.getElementById('hub-nav-toggle');
    var panel = document.getElementById('hub-mobile-nav');
    var iconOpen = document.getElementById('hub-nav-icon-open');
    var iconClose = document.getElementById('hub-nav-icon-close');
    if (!btn || !panel) return;
    btn.addEventListener('click', function () {
      var nowHidden = panel.classList.toggle('hidden');
      btn.setAttribute('aria-expanded', nowHidden ? 'false' : 'true');
      if (iconOpen) iconOpen.classList.toggle('hidden', !nowHidden);
      if (iconClose) iconClose.classList.toggle('hidden', nowHidden);
    });
    panel.querySelectorAll('a').forEach(function (a) {
      a.addEventListener('click', function () {
        panel.classList.add('hidden');
        btn.setAttribute('aria-expanded', 'false');
        if (iconOpen) iconOpen.classList.remove('hidden');
        if (iconClose) iconClose.classList.add('hidden');
      });
    });
  }

  function stampYear() {
    var el = document.getElementById('footer-year');
    if (el) el.textContent = String(new Date().getFullYear());
  }

  function loadScript(src) {
    return new Promise(function (resolve, reject) {
      var existing = document.querySelector('script[src="' + src + '"]');
      if (existing) {
        existing.addEventListener('load', resolve, { once: true });
        existing.addEventListener('error', reject, { once: true });
        if (existing.dataset.loaded === 'true') resolve();
        return;
      }

      var script = document.createElement('script');
      script.src = src;
      script.async = true;
      script.onload = function () {
        script.dataset.loaded = 'true';
        resolve();
      };
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  function initInteractiveBackground() {
    var bg = document.getElementById('vanta-bg');
    if (!bg) return;

    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var small = window.innerWidth < 768;
    if (reduced || small) return;

    loadScript('https://cdnjs.cloudflare.com/ajax/libs/three.js/r134/three.min.js')
      .then(function () { return loadScript('https://cdn.jsdelivr.net/npm/vanta@0.5.24/dist/vanta.net.min.js'); })
      .then(function () {
        if (!window.VANTA || !window.VANTA.NET) return;
        window.VANTA.NET({
          el: '#vanta-bg',
          mouseControls: true,
          touchControls: true,
          gyroControls: false,
          minHeight: 200.00,
          minWidth: 200.00,
          scale: 1.00,
          scaleMobile: 1.00,
          color: 0xf59e0b,
          backgroundColor: 0x090604,
          points: 16.00,
          maxDistance: 25.00,
          spacing: 18.00
        });
      })
      .catch(function () {
        bg.classList.add('vanta-fallback');
      });
  }

  function init() {
    highlightActive();
    bindMobile();
    stampYear();
    initInteractiveBackground();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
