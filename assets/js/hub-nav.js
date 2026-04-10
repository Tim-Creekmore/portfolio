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

  function init() {
    highlightActive();
    bindMobile();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
