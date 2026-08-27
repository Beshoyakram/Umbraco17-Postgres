(() => {
  const header = document.querySelector("[data-header]");
  const toggle = document.querySelector("[data-nav-toggle]");
  const nav = document.querySelector("[data-nav]");

  if (toggle && nav) {
    toggle.addEventListener("click", () => {
      const open = nav.classList.toggle("is-open");
      toggle.setAttribute("aria-expanded", open ? "true" : "false");
    });

    nav.querySelectorAll("a").forEach((link) => {
      link.addEventListener("click", () => {
        nav.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
      });
    });
  }

  if (header) {
    const onScroll = () => {
      header.classList.toggle("is-scrolled", window.scrollY > 12);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
  }

  const revealItems = document.querySelectorAll("[data-reveal]");
  if (revealItems.length && "IntersectionObserver" in window) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.16 }
    );
    revealItems.forEach((item) => observer.observe(item));
  } else {
    revealItems.forEach((item) => item.classList.add("is-visible"));
  }

  const slider = document.querySelector("[data-testimonials]");
  const dotsWrap = document.querySelector("[data-testimonial-dots]");
  if (slider && dotsWrap) {
    const quotes = Array.from(slider.querySelectorAll(".c-quote"));
    let index = 0;
    let timer;

    const show = (next) => {
      quotes.forEach((quote, i) => quote.classList.toggle("is-active", i === next));
      dotsWrap.querySelectorAll("button").forEach((dot, i) => {
        dot.classList.toggle("is-active", i === next);
      });
      index = next;
    };

    quotes.forEach((_, i) => {
      const button = document.createElement("button");
      button.type = "button";
      button.setAttribute("aria-label", `Show testimonial ${i + 1}`);
      if (i === 0) button.classList.add("is-active");
      button.addEventListener("click", () => {
        show(i);
        restart();
      });
      dotsWrap.appendChild(button);
    });

    const restart = () => {
      clearInterval(timer);
      timer = setInterval(() => show((index + 1) % quotes.length), 6500);
    };

    restart();
  }
})();
