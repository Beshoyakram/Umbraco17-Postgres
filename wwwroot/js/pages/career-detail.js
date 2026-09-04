(function () {
  var form = document.getElementById('centro-career-apply-form');
  if (!form) return;

  var submitBtn = document.getElementById('career-apply-submit-btn');
  var responseMsg = document.getElementById('career-apply-response');
  var defaultLabel = submitBtn ? submitBtn.innerText : 'Submit';

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    if (!submitBtn || !responseMsg) return;

    responseMsg.classList.remove('is-visible', 'is-success', 'is-error');
    submitBtn.innerText = 'Submitting...';
    submitBtn.disabled = true;

    try {
      var formData = new FormData(form);
      var response = await fetch('/careers/apply', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });

      var data = await response.json();
      responseMsg.classList.add('is-visible', data.success ? 'is-success' : 'is-error');
      responseMsg.innerText = data.message || (data.success ? 'Application submitted.' : 'Could not submit application.');

      if (data.success) {
        form.reset();
      }
    } catch (err) {
      responseMsg.classList.add('is-visible', 'is-error');
      responseMsg.innerText = 'Something went wrong. Please try again later.';
    } finally {
      submitBtn.innerText = defaultLabel;
      submitBtn.disabled = false;
    }
  });
})();
