(function () {
  function initAos() {
    if (typeof AOS === 'undefined') return;
    AOS.init({ duration: 800, offset: 80, once: false });
    AOS.refresh();
  }

  initAos();

  window.addEventListener('load', function () {
    if (typeof AOS !== 'undefined') AOS.refresh();
  });

  window.addEventListener('pageshow', function (event) {
    if (typeof AOS === 'undefined') return;
    if (event.persisted) {
      document.querySelectorAll('[data-aos]').forEach(function (el) {
        el.classList.remove('aos-animate');
      });
    }
    AOS.refresh();
  });

  var tracks = document.querySelectorAll('.logo-slid .slide-track');
  if (tracks.length && 'IntersectionObserver' in window) {
    var trackObserver = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        var track = entry.target;
        if (entry.isIntersecting) {
          track.classList.add('is-in-view');
          track.querySelectorAll('img').forEach(function (img) {
            img.style.animation = 'none';
            void img.offsetWidth;
            img.style.animation = '';
          });
        } else {
          track.classList.remove('is-in-view');
        }
      });
    }, { threshold: 0.15 });

    tracks.forEach(function (track) { trackObserver.observe(track); });
  }

  var footer = document.querySelector('.footer.footer_bg');
  if (footer && 'IntersectionObserver' in window) {
    var footerObserver = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting && typeof AOS !== 'undefined') {
          AOS.refresh();
        }
      });
    }, { threshold: 0.08 });
    footerObserver.observe(footer);
  }

  var projectSection = document.querySelector('.lastest_project');
  if (projectSection && 'IntersectionObserver' in window && typeof AOS !== 'undefined') {
    var projectObserver = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          AOS.refresh();
        }
      });
    }, { threshold: 0.1 });
    projectObserver.observe(projectSection);
  }
})();
