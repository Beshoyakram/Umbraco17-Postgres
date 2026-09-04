(() => {
  const root = document.querySelector(".blog-detail .blog-full-body");
  if (!root) return;

  // Animate charts into view
  const charts = root.querySelectorAll(".reveal-chart, .rcm-chart");
  if ("IntersectionObserver" in window && charts.length) {
    const chartObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("in-view");
            chartObserver.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.2 }
    );
    charts.forEach((chart) => chartObserver.observe(chart));
  } else {
    charts.forEach((chart) => chart.classList.add("in-view"));
  }

  // Denial bar click feedback
  root.querySelectorAll(".denial-bar").forEach((bar) => {
    bar.addEventListener("click", () => {
      root.querySelectorAll(".denial-bar").forEach((item) => {
        item.style.filter = "";
      });
      bar.style.filter = "saturate(1.3) brightness(.92)";
    });
  });

  // Workload calculator
  const claims = root.querySelector("#claims");
  const rate = root.querySelector("#rate");
  if (claims && rate) {
    const format = new Intl.NumberFormat("en-US");
    const calculate = () => {
      const denied = Math.round((Number(claims.value) * Number(rate.value)) / 100);
      const hours = (denied * 20) / 60;
      const claimsOut = root.querySelector("#claims-out");
      const rateOut = root.querySelector("#rate-out");
      const denialsOut = root.querySelector("#denials-out");
      const hoursOut = root.querySelector("#hours-out");
      const daysOut = root.querySelector("#days-out");
      if (claimsOut) claimsOut.textContent = format.format(Number(claims.value));
      if (rateOut) rateOut.textContent = rate.value + "%";
      if (denialsOut) denialsOut.textContent = format.format(denied);
      if (hoursOut) hoursOut.textContent = format.format(Math.round(hours));
      if (daysOut) daysOut.textContent = (hours / 8).toFixed(1);
    };
    claims.addEventListener("input", calculate);
    rate.addEventListener("input", calculate);
    calculate();
  }
})();
