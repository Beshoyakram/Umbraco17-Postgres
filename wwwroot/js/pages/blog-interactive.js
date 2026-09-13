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

  // Contact Center Staffing Calculator
  const seatPlanner = root.querySelector("#centro-seat-planner");
  if (seatPlanner && !seatPlanner.dataset.calcInitialized) {
    seatPlanner.dataset.calcInitialized = "true";

    const byId = (id) => seatPlanner.querySelector("#" + id);
    const checkedValue = (name) => {
      const item = seatPlanner.querySelector('input[name="' + name + '"]:checked');
      return item ? item.value : "";
    };
    const formatNumber = (value) => Math.round(value).toLocaleString("en-US");
    const titleCase = (value) => value.charAt(0).toUpperCase() + value.slice(1);

    const inputs = seatPlanner.querySelectorAll("input, select");
    const contacts = byId("csp-contacts");
    const aht = byId("csp-aht");
    const hours = byId("csp-hours");
    const days = byId("csp-days");
    const occupancy = byId("csp-occupancy");
    const shrinkage = byId("csp-shrinkage");
    const need = byId("csp-need");
    const launch = byId("csp-launch");
    let currentBrief = "";
    let toastTimer;

    const demandFactors = { steady: 1, variable: 1.12, high: 1.25 };
    const serviceFactors = { standard: 1, responsive: 1.10, priority: 1.20 };

    function paintRange(input) {
      if (!input) return;
      const min = Number(input.min);
      const max = Number(input.max);
      const progress = ((Number(input.value) - min) / (max - min)) * 100;
      input.style.setProperty("--range-progress", progress + "%");
    }

    function getBand(seats) {
      if (seats <= 10) return "Focused team";
      if (seats <= 30) return "Growth team";
      if (seats <= 75) return "Scaled program";
      return "Enterprise program";
    }

    function getScale(maxSeats) {
      const options = [10, 25, 50, 100, 250, 500, 1000, 2000];
      let chosen = options[options.length - 1];
      for (let i = 0; i < options.length; i++) {
        if (maxSeats <= options[i]) {
          chosen = options[i];
          break;
        }
      }
      return chosen;
    }

    function update() {
      if (!contacts || !aht || !hours || !days || !occupancy || !shrinkage) return;
      const monthlyContacts = Number(contacts.value);
      const handleMinutes = Number(aht.value);
      const hoursPerDay = Number(hours.value);
      const daysPerWeek = Number(days.value);
      const occupancyRate = Number(occupancy.value);
      const shrinkageRate = Number(shrinkage.value);
      const demand = checkedValue("csp-demand") || "variable";
      const service = checkedValue("csp-service") || "standard";

      const monthlyScheduledHours = 173.2;
      const workloadHours = (monthlyContacts * handleMinutes) / 60;
      const productiveHoursPerFte = monthlyScheduledHours * occupancyRate * (1 - shrinkageRate);
      let workloadSeats = workloadHours / productiveHoursPerFte;
      workloadSeats = workloadSeats * (demandFactors[demand] || 1) * (serviceFactors[service] || 1);

      const coverageHoursPerMonth = hoursPerDay * daysPerWeek * 4.33;
      const coverageFloor = coverageHoursPerMonth / (monthlyScheduledHours * (1 - shrinkageRate));
      const baseSeats = Math.max(workloadSeats, coverageFloor);
      const lowSeats = Math.max(1, Math.ceil(baseSeats));
      const highSeats = Math.max(lowSeats + 1, Math.ceil(baseSeats * 1.15));
      const scaleMax = getScale(highSeats);
      const barWidth = Math.max(4, Math.min(100, (highSeats / scaleMax) * 100));
      const driver = workloadSeats >= coverageFloor ? "Workload capacity" : "Coverage continuity";

      const contactsVal = byId("csp-contacts-value");
      const ahtVal = byId("csp-aht-value");
      const seatLow = byId("csp-seat-low");
      const seatHigh = byId("csp-seat-high");
      const seatFill = byId("csp-seat-fill");
      const scaleQuarter = byId("csp-scale-quarter");
      const scaleHalf = byId("csp-scale-half");
      const scaleMaxEl = byId("csp-scale-max");
      const workloadEl = byId("csp-workload");
      const coverageEl = byId("csp-coverage");
      const driverEl = byId("csp-driver");
      const bandEl = byId("csp-band");

      if (contactsVal) contactsVal.textContent = formatNumber(monthlyContacts);
      if (ahtVal) ahtVal.textContent = handleMinutes + (handleMinutes === 1 ? " minute" : " minutes");
      if (seatLow) seatLow.textContent = formatNumber(lowSeats);
      if (seatHigh) seatHigh.textContent = formatNumber(highSeats);
      if (seatFill) seatFill.style.width = barWidth + "%";
      if (scaleQuarter) scaleQuarter.textContent = formatNumber(Math.round(scaleMax / 4));
      if (scaleHalf) scaleHalf.textContent = formatNumber(Math.round(scaleMax / 2));
      if (scaleMaxEl) scaleMaxEl.textContent = formatNumber(scaleMax) + (highSeats > 1000 ? "+" : "");
      if (workloadEl) workloadEl.textContent = formatNumber(workloadHours) + " hours";
      if (coverageEl) coverageEl.textContent = hoursPerDay + "h \u00D7 " + daysPerWeek + " days";
      if (driverEl) driverEl.textContent = driver;
      if (bandEl) bandEl.textContent = getBand(highSeats);

      const launchSentence = launch && launch.value === "Exploring options"
        ? "We are currently exploring options."
        : "Our preferred launch window is " + (launch ? launch.value.toLowerCase() : "") + ".";

      currentBrief =
        "We are planning for approximately " + formatNumber(lowSeats) + "\u2013" + formatNumber(highSeats) +
        " dedicated seats for " + (need ? need.value.toLowerCase() : "customer service") +
        ", based on " + formatNumber(monthlyContacts) +
        " monthly interactions, a " + handleMinutes + "-minute average handling time and " +
        hoursPerDay + "-hour coverage across " + daysPerWeek + " days. " + launchSentence;

      const briefText = byId("csp-brief-text");
      if (briefText) briefText.textContent = currentBrief;

      const consultLink = byId("csp-consult-link");
      if (consultLink) {
        const params = new URLSearchParams({
          source: "seat-planner",
          seats: lowSeats + "-" + highSeats,
          contacts: monthlyContacts,
          coverage: hoursPerDay + "h-" + daysPerWeek + "d",
          service: titleCase(service),
          need: need ? need.value : "",
          launch: launch ? launch.value : ""
        });
        consultLink.href = "https://centrocdx.com/get-in-touch/?" + params.toString();
      }

      paintRange(contacts);
      paintRange(aht);
    }

    function showToast(message) {
      const toast = byId("csp-toast") || root.querySelector("#csp-toast");
      if (!toast) return;
      toast.textContent = message;
      toast.classList.add("is-visible");
      window.clearTimeout(toastTimer);
      toastTimer = window.setTimeout(() => toast.classList.remove("is-visible"), 2200);
    }

    function copyBrief() {
      if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(currentBrief).then(() => showToast("Planning brief copied.")).catch(fallbackCopy);
      } else {
        fallbackCopy();
      }
      function fallbackCopy() {
        const field = document.createElement("textarea");
        field.value = currentBrief;
        field.setAttribute("readonly", "");
        field.style.position = "fixed";
        field.style.opacity = "0";
        document.body.appendChild(field);
        field.select();
        const copied = document.execCommand("copy");
        document.body.removeChild(field);
        showToast(copied ? "Planning brief copied." : "Select and copy the brief above.");
      }
    }

    inputs.forEach((input) => {
      input.addEventListener("input", update);
      input.addEventListener("change", update);
    });

    const copyBtn = byId("csp-copy-brief");
    if (copyBtn) copyBtn.addEventListener("click", copyBrief);

    update();
  }

  // Reading progress bar
  const progress = document.querySelector(".rcm-progress span");
  if (progress) {
    const updateProgress = () => {
      const doc = document.documentElement;
      const available = doc.scrollHeight - doc.clientHeight;
      progress.style.transform = "scaleX(" + (available > 0 ? Math.min(1, doc.scrollTop / available) : 0) + ")";
    };
    window.addEventListener("scroll", updateProgress, { passive: true });
    updateProgress();
  }

  // Sticky rail navigation buttons/links
  const railButtons = root.querySelectorAll(".rcm-rail button, .rcm-rail a[data-target], .rcm-rail a[href^='#']");
  if (railButtons.length) {
    railButtons.forEach((btn) => {
      btn.addEventListener("click", (e) => {
        const targetId = btn.dataset.target || (btn.getAttribute("href") || "").replace(/^#/, "");
        const targetEl = document.getElementById(targetId);
        if (targetEl) {
          e.preventDefault();
          targetEl.scrollIntoView({ behavior: "smooth", block: "start" });
          railButtons.forEach((b) => b.classList.remove("active"));
          btn.classList.add("active");
        }
      });
    });

    const targetSections = [...railButtons].map((btn) => {
      const targetId = btn.dataset.target || (btn.getAttribute("href") || "").replace(/^#/, "");
      return document.getElementById(targetId);
    }).filter(Boolean);

    if ("IntersectionObserver" in window && targetSections.length) {
      const sectionObserver = new IntersectionObserver(
        (entries) => {
          const visible = entries
            .filter((entry) => entry.isIntersecting)
            .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
          if (!visible) return;
          railButtons.forEach((btn) => {
            const targetId = btn.dataset.target || (btn.getAttribute("href") || "").replace(/^#/, "");
            btn.classList.toggle("active", targetId === visible.target.id);
          });
        },
        { rootMargin: "-18% 0px -62% 0px", threshold: [0, 0.15, 0.4] }
      );
      targetSections.forEach((sec) => sectionObserver.observe(sec));
    }
  }
})();
