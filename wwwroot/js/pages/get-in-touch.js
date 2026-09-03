(function () {
  var form = document.getElementById('centro-get-in-touch-form');
  if (!form) return;

  var submitBtn = document.getElementById('get-in-touch-submit-btn');
  var responseMsg = document.getElementById('form-response-message');

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    if (!submitBtn || !responseMsg) return;

    responseMsg.classList.remove('is-visible', 'is-success', 'is-error');
    submitBtn.innerText = 'Sending...';
    submitBtn.disabled = true;

    try {
      var formData = new FormData(form);
      var response = await fetch('/contact/submit', {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });

      var data = await response.json();
      responseMsg.classList.add('is-visible', data.success ? 'is-success' : 'is-error');
      responseMsg.innerText = data.message || (data.success ? 'Message sent.' : 'Could not send message.');

      if (data.success) {
        form.reset();
      }
    } catch (err) {
      responseMsg.classList.add('is-visible', 'is-error');
      responseMsg.innerText = 'Something went wrong. Please try again later.';
    } finally {
      submitBtn.innerText = 'Send';
      submitBtn.disabled = false;
    }
  });
})();
