  // reveal on scroll
  const io = new IntersectionObserver(entries => {
    entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); } });
  }, { threshold: 0.12 });
  document.querySelectorAll('.reveal').forEach(el => io.observe(el));

  // form validation + submit
  const form = document.getElementById('ccwForm');
  const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    let ok = true;
    form.querySelectorAll('[data-field]').forEach(g => {
      const ctl = g.querySelector('input,select');
      let valid = ctl.value.trim() !== '';
      if (valid && ctl.type === 'email') valid = emailRe.test(ctl.value.trim());
      g.classList.toggle('invalid', !valid);
      if (!valid) ok = false;
    });
    if (!ok) { form.querySelector('.invalid')?.scrollIntoView({behavior:'smooth', block:'center'}); return; }

    const action = form.dataset.zohoAction;
    if (action) {
      try {
        await fetch(action, { method: 'POST', mode: 'no-cors', body: new FormData(form) });
      } catch (err) { /* still show success; Zoho no-cors gives opaque response */ }
    }
    document.getElementById('formFields').style.display = 'none';
    document.getElementById('formSuccess').style.display = 'block';
    form.scrollIntoView({behavior:'smooth', block:'center'});
  });

  // video testimonial: swap poster for iframe on click (set data-embed first)
  const vf = document.getElementById('videoFrame');
  function playVideo(){
    const src = vf.dataset.embed;
    if (!src) return; // no embed URL configured yet
    vf.innerHTML = '<iframe src="' + src + (src.includes('?') ? '&' : '?') + 'autoplay=1" allow="autoplay; encrypted-media; picture-in-picture" allowfullscreen></iframe>';
  }
  vf.addEventListener('click', playVideo);
  vf.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); playVideo(); } });

  // clear error state while typing
  form.querySelectorAll('input,select').forEach(ctl => {
    ctl.addEventListener('input', () => ctl.closest('[data-field]')?.classList.remove('invalid'));
  });