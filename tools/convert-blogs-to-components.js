const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const CONTENT_KEYS = {
  blogHtmlBlock: 'a7010006-aaaa-4bbb-8ccc-000000000001',
  blogRichTextBlock: 'a7010006-aaaa-4bbb-8ccc-000000000002',
  blogImageBlock: 'a7010006-aaaa-4bbb-8ccc-000000000003',
  blogHeadingBlock: 'a7010006-aaaa-4bbb-8ccc-000000000004',
  blogSectionBlock: 'a7010006-aaaa-4bbb-8ccc-000000000005',
  blogCalloutBlock: 'a7010006-aaaa-4bbb-8ccc-000000000006',
  blogFaqItemBlock: 'a7010006-aaaa-4bbb-8ccc-000000000007',
  blogCtaBlock: 'a7010006-aaaa-4bbb-8ccc-000000000008',
  blogQuoteBlock: 'a7010006-aaaa-4bbb-8ccc-000000000009',
  blogTableBlock: 'a7010006-aaaa-4bbb-8ccc-000000000010',
  blogAuthorBlock: 'a7010006-aaaa-4bbb-8ccc-000000000011',
  blogTocBlock: 'a7010006-aaaa-4bbb-8ccc-000000000012',
  blogStaffingCalculatorBlock: 'a7010006-aaaa-4bbb-8ccc-000000000013'
};

function formatHeadingLevel(levelStr) {
  const norm = (levelStr || 'h2').toLowerCase().trim();
  const valid = norm === 'h3' ? 'h3' : 'h2';
  return JSON.stringify([valid]);
}

function buildBlockListJson(rawBlocks) {
  const contentData = [];
  const expose = [];
  const layoutItems = [];

  for (const b of rawBlocks) {
    const key = crypto.randomUUID();
    const typeKey = CONTENT_KEYS[b.type];
    if (!typeKey) {
      console.warn('Unknown block type:', b.type);
      continue;
    }

    const values = [];
    for (const [alias, val] of Object.entries(b.values)) {
      values.push({
        alias,
        culture: 'en-US',
        editorAlias: null,
        segment: null,
        value: val
      });
    }

    contentData.push({
      contentTypeKey: typeKey,
      key,
      values
    });

    expose.push({
      contentKey: key,
      culture: 'en-US',
      segment: null
    });

    layoutItems.push({
      contentKey: key,
      contentUdi: null,
      settingsKey: null,
      settingsUdi: null
    });
  }

  return {
    contentData,
    settingsData: [],
    expose,
    layout: {
      'Umbraco.BlockList': layoutItems
    }
  };
}

function parseStandardPost(html) {
  const blocks = [];
  let raw = html.trim();
  raw = raw.replace(/^<\/p>\s*/i, '').replace(/\s*<p>\s*$/i, '');

  function cleanHtml(str) {
    if (!str) return '';
    return str
      .replace(/^<\/div>\s*/gi, '')
      .replace(/\s*<div>\s*$/gi, '')
      .replace(/^<\/p>\s*/gi, '')
      .replace(/\s*<p>\s*$/gi, '')
      .trim();
  }

  function pushRichText(text) {
    if (!text) return;
    const clean = cleanHtml(text);
    if (!clean || clean === '<p></p>' || clean === '<p>&nbsp;</p>' || clean === '</p>' || clean === '<p>') return;
    if (clean === '</div>' || clean === '</div></div>') return;
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: clean }
    });
  }

  const tokenRegex = /(<div class="[^"]*rank-math-toc-block[^"]*"[\s\S]*?<\/nav>\s*<\/div>)|(<div id="rank-math-faq"[\s\S]*?<\/div>\s*<\/div>\s*<\/div>\s*<\/div>|<div id="rank-math-faq"[\s\S]*?<\/div>\s*<\/div>\s*<\/div>)|(<figure class="[^"]*wp-block-image[^"]*"[\s\S]*?<\/figure>)|(<figure class="[^"]*wp-block-table[^"]*"[\s\S]*?<\/figure>|<table[\s\S]*?<\/table>)|(<blockquote[\s\S]*?<\/blockquote>)|(<h([23])([^>]*)>([\s\S]*?)<\/h\7>)/gi;

  let lastIndex = 0;
  let match;

  while ((match = tokenRegex.exec(raw)) !== null) {
    const precedingText = raw.slice(lastIndex, match.index);
    pushRichText(precedingText);
    lastIndex = tokenRegex.lastIndex;

    const [full, tocMatch, faqMatch, imgMatch, tableMatch, quoteMatch, hMatch, hLevel, hAttrs, hInner] = match;

    if (tocMatch) {
      const titleM = tocMatch.match(/<h2[^>]*>([\s\S]*?)<\/h2>/i);
      const title = titleM ? titleM[1].replace(/<[^>]+>/g, '').trim() : 'Table of Contents';
      const navM = tocMatch.match(/<nav>([\s\S]*?)<\/nav>/i);
      const linksHtml = navM ? navM[1].trim() : tocMatch;
      blocks.push({
        type: 'blogTocBlock',
        values: { title, linksHtml }
      });
    } else if (faqMatch) {
      const itemRegex = /<div id="([^"]*)" class="[^"]*rank-math-list-item[^"]*"[^>]*>[\s\S]*?<h4 class="[^"]*rank-math-question[^"]*"[^>]*>([\s\S]*?)<\/h4>[\s\S]*?<div class="[^"]*rank-math-answer[^"]*"[^>]*>([\s\S]*?)<\/div>\s*<\/div>/gi;
      let itemM;
      let count = 0;
      while ((itemM = itemRegex.exec(faqMatch)) !== null) {
        count++;
        const q = itemM[2].replace(/<[^>]+>/g, '').trim();
        const a = itemM[3].trim();
        blocks.push({
          type: 'blogFaqItemBlock',
          values: { question: q, answer: a }
        });
      }
      if (count === 0) {
        pushRichText(faqMatch);
      }
    } else if (imgMatch) {
      const srcM = imgMatch.match(/src="([^"]+)"/i);
      const altM = imgMatch.match(/alt="([^"]*)"/i);
      const capM = imgMatch.match(/<figcaption[^>]*>([\s\S]*?)<\/figcaption>/i);
      const src = srcM ? srcM[1] : '';
      const alt = altM ? altM[1] : '';
      const caption = capM ? capM[1].replace(/<[^>]+>/g, '').trim() : '';
      if (src) {
        blocks.push({
          type: 'blogImageBlock',
          values: { imageUrl: src, altText: alt, caption }
        });
      }
    } else if (tableMatch) {
      blocks.push({
        type: 'blogTableBlock',
        values: { caption: '', tableHtml: tableMatch }
      });
    } else if (quoteMatch) {
      const pM = quoteMatch.match(/<p[^>]*>([\s\S]*?)<\/p>/i);
      const citeM = quoteMatch.match(/<cite[^>]*>([\s\S]*?)<\/cite>/i);
      const quote = pM ? pM[1].replace(/<[^>]+>/g, '').trim() : quoteMatch.replace(/<[^>]+>/g, '').trim();
      const attribution = citeM ? citeM[1].replace(/<[^>]+>/g, '').trim() : '';
      blocks.push({
        type: 'blogQuoteBlock',
        values: { quote, attribution }
      });
    } else if (hMatch) {
      const headingText = hInner.replace(/<[^>]+>/g, '').trim();
      const idM = hAttrs.match(/id="([^"]+)"/i);
      const anchorId = idM ? idM[1] : '';

      if (headingText.toLowerCase().includes('about the author')) {
        const remaining = raw.slice(lastIndex);
        const nameM = remaining.match(/<a[^>]*href="([^"]*linkedin[^"]*)"[^>]*>([\s\S]*?)<\/a>/i);
        const textM = remaining.match(/<p[^>]*>([\s\S]*?)<\/p>/i);
        if (textM) {
          const rawBio = textM[1].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
          const name = nameM ? nameM[2].trim() : 'Gehad Ahmed';
          const linkedIn = nameM ? nameM[1] : '';
          blocks.push({
            type: 'blogAuthorBlock',
            values: {
              authorName: name,
              role: 'Senior Content Creator at Centro CDX',
              bio: rawBio,
              linkedInUrl: linkedIn
            }
          });
          lastIndex = raw.length;
          continue;
        }
      }

      blocks.push({
        type: 'blogHeadingBlock',
        values: {
          heading: headingText,
          level: formatHeadingLevel(`h${hLevel}`),
          anchorId: anchorId
        }
      });
    }
  }

  if (lastIndex < raw.length) {
    pushRichText(raw.slice(lastIndex));
  }

  return blocks;
}

function parseStaffingCalculatorPost() {
  const blocks = [];

  blocks.push({
    type: 'blogImageBlock',
    values: {
      imageUrl: 'https://centrocdx.com/wp-content/uploads/2026/09/ChatGPT-Image-Sep-3-2026-01_39_56-PM-1024x512.png',
      altText: 'Contact center staffing calculator',
      caption: ''
    }
  });

  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'What is a contact center staffing calculator?',
      level: formatHeadingLevel('h2'),
      anchorId: 'how-it-works'
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'Core Definition',
      text: 'One seat = one dedicated full-time-equivalent role. The result estimates staffing capacity, not physical workstations or a final commercial quote.'
    }
  });

  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: '<p class="wp-block-paragraph">A contact center staffing calculator estimates the number of support agents needed by combining workload, operating coverage, productive availability and service expectations.</p><p class="wp-block-paragraph">A simple headcount calculation can miss what happens inside a real support operation. Customer demand does not arrive evenly, and promotions, billing cycles, product launches and seasonal peaks can create sharp volume changes inside the same month.</p><p class="wp-block-paragraph">Your staffing plan also needs to account for breaks, coaching, training, leave, quality reviews and other time when an employee is scheduled but not available to handle contacts.</p>'
    }
  });

  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Four variables that can move your seat requirement',
      level: formatHeadingLevel('h2'),
      anchorId: 'factors'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '01',
      title: 'Contact workload',
      anchorId: 'workload',
      body: '<p>Monthly volume multiplied by the average time needed to resolve each interaction creates the base workload.</p>'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '02',
      title: 'Demand variability',
      anchorId: 'demand',
      body: '<p>Evenly distributed contacts are easier to staff than bursts caused by campaigns, launches, renewals or peak seasons.</p>'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '03',
      title: 'Availability',
      anchorId: 'availability',
      body: '<p>Occupancy and shrinkage reflect the productive time available after normal operational requirements are considered.</p>'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '04',
      title: 'Service expectation',
      anchorId: 'service',
      body: '<p>Faster response targets usually require more capacity than a model designed around standard turnaround times.</p>'
    }
  });

  blocks.push({
    type: 'blogStaffingCalculatorBlock',
    values: {
      heading: 'Contact Center Staffing Calculator: Build Your First Staffing Range.',
      subheading: 'Adjust the inputs to reflect a typical month. The calculator converts your workload and coverage assumptions into an estimated staffing range instantly.',
      consultationUrl: 'https://centrocdx.com/get-in-touch/'
    }
  });

  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'How many customer support agents do you need?',
      level: formatHeadingLevel('h2'),
      anchorId: 'how-many-agents'
    }
  });

  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: '<p class="wp-block-paragraph">The answer depends on more than monthly volume. Workload, coverage hours, occupancy, shrinkage, response targets and demand peaks all affect the number of full-time-equivalent roles required.</p><p class="wp-block-paragraph">Start by converting monthly interactions and average handling time into total workload hours. Then adjust for the share of scheduled time agents can realistically spend handling customer contacts.</p><p class="wp-block-paragraph">Coverage requirements and demand variability can increase the staffing range further, especially when your operation needs extended hours, seven-day support or faster response targets.</p>'
    }
  });

  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'How to calculate contact center staffing requirements',
      level: formatHeadingLevel('h2'),
      anchorId: 'how-to-calculate'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '1',
      title: 'Calculate base workload',
      anchorId: 'step-1',
      body: '<p>Monthly interactions are multiplied by average handling time to estimate total handling hours.</p>'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '2',
      title: 'Adjust available capacity',
      anchorId: 'step-2',
      body: '<p>Occupancy and shrinkage reduce the productive hours available from each full-time-equivalent role.</p>'
    }
  });

  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '3',
      title: 'Add planning protection',
      anchorId: 'step-3',
      body: '<p>Demand variability, response expectations and the selected coverage model shape the final range.</p>'
    }
  });

  const faqs = [
    {
      q: 'How many customer support agents do I need?',
      a: 'The number of agents you need depends on contact volume, average handling time, operating coverage, occupancy, shrinkage, service expectations and demand variability. Use the calculator above for an initial staffing range, then validate it with interval-level demand and scheduling data.'
    },
    {
      q: 'How do you calculate contact center staffing requirements?',
      a: 'Begin with total workload by multiplying interaction volume by average handling time. Then adjust the productive capacity of each full-time-equivalent role for occupancy and shrinkage, and add protection for coverage requirements, demand peaks and response targets.'
    },
    {
      q: 'Why does the calculator show a range instead of one number?',
      a: 'An exact staffing requirement depends on interval-level demand, schedules and service targets. A range is more honest at the early planning stage and gives your operations team room to validate the assumptions.'
    },
    {
      q: 'What should be included in average handling time?',
      a: 'Use the average active work required per interaction, including talk or response time and the related after-contact work. For a blended channel model, use a workload-weighted average.'
    },
    {
      q: 'What do occupancy and shrinkage mean?',
      a: 'Occupancy is the share of available handling time spent actively working on contacts. Shrinkage represents scheduled time unavailable for contact handling, such as coaching, training, leave, meetings and breaks.'
    },
    {
      q: 'Is this a pricing quote?',
      a: 'No. The result is a capacity estimate. A commercial proposal needs the service scope, channel mix, operating location, language requirements, technology, schedule and quality requirements.'
    }
  ];

  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Frequently Asked Questions',
      level: formatHeadingLevel('h2'),
      anchorId: 'faq'
    }
  });

  for (const f of faqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Operations in Action',
      title: 'Capacity is only the start. Execution makes it work.',
      text: 'Centro can help validate the workload, refine the staffing model and design a scalable contact center operation around your service goals, from workforce planning through day-to-day contact center outsourcing.',
      buttonLabel: 'Discuss this plan with Centro',
      buttonLink: JSON.stringify([
        {
          name: 'Discuss this plan with Centro',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

function parseHealthcareRcmPost() {
  const blocks = [];

  // Sticky In-Article Navigation Rail
  blocks.push({
    type: 'blogTocBlock',
    values: {
      title: 'In this article',
      linksHtml: `<ol>
  <li><button type="button" data-target="trend-1" data-no="01">Denial prevention</button></li>
  <li><button type="button" data-target="trend-2" data-no="02">Clean claim rate</button></li>
  <li><button type="button" data-target="trend-3" data-no="03">Prior authorization</button></li>
  <li><button type="button" data-target="trend-4" data-no="04">Payer automation</button></li>
  <li><button type="button" data-target="trend-5" data-no="05">Staffing squeeze</button></li>
  <li><button type="button" data-target="trend-6" data-no="06">AI ROI gap</button></li>
  <li><button type="button" data-target="trend-7" data-no="07">Outsourcing capacity</button></li>
  <li><button type="button" data-target="faq" data-no="FAQ">Questions answered</button></li>
</ol>`
    }
  });

  // Intro prose
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="rcm-intro">
<p>Somewhere in your practice right now, a claim is sitting in a work queue that would have been paid without a second look two years ago.</p>
<p>That’s the shortest possible summary of healthcare revenue cycle management trends in 2026.</p>
<p>Denial rates are climbing, billing teams are thinner, and the technology that was supposed to absorb the difference is still not showing results for most organizations.</p>
<p>Below are the seven trends actually reshaping revenue cycle operations this year, what the data says about each, and what it means for a physician practice, clinic, or hospital billing team.</p>
</div>`
    }
  });

  // At a glance 7 trends navigation grid
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<section class="rcm-at-glance" aria-labelledby="glance-title">
  <span class="rcm-section-label" id="glance-title">Seven trends at a glance</span>
  <div class="rcm-trend-grid">
    <a class="rcm-trend-link" href="#trend-1"><span class="rcm-trend-no">01</span><span><b>Denial prevention replaces denial cleanup</b><small>Denial rates past benchmark, appeals unaffordable</small></span></a>
    <a class="rcm-trend-link" href="#trend-2"><span class="rcm-trend-no">02</span><span><b>Clean claim rate becomes the headline metric</b><small>Leading indicator instead of lagging</small></span></a>
    <a class="rcm-trend-link" href="#trend-3"><span class="rcm-trend-no">03</span><span><b>Prior authorization automation accelerates</b><small>CMS-0057-F API deadline lands January 2027</small></span></a>
    <a class="rcm-trend-link" href="#trend-4"><span class="rcm-trend-no">04</span><span><b>Payer automation outpaces manual claim work</b><small>Machines review claims, people fix rejections</small></span></a>
    <a class="rcm-trend-link" href="#trend-5"><span class="rcm-trend-no">05</span><span><b>The staffing squeeze becomes structural</b><small>More denials, same number of people</small></span></a>
    <a class="rcm-trend-link" href="#trend-6"><span class="rcm-trend-no">06</span><span><b>The AI ROI gap gets named out loud</b><small>Broad adoption, narrow proven return</small></span></a>
    <a class="rcm-trend-link" href="#trend-7"><span class="rcm-trend-no">07</span><span><b>Outsourcing becomes a hiring workaround</b><small>Trained staff without the recruiting cycle</small></span></a>
  </div>
</section>`
    }
  });

  // Trend 01
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '01',
      title: 'Denial Prevention Is Replacing Denial Cleanup',
      anchorId: 'trend-1',
      body: `<p>Every practice knows the 5% to 10% denial benchmark. Far fewer are hitting it.</p>
<p>That range is <a href="https://www.hfma.org/revenue-cycle/kpis/7-kpis-providers-should-be-tracking/" target="_blank" rel="noopener">HFMA’s published KPI standard</a>, with under 5% considered optimal. Denials have been climbing past it for years: <a href="https://www.experian.com/blogs/healthcare/state-of-claims-2025/" target="_blank" rel="noopener">Experian Health</a> found 41% of providers now report rates of 10% or higher, up from 38% in 2024 and 30% in 2022.</p>
<figure class="rcm-chart reveal-chart" aria-labelledby="denial-caption">
  <div class="rcm-chart-head"><figcaption id="denial-caption"><h3>Providers reporting denial rates above 10%</h3><span class="rcm-chart-source">Source: Experian Health, State of Claims</span></figcaption><span class="rcm-chart-note">Select a bar</span></div>
  <div class="denial-chart">
    <div class="denial-axis" aria-hidden="true"><span>45%</span><span>30%</span><span>15%</span><span>0%</span></div>
    <div class="denial-plot">
      <button class="denial-bar" style="--h:66.67%;--bar:#8bbce8" type="button" aria-label="2022: 30 percent"><strong>30%</strong><span>2022</span></button>
      <button class="denial-bar" style="--h:84.44%;--bar:#3f91db" type="button" aria-label="2024: 38 percent"><strong>38%</strong><span>2024</span></button>
      <button class="denial-bar" style="--h:91.11%;--bar:#2369a8" type="button" aria-label="2025: 41 percent"><strong>41%</strong><span>2025</span></button>
    </div>
  </div>
</figure>
<p>For most of that period, the response was to appeal harder. That’s what has changed. Reworking a denial costs real staff hours, and denial volume has grown faster than billing headcount. The claims that lose the triage fight are never resubmitted at all.</p>
<p>Practice leaders now name denials and appeals their single biggest source of revenue cycle leakage in <a href="https://www.mgma.com/mgma-stat/detecting-and-fixing-leaks-across-the-revenue-cycle" target="_blank" rel="noopener">MGMA’s January 2026 Stat poll</a>, ahead of front-end issues, billing, and coding.</p>
<p>That’s why denial management is moving upstream: real-time eligibility at registration, authorization status tied to scheduling, and root-cause categorization by payer so the same denial doesn’t recur every week.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'If your team reports how many appeals it filed but not how many denials it prevented, you are measuring the wrong end of the process. Appeals cost staff hours. Prevention costs a process change.'
    }
  });

  // Trend 02
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '02',
      title: 'Clean Claim Rate Is Becoming the Headline Metric',
      anchorId: 'trend-2',
      body: `<p>Revenue cycle teams are shifting their primary KPI from lagging measures to leading ones, and clean claim rate is where that lands.</p>
<p><a href="https://www.hfma.org/data-and-insights/map-initiative/map-keys/" target="_blank" rel="noopener">HFMA’s MAP Keys</a>, the industry-standard KPI set, define the measure as claims that pass all edits in the claims processing tool without manual intervention.</p>
<p>HFMA’s own KPI guidance points providers toward a 98% clean claims rate, and puts net collection at 95% minimum with 97% to 99% optimal.</p>
<p>The reason it’s displacing the traditional dashboard is timing. Net collection rate and days in A/R both tell you what already went wrong, weeks after the claim left your system. Clean claim rate tells you what is about to go wrong, while intervention is still possible.</p>
<p>One caveat that undercuts a lot of dashboards: measure it at the payer, not the clearinghouse.</p>
<p>A claim that clears your scrubber has passed your own edits, not the payer’s, so clearinghouse-level reporting flatters the number and hides exactly the errors you are trying to catch.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'If you track one number this year, track this one. It’s also the metric any automation vendor or outsourcing partner should be willing to be measured against, provided you agree on the definition first.'
    }
  });

  // Trend 03
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '03',
      title: 'Prior Authorization Automation Is Accelerating on a Regulatory Clock',
      anchorId: 'trend-3',
      body: `<p>Prior authorization is the most expensive manual process left in the revenue cycle, and CMS has now attached a deadline to fixing it.</p>
<p>Start with what it costs practices today. <a href="https://www.mgma.com/articles/the-prior-authorization-landscape-in-2025" target="_blank" rel="noopener">MGMA’s Annual Regulatory Burden Report</a> found 92% of medical groups hired or reassigned staff solely to handle prior authorization volume, 60% said at least three employees touch a single request, and 35% spend upwards of 35 minutes on average per request.</p>
<p>That burden persists because the transaction never got automated the way the rest of the revenue cycle did.</p>
<p>The <a href="https://www.caqh.org/insights/caqh-index-report" target="_blank" rel="noopener">2025 CAQH Index</a> puts electronic adoption of medical prior authorization at <a href="https://www.ajmc.com/view/caqh-index-finds-20-billion-in-cost-savings-opportunities" target="_blank" rel="noopener">40%, up from 31% in the 2023 Index</a> but still far behind claim status inquiry at 81% and claim payment at 78%.</p>
<figure class="rcm-chart reveal-chart" aria-labelledby="electronic-caption">
  <div class="rcm-chart-head"><figcaption id="electronic-caption"><h3>Medical transactions conducted fully electronically</h3><span class="rcm-chart-source">Source: <a href="https://www.caqh.org/insights/caqh-index-report" target="_blank" rel="noopener">2025 CAQH Index</a></span></figcaption><span class="rcm-chart-note">2025 adoption</span></div>
  <div class="bar-chart">
    <div class="hbar-row"><b>Claim status inquiry</b><div class="hbar-track"><div class="hbar-fill" style="--w:81%;--c:#9cc6e9"></div></div><em>81%</em></div>
    <div class="hbar-row"><b>Claim payment</b><div class="hbar-track"><div class="hbar-fill" style="--w:78%;--c:#9cc6e9"></div></div><em>78%</em></div>
    <div class="hbar-row"><b>Prior authorization</b><div class="hbar-track"><div class="hbar-fill" style="--w:40%;--c:#2369a8"></div></div><em>40%</em></div>
  </div>
</figure>
<p>Closing gaps like that one across all transaction types is where CAQH finds <a href="https://www.ajmc.com/view/caqh-index-finds-20-billion-in-cost-savings-opportunities" target="_blank" rel="noopener">more than $20 billion in unrealized savings</a>.</p>
<p>That’s the gap <a href="https://www.cms.gov/initiatives/burden-reduction/overview/interoperability/policies-regulations/cms-interoperability-prior-authorization-final-rule-cms-0057-f" target="_blank" rel="noopener">CMS-0057-F, the Interoperability and Prior Authorization Final Rule</a>, is built to close. It requires impacted payers to stand up FHIR-based Prior Authorization APIs, with the core requirement landing in January 2027.</p>
<p>It compresses decision timelines to 72 hours for expedited requests and seven calendar days for standard ones, and requires payers to publish denial reasons and annual reporting metrics.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'The payer-side infrastructure is being built whether you prepare or not. The rule covers Medicare Advantage, Medicaid and CHIP managed care, state Medicaid and CHIP FFS, and QHP issuers on the federal exchanges, so your workflow stays hybrid, and knowing which of your payers fall inside that scope is the first move.'
    }
  });

  // Trend 04
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '04',
      title: 'Payer Automation Is Outpacing Manual Claim Work',
      anchorId: 'trend-4',
      body: `<p>Insurers check your claims with machines. Your staff fixes the rejections. Those two things don’t scale at the same rate.</p>
<p>When a claim reaches a payer, it runs against thousands of rules in about a second: is this code valid alongside that one, does the diagnosis support the procedure, was authorization on file.</p>
<p>Anything out of place bounces automatically. A human reviewer once applied judgment to borderline claims. Automated review applies the rule.</p>
<p>Your side still runs at human speed. A rejection lands, a biller works out what went wrong, corrects it, and resubmits. One claim, one person, twenty minutes.</p>
<p>Annual code set changes cause spikes on a schedule, and providers feel it: 68% told <a href="https://www.experianplc.com/newsroom/press-releases/2025/experian-health-s-3rd-annual-state-of-claims-survey-finds-denial" target="_blank" rel="noopener">Experian Health</a> that submitting clean claims is harder than a year ago, and 54% said claim errors are increasing.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'You can’t out-staff an automated system. The only place the math works in your favor is before submission, where one process fix prevents every future instance of that error.'
    }
  });

  // Trend 05 with interactive calculator
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '05',
      title: 'The Staffing Squeeze Is Becoming Structural',
      anchorId: 'trend-5',
      body: `<p>Certified coders and experienced AR follow-up staff have been hard to hire and expensive to keep for years.</p>
<p>What changed is the volume of work landing on those thinner teams, and Experian Health’s 2025 survey names staffing shortages alongside rising denials as a core provider concern.</p>
<p>Every denial takes about the same staff time to work, so twice the denials means twice the hours. Your headcount didn’t double. That time comes out of finding the causes, so the same claims keep breaking.</p>
<div class="rcm-calculator" aria-labelledby="calc-title">
  <span class="rcm-section-label">Interactive workload lens</span>
  <h3 id="calc-title">See what denial rework costs in staff time</h3>
  <p>Illustrative estimate based on the article’s 20-minute manual rework example.</p>
  <div class="calc-controls">
    <div class="calc-field"><label for="claims">Monthly claims <output id="claims-out">2,000</output></label><input id="claims" type="range" min="500" max="10000" step="500" value="2000"></div>
    <div class="calc-field"><label for="rate">Denial rate <output id="rate-out">10%</output></label><input id="rate" type="range" min="5" max="20" step="1" value="10"></div>
  </div>
  <div class="calc-result" aria-live="polite">
    <div><strong id="denials-out">200</strong><span>denied claims per month</span></div>
    <div><strong id="hours-out">67</strong><span>rework hours per month</span></div>
    <div><strong id="days-out">8.3</strong><span>eight-hour workdays</span></div>
  </div>
</div>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'You won’t hire your way out of this. Coders are scarce, and denials are growing faster than payroll, which leaves two levers: automate the repetitive work, or add capacity through a partner. Most practices end up using both.'
    }
  });

  // Trend 06 with gap chart
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '06',
      title: 'The AI ROI Gap Is Finally Being Named Out Loud',
      anchorId: 'trend-6',
      body: `<p>For two years, the industry talked about AI adoption. In 2026, it’s talking about returns, and the numbers are uncomfortable.</p>
<p><a href="https://www.hfma.org/technology/most-healthcare-organizations-are-adopting-ai-in-the-revenue-cycle-hfma-poll/" target="_blank" rel="noopener">HFMA and FinThrive research</a> found 63% of healthcare organizations had integrated AI-powered automation into claims processing, but only 15% reported clear positive ROI.</p>
<p>Experian Health’s State of Claims research found 67% of finance professionals believe AI can improve claims processes while only 14% had applied it to denials.</p>
<p>A <a href="https://www.hfma.org/revenue-cycle/denials-management/ai-adoption-in-denials-management-lags-as-other-rcm-uses-expand/" target="_blank" rel="noopener">2025 Bain survey</a> has also put denials-specific AI adoption at roughly one in five providers, well behind documentation support and coding.</p>
<figure class="rcm-chart reveal-chart" aria-labelledby="gap-caption">
  <div class="rcm-chart-head"><figcaption id="gap-caption"><h3>The distance between confidence and proven use</h3><span class="rcm-chart-source">Sources: HFMA and FinThrive (top); Experian Health State of Claims (bottom)</span></figcaption><span class="rcm-chart-note">Percentage points</span></div>
  <div class="gap-chart">
    <div class="gap-row"><span class="gap-label">Integrated AI into claims processing vs. clear positive ROI</span><div class="gap-line" style="--from:15%;--to:63%;--mid:39%"><span class="gap-value from">15%</span><span class="gap-value to">63%</span><span class="gap-points">48-point gap</span></div></div>
    <div class="gap-row"><span class="gap-label">Applied AI to denials vs. believe AI can improve claims</span><div class="gap-line" style="--from:14%;--to:67%;--mid:40.5%"><span class="gap-value from">14%</span><span class="gap-value to">67%</span><span class="gap-points">53-point gap</span></div></div>
  </div>
</figure>
<p>These numbers show us that adoption is broad, and that returns are concentrated in just a small minority. The highest-value use case is the least automated.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'Before you sign anything, write down the number the tool is supposed to move and what it reads today. Clean claim rate, denial rate on one payer, hours spent on eligibility checks.'
    }
  });

  // Trend 07
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '07',
      title: 'Outsourcing Is Shifting From a Cost Play to a Capacity Play',
      anchorId: 'trend-7',
      body: `<p>Outsourcing used to be evaluated mainly on cost. In 2026, practices are reaching for it because they can’t hire fast enough, and a partner comes with coders and AR staff already trained.</p>
<p>The work that moves out is usually the repetitive, high-volume kind: eligibility checks, prior authorization follow-up, coding, charge entry, payment posting, AR follow-up, and appeals.</p>
<p>What has changed is what practices ask before signing. Instead of leading with price, they ask what the partner measures, whether denial patterns get reported back so the front desk can fix them, and how patient data is protected.</p>`
    }
  });

  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'What it means for you',
      text: 'Add up what billing really costs you now. Salaries, benefits, training, software, the weeks a seat sits empty, and the denials nobody had time to work. That total is the number to compare a partner against, not the salary line by itself.'
    }
  });

  // FAQs
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Questions Revenue Cycle Leaders Are Asking',
      level: formatHeadingLevel('h2'),
      anchorId: 'faq'
    }
  });

  const rcmFaqs = [
    {
      q: 'Why are claim denials increasing?',
      a: 'Payers now screen claims automatically, prior authorization rules are enforced more strictly, and annual code updates create fresh mismatches. Most of the increase isn’t sloppy billing.'
    },
    {
      q: 'What is the average denial rate for physician practices?',
      a: 'HFMA puts the industry average at 5% to 10%, with under 5% optimal. Experian Health found 41% of providers are above 10%. Behavioral health, orthopedics, and physical therapy run higher than primary care.'
    },
    {
      q: 'How can you reduce denials without hiring more staff?',
      a: 'Move the work upstream. Verify eligibility at registration, tie authorization tracking to scheduling, and fix the workflow behind your top denial codes.'
    },
    {
      q: 'Is AI worth it for medical billing?',
      a: 'It depends on scope. Broad platform rollouts have a poor track record, with far more organizations adopting AI than reporting clear returns. Narrow uses like eligibility automation pay back faster.'
    },
    {
      q: 'How much does RCM outsourcing cost per claim?',
      a: 'Usually a percentage of collections or a flat rate per claim, depending on specialty and scope. Compare it against what billing costs you today, including benefits, software, and vacant seats.'
    }
  ];

  for (const f of rcmFaqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // Closing CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Revenue cycle assessment',
      title: 'Ready to See Where Your Revenue Cycle Is Leaking?',
      text: `<p>Most practices know they are collecting less than they should. Knowing why is harder. The denial rate creeps up, the same codes keep coming back, and nobody has a free afternoon to trace them.</p>
<p>That’s the work Centro does. We handle eligibility verification, prior authorization follow-up, coding, charge capture, payment posting, AR follow-up, and appeals.</p>
<div class="rcm-services" aria-label="Centro revenue cycle services">
  <span>Eligibility verification</span><span>Prior authorization follow-up</span><span>Medical coding</span><span>Charge capture</span><span>Payment posting</span><span>AR follow-up and appeals</span>
</div>
<p>Because we work within your EHR and deal directly with insurers, denials come back to you with the cause attached and the upstream fix identified.</p>
<p>Centro has run healthcare revenue cycle operations since 2009, across five delivery countries. We stay deliberately boutique, so you keep the same team rather than a rotating account manager, and every engagement is HIPAA compliant.</p>`,
      buttonLabel: 'Contact Centro',
      buttonLink: JSON.stringify([
        {
          name: 'Contact Centro',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  // Disclaimer block below CTA
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="rcm-disclaimer">
  <p><em>Centro is a healthcare business process outsourcing and revenue cycle management company supporting physician practices, clinics, and hospitals across the United States.</em></p>
  <p><em>Centro’s services cover the full revenue cycle from patient access and eligibility verification through medical coding, claims submission, denial management, and accounts receivable follow-up.</em></p>
</div>`
    }
  });

  return blocks;
}

// 1. HOSPITAL 57357
function parseHospital57357Post(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Intro
  const introM = clean.match(/<section class="c57357-intro"[\s\S]*?<\/section>/i);
  if (introM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-57357-story">\n${introM[0]}\n</div>` }
    });
  }

  // Giving
  const givingM = clean.match(/<section class="c57357-giving"[\s\S]*?<\/section>/i);
  if (givingM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-57357-story">\n${givingM[0]}\n</div>` }
    });
  }

  // Path
  const pathM = clean.match(/<section class="c57357-path"[\s\S]*?<\/section>/i);
  if (pathM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-57357-story">\n${pathM[0]}\n</div>` }
    });
  }

  // Impact
  const impactM = clean.match(/<section class="c57357-impact"[\s\S]*?<\/section>/i);
  if (impactM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-57357-story">\n${impactM[0]}\n</div>` }
    });
  }

  // About hospital
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'About Hospital 57357',
      level: formatHeadingLevel('h2'),
      anchorId: 'about-57357'
    }
  });

  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-57357-story"><p class="c57357-section-copy">Children’s Cancer Hospital Egypt 57357 was established in 2007 with a mission focused on combating childhood cancer through quality care, research, and education while providing treatment to children free of charge.</p></div>`
    }
  });

  const faqs = [
    {
      q: 'Built Through Community Support',
      a: 'The hospital was built through donations, demonstrating the impact individuals, organizations, and communities can create when they come together behind a shared purpose.'
    },
    {
      q: 'Specialized Pediatric Oncology Care',
      a: 'Hospital 57357 provides specialized care for children while working to ensure that access to treatment is not determined by a family’s financial circumstances.'
    },
    {
      q: 'Research and Education',
      a: 'Research and education form an important part of the hospital’s mission, supporting the continued development of knowledge and care in pediatric oncology.'
    },
    {
      q: 'Support Beyond Treatment',
      a: 'The hospital’s approach also includes rehabilitation, psychological support, social support, and services designed to help children and their families throughout the care journey.'
    }
  ];

  for (const f of faqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // Closing CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'About Centro',
      title: 'People, Performance, and Meaningful Impact',
      text: `<p>Centro is a global BPO and technology services provider supporting organizations through Contact Center Outsourcing, healthcare operations, and digital transformation. Across our global delivery network, we combine operational expertise with a people-first culture that encourages professional growth, community engagement, and real-world results.</p><div class="c57357-centro__services" aria-label="Centro areas of expertise"><span>Contact Center Outsourcing</span><span>Healthcare Operations</span><span>Digital Transformation</span></div>`,
      buttonLabel: 'Learn More About Centro',
      buttonLink: JSON.stringify([
        {
          name: 'Learn More About Centro',
          url: 'https://centrocdx.com/about',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 2. IAOP POST
function parseIaopPost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<section class="centro-hero"[\s\S]*?<\/section>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-blog-wrap">\n${heroM[0]}\n</div>` }
    });
  }

  // Main Image
  blocks.push({
    type: 'blogImageBlock',
    values: {
      imageUrl: 'https://centrocdx.com/wp-content/uploads/2026/04/Blog-IAOP.png',
      altText: 'Centro recognized in the 2026 IAOP Global Outsourcing 100',
      caption: 'Centro recognized in the 2026 IAOP Global Outsourcing 100'
    }
  });

  // Intro section
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><section class="centro-section"><p>The 2026 recognition represents Centro’s fourth consecutive year receiving recognition from the International Association of Outsourcing Professionals (IAOP), reinforcing the company’s sustained momentum within the global outsourcing industry.</p><p>The Global Outsourcing 100 brings visibility to outsourcing service providers from around the world, spanning established industry leaders and emerging organizations demonstrating strong growth, innovation and market impact.</p></section></div></div>`
    }
  });

  // Recognition card
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><section class="centro-recognition"><div class="centro-recognition-number"><strong>4</strong><span>Consecutive Years</span></div><div class="centro-recognition-copy"><span>IAOP Global Outsourcing 100</span><h2>Consistent recognition. Continued momentum.</h2><p>Centro’s fourth consecutive recognition reflects the continued development of its global delivery capabilities and commitment to high-value outsourcing outcomes.</p></div></section></div></div>`
    }
  });

  // Heading: Recognition Built on Outsourcing Excellence
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Recognition Built on Outsourcing Excellence',
      level: formatHeadingLevel('h2'),
      anchorId: 'recognition-excellence'
    }
  });

  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><p>IAOP’s Global Outsourcing 100 is designed to recognize outsourcing providers demonstrating the capabilities, vision and performance required in an evolving global services market.</p><p>For Centro, repeated recognition reflects a broader commitment to building scalable operations, developing world-class talent and delivering measurable value for clients across international markets.</p></div></div>`
    }
  });

  // Quote
  blocks.push({
    type: 'blogQuoteBlock',
    values: {
      quote: '“Being recognized as a Rising Star by the IAOP Global 100 for the fourth year running is a powerful validation of our team’s relentless drive and our passion for outsourcing excellence. This honor pushes us to keep raising the bar for what a modern outsourcing company can achieve for our clients.”',
      attribution: 'Hesham Farag, Executive Chairman & Founder, Centro'
    }
  });

  // Heading: What This Recognition Represents
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'What This Recognition Represents',
      level: formatHeadingLevel('h2'),
      anchorId: 'recognition-represents'
    }
  });

  // Impact 3 cards
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><p>Centro continues to strengthen a global delivery model designed to support organizations with scalable outsourcing capabilities across customer experience, technical support and back-office operations.</p><div class="centro-impact-grid"><div class="centro-impact-card"><span class="centro-impact-index">01</span><h3>Global Delivery</h3><p>A delivery model built to support international operations and changing business requirements.</p></div><div class="centro-impact-card"><span class="centro-impact-index">02</span><h3>Scalable Operations</h3><p>Outsourcing capabilities designed to grow alongside client demand and operational complexity.</p></div><div class="centro-impact-card"><span class="centro-impact-index">03</span><h3>Client Outcomes</h3><p>A continued focus on execution, performance and meaningful business impact.</p></div></div></div></div>`
    }
  });

  // Heading: A Shared Achievement
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'A Shared Achievement',
      level: formatHeadingLevel('h2'),
      anchorId: 'shared-achievement'
    }
  });

  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><p>This recognition belongs to the teams behind Centro’s operations and to the clients who continue to place their trust in the organization.</p><p>As Centro continues to expand its capabilities, the focus remains on developing people, strengthening operations and raising the standard of what businesses can expect from a modern outsourcing partner.</p></div></div>`
    }
  });

  // About IAOP
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="centro-blog-wrap"><div class="centro-content-width"><section class="centro-about-box"><div><h2>About IAOP</h2><p>IAOP is a global association for outsourcing and business services professionals, bringing together customers, providers and advisors through professional development, industry collaboration, recognition and shared expertise.</p></div><a class="centro-about-mark" href="https://www.iaop.org/" target="_blank" rel="noopener" aria-label="Visit the IAOP website"><span>IAOP</span></a></section></div></div>`
    }
  });

  // FAQs
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Frequently Asked Questions',
      level: formatHeadingLevel('h2'),
      anchorId: 'faq'
    }
  });

  const iaopFaqs = [
    {
      q: 'Has Centro won BPO industry awards?',
      a: 'Yes. Centro has received recognition from major organizations within the outsourcing industry, including repeated recognition in the IAOP Global Outsourcing 100.'
    },
    {
      q: 'What is the IAOP Global Outsourcing 100?',
      a: 'The Global Outsourcing 100 is an annual IAOP program recognizing outsourcing service providers from around the world, including established leaders and Rising Stars.'
    },
    {
      q: 'What does IAOP Rising Star recognition mean?',
      a: 'Rising Star is a category within the Global Outsourcing 100 that recognizes outsourcing providers demonstrating notable growth, momentum and developing market leadership.'
    }
  ];

  for (const f of iaopFaqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Build With Centro',
      title: 'Looking for an outsourcing partner built for what comes next?',
      text: 'Explore Centro’s Business Process Outsourcing capabilities and discover how our teams support scalable, high-performing operations.',
      buttonLabel: 'Explore Our Solutions',
      buttonLink: JSON.stringify([
        {
          name: 'Explore Our Solutions',
          url: 'https://centrocdx.com/solution/bpo',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 3. STUDENT EXPERIENCE POST
function parseStudentExperiencePost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Intro prose
  blocks.push({
    type: 'blogRichTextBlock',
    values: {
      text: `<div class="sxp"><p class="rv">Higher education leaders often mistake a quiet inbox for a satisfied student body. In reality, silence usually signals a student who has already checked out mentally. By the time the withdrawal form hits the registrar, the opportunity to intervene has vanished.</p><p class="rv">A consistent student support services experience acts as an early warning system for disengagement. This strategy replaces reactive crisis management with a coherent, invisible safety net.</p><p class="rv">The following steps detail how to build a support structure that anticipates needs before they become crises.</p></div>`
    }
  });

  // TOC Rail
  blocks.push({
    type: 'blogTocBlock',
    values: {
      title: 'The five steps',
      linksHtml: `<ol><li><a href="#sxpS1">Map the full student journey</a></li><li><a href="#sxpS2">Build engagement into the structure</a></li><li><a href="#sxpS3">Address the administrative burden</a></li><li><a href="#sxpS4">Use student lifecycle management</a></li><li><a href="#sxpS5">Design services around outcomes</a></li></ol>`
    }
  });

  // Step 1
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '01',
      title: 'Map the Full Student Journey',
      anchorId: 'sxpS1',
      body: `<p>Optimization is impossible without clear visibility into the current <a href="https://centrocdx.com/the-hidden-risk-of-inconsistent-customer-responses-across-channels/">student experience</a>. Student journey mapping serves as the essential first step that most institutions overlook. This exercise captures every touchpoint from the first inquiry to orientation and beyond.</p><p>Effective journey mapping captures every touchpoint: first inquiry, application, orientation, academic advising, and beyond. The goal is to identify where students consistently struggle, feel confused, or disengage.</p><div class="sxp-cards rv"><div class="sxp-card"><b>Students do not know what is available to them</b><p>The <a href="https://www.insidehighered.com/news/student-success/academic-life/2024/09/24/survey-gaps-persist-college-student-resource" target="_blank" rel="noopener">Listening to Learners 2024 survey</a> found that fewer than half of students were aware of academic advising or career counseling at their institution.</p></div><div class="sxp-card"><b>Help arrives too late to change a decision</b><p>Those who leave are the least likely to have accessed the support available. They either never found the resources or reached out after they had already checked out.</p></div><div class="sxp-card"><b>Departmental inconsistency creates a fragmented experience</b><p>A fast financial aid office matters little if the registrar takes weeks to reply. Mixed signals leave students feeling like they are navigating two different institutions.</p></div></div><p>Journey mapping allows you to identify and fix any outstanding issues before they cause lasting damage.</p>`
    }
  });

  // Step 2
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '02',
      title: 'Build Student Engagement Strategies Into the Structure',
      anchorId: 'sxpS2',
      body: `<p>Real engagement requires more than an occasional email. Sending a list of resources once a term is a passive approach that students quickly overlook, so it doesn’t really work.</p><p>In order for support to work, it has to be a part of their daily routine. Student engagement strategies must be embedded in core operations, not just a layer of occasional outreach.</p><div class="sxp-list rv"><div><strong>Proactive intervention.</strong> Initiating contact the moment attendance or platform activity fluctuates, ensuring support is offered before the student disengages.</div><div><strong>Immediate outreach.</strong> Stepping in when login frequency or engagement patterns shift, to address friction in real time.</div><div><strong>Early-stage engagement.</strong> Addressing changes in activity immediately, so students stay connected to their coursework.</div><div><strong>Real-time support.</strong> Reaching out as soon as participation patterns change, providing help at the exact moment it is needed.</div></div><div class="sxp-band rv"><span class="sxp-band-n">1 in 3</span><p>enrolled students still considered withdrawing, according to the Lumina Foundation and Gallup State of Higher Education Study. Emotional stress and cost concerns drive most of it, and proactive engagement that addresses those pressures early has a measurable impact on whether students stay.</p></div>`
    }
  });

  // Step 3
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '03',
      title: 'Address the Administrative Burden',
      anchorId: 'sxpS3',
      body: `<p>Administrative friction affects staff as much as students. Inefficient processes, including manual case tracking, disconnected platforms, and repeated handoffs between departments, consume time that could go toward actual student support.</p><p>So when advisors spend half their day on administrative tasks, fewer students get the attention they need.</p><p>This is especially acute during peak enrollment periods, when inquiry volumes skyrocket and response times are stretched more than normal. Students who can’t get timely answers during registration make decisions, sometimes the wrong ones, based on incomplete information.</p><p>A well-designed student help desk function, backed by consistent knowledge systems, reduces both staff burden and student frustration at once.</p>`
    }
  });

  // Step 4
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '04',
      title: 'Use Student Lifecycle Management',
      anchorId: 'sxpS4',
      body: `<p>Consistency across the full student journey requires a framework that spans enrollment to graduation, not just the first semester, and that’s where student lifecycle management becomes essential.</p><p>Lifecycle management means treating enrollment, early semesters, mid-program, the final year, and the alumni transition as a continuous sequence. The student’s history stays with them across every department, allowing support to grow more precise at every milestone.</p>`
    }
  });

  // Step 5
  blocks.push({
    type: 'blogSectionBlock',
    values: {
      number: '05',
      title: 'Design Student Support Services Around Outcomes',
      anchorId: 'sxpS5',
      body: `<p>Support services are only as good as the outcomes they produce. Tracking activity metrics—such as tickets closed or emails sent—reveals nothing about whether a student actually stayed enrolled or completed their degree.</p><p>Connect every support intervention to retention, satisfaction, and progression. When support teams know the downstream impact of their work, they solve problems rather than just closing tickets.</p>`
    }
  });

  // FAQs
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Frequently Asked Questions',
      level: formatHeadingLevel('h2'),
      anchorId: 'faq'
    }
  });

  const sxpFaqs = [
    {
      q: 'What are student support services?',
      a: 'Student support services encompass academic advising, financial aid guidance, career counseling, mental health resources, and administrative help desk functions designed to assist students from enrollment through graduation.'
    },
    {
      q: 'Why do students disengage before they formally withdraw?',
      a: 'Disengagement is rarely sudden. It begins with skipped classes, unanswered emails, or missed administrative deadlines when students feel overwhelmed by academic pressures or isolated from support systems.'
    },
    {
      q: 'Can institutions outsource student support without losing quality?',
      a: 'Yes. Partnering with a dedicated education BPO provider extends coverage across evenings and weekends, absorbs peak enrollment surges, and maintains consistent service standards while freeing institutional advisors for high-touch advising.'
    }
  ];

  for (const f of sxpFaqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Higher Education Support',
      title: 'Ready to Build a Consistent Student Support Operation?',
      text: 'Centro partners with higher education institutions to deliver scalable, multichannel student support across admissions, enrollment, financial aid, and ongoing advising.',
      buttonLabel: 'Contact Centro',
      buttonLink: JSON.stringify([
        {
          name: 'Contact Centro',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 4. RCM SOLUTIONS POST
function parseRcmSolutionsPost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Intro
  const introM = clean.match(/<section class="rcm-intro"[\s\S]*?<\/section>/i);
  if (introM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-rcm-story">\n${introM[0]}\n</div>` }
    });
  }

  // Challenges
  const chalM = clean.match(/<section class="rcm-challenges"[\s\S]*?<\/section>/i);
  if (chalM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-rcm-story">\n${chalM[0]}\n</div>` }
    });
  }

  // Outcomes
  const outM = clean.match(/<section class="rcm-outcomes"[\s\S]*?<\/section>/i);
  if (outM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-rcm-story">\n${outM[0]}\n</div>` }
    });
  }

  // Heading: Why Centros RCM Solutions Matter
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Why Centro’s RCM Solutions Matter',
      level: formatHeadingLevel('h2'),
      anchorId: 'why-centro-rcm'
    }
  });

  const faqs = [
    {
      q: 'Specialized Healthcare RCM Expertise',
      a: 'Dedicated billing specialists with in-depth knowledge of payer policies, specialty coding, and denial mitigation.'
    },
    {
      q: 'End-to-End Claims Management',
      a: 'From patient registration and eligibility to charge entry, claim submission, payment posting, and appeals.'
    },
    {
      q: 'Technology-Enabled Visibility',
      a: 'Transparent reporting, real-time dashboards, and workflow analytics integrated with leading EHR systems.'
    },
    {
      q: 'Scalable Delivery',
      a: 'Flexible offshore staffing models that adapt to volume swings, open-enrollment spikes, and organizational growth.'
    },
    {
      q: 'A Clearer Patient Billing Experience',
      a: 'Compassionate, multi-channel patient billing support that answers questions promptly and improves patient collection rates.'
    }
  ];

  for (const f of faqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Healthcare Operations',
      title: 'Protect Revenue and Reduce Claim Delays with Centro',
      text: 'Explore Centro’s Healthcare Revenue Cycle Management solutions to improve clean-claim performance and streamline billing workflows.',
      buttonLabel: 'Explore Healthcare Solutions',
      buttonLink: JSON.stringify([
        {
          name: 'Explore Healthcare Solutions',
          url: 'https://centrocdx.com/solution/health',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 5. BUYERS GUIDE POST
function parseBuyersGuidePost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<header class="cx-hero"[\s\S]*?<\/header>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<article class="centro-cx-lab">\n${heroM[0]}\n</article>` }
    });
  }

  // Question sections
  const secRegex = /<section class="cx-section[^"]*"[\s\S]*?<\/section>/gi;
  let sm;
  while ((sm = secRegex.exec(clean)) !== null) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<article class="centro-cx-lab">\n${sm[0]}\n</article>` }
    });
  }

  // Closing CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Contact Center Assessment',
      title: 'Ready to Find the Right Outsourcing Partner?',
      text: 'Centro delivers high-performing customer experience solutions built around transparency, scalability, and measurable performance.',
      buttonLabel: 'Talk to Our CX Team',
      buttonLink: JSON.stringify([
        {
          name: 'Talk to Our CX Team',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 6. CCW REPORT
function parseCcwReportPost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<section class="ccw-hero"[\s\S]*?<\/section>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-ccw-report">\n${heroM[0]}\n</div>` }
    });
  }

  // Pulse
  const pulseM = clean.match(/<section class="ccw-pulse"[\s\S]*?<\/section>/i);
  if (pulseM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-ccw-report">\n${pulseM[0]}\n</div>` }
    });
  }

  // Signals
  const signalsM = clean.match(/<section class="ccw-signals"[\s\S]*?<\/section>/i);
  if (signalsM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="centro-ccw-report">\n${signalsM[0]}\n</div>` }
    });
  }

  // Questions
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Questions to Ask Your CX Operations',
      level: formatHeadingLevel('h2'),
      anchorId: 'ccw-questions'
    }
  });

  const ccwFaqs = [
    {
      q: 'Where does customer context break today?',
      a: 'Identify where customers are forced to repeat information across channels or between AI self-service and human support agents.'
    },
    {
      q: 'Which decisions should AI support, and which should remain human?',
      a: 'Automate high-frequency, rule-based inquiries while directing complex, emotional, or high-value customer interactions to skilled representatives.'
    },
    {
      q: 'Do frontline teams have enough visibility to resolve the issue?',
      a: 'Empower agents with unified CRM data, real-time knowledge bases, and clear authority to resolve inquiries on the first contact.'
    },
    {
      q: 'Are CX metrics connected to business outcomes?',
      a: 'Connect CSAT, FCR, and handle times directly to retention rates, customer lifetime value, and reduced cost-to-serve.'
    },
    {
      q: 'Can the operating model scale without losing quality?',
      a: 'Ensure robust training, continuous quality assurance, and flexible workforce management that maintain performance during volume spikes.'
    }
  ];

  for (const f of ccwFaqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Next-Gen CX Operations',
      title: 'Build Operations That Deliver on the Promise of CX',
      text: 'Explore how Centro combines intelligent automation with skilled human teams to scale customer operations without losing quality.',
      buttonLabel: 'Connect With Centro',
      buttonLink: JSON.stringify([
        {
          name: 'Connect With Centro',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 7. 2026 CONTACT CENTER TRENDS
function parseTrends2026Post(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<section class="ct-hero"[\s\S]*?<\/section>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="ct-wrap">\n${heroM[0]}\n</div>` }
    });
  }

  // Intro
  const introM = clean.match(/<section class="ct-intro"[\s\S]*?<\/section>/i);
  if (introM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="ct-wrap">\n${introM[0]}\n</div>` }
    });
  }

  // Trends
  const trendRegex = /<section class="ct-trend"[\s\S]*?<\/section>/gi;
  let tm;
  let count = 1;
  while ((tm = trendRegex.exec(clean)) !== null) {
    const raw = tm[0];
    const num = String(count).padStart(2, '0');
    const h2M = raw.match(/<h2>([\s\S]*?)<\/h2>/i);
    const title = h2M ? h2M[1].replace(/<[^>]+>/g, '').trim() : `Trend ${num}`;
    const bodyM = raw.replace(/<div class="ct-trend-head"[\s\S]*?<\/div>/i, '').replace(/<\/?section[^>]*>/gi, '').trim();

    blocks.push({
      type: 'blogSectionBlock',
      values: {
        number: num,
        title,
        anchorId: `trend-${count}`,
        body: bodyM
      }
    });
    count++;
  }

  // Conclusion
  const concM = clean.match(/<section class="ct-conclusion"[\s\S]*?<\/section>/i);
  if (concM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="ct-wrap">\n${concM[0]}\n</div>` }
    });
  }

  // FAQ
  blocks.push({
    type: 'blogHeadingBlock',
    values: {
      heading: 'Frequently Asked Questions',
      level: formatHeadingLevel('h2'),
      anchorId: 'faq'
    }
  });

  const faqs = [
    {
      q: 'How is AI changing contact center operations in 2026?',
      a: 'AI is shifting from standalone chatbots to agent-assist copilots that summarize conversations, suggest next-best actions, and automate post-call documentation in real time.'
    },
    {
      q: 'What role do human agents play in modern contact centers?',
      a: 'Human agents handle complex, empathetic, and high-stakes customer interactions that require critical thinking, emotional intelligence, and nuanced problem resolution.'
    },
    {
      q: 'How can organizations balance automation with personalized customer service?',
      a: 'By offering seamless handoffs from self-service to human agents, retaining conversational context, and letting customers choose their preferred communication channel.'
    }
  ];

  for (const f of faqs) {
    blocks.push({
      type: 'blogFaqItemBlock',
      values: { question: f.q, answer: f.a }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: '2026 CX Strategy',
      title: 'Ready to Modernize Your Contact Center Operations?',
      text: 'Discover how Centro helps brands deploy next-generation contact center capabilities with skilled teams and proven technology.',
      buttonLabel: 'Discuss Your CX Goals',
      buttonLink: JSON.stringify([
        {
          name: 'Discuss Your CX Goals',
          url: 'https://centrocdx.com/get-in-touch/',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 8. BILINGUAL EXCELLENCE
function parseBilingualPost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<section class="cc-hero"[\s\S]*?<\/section>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cc-wrap">\n${heroM[0]}\n</div>` }
    });
  }

  // Results
  const resM = clean.match(/<section class="cc-results"[\s\S]*?<\/section>/i);
  if (resM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cc-wrap">\n${resM[0]}\n</div>` }
    });
  }

  // Sections
  const secRegex = /<section class="cc-section"[\s\S]*?<\/section>/gi;
  let sm;
  while ((sm = secRegex.exec(clean)) !== null) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cc-wrap">\n${sm[0]}\n</div>` }
    });
  }

  // Impact
  const impM = clean.match(/<section class="cc-impact"[\s\S]*?<\/section>/i);
  if (impM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cc-wrap">\n${impM[0]}\n</div>` }
    });
  }

  // Callout takeaway
  blocks.push({
    type: 'blogCalloutBlock',
    values: {
      label: 'Key Takeaway',
      text: 'Bilingual excellence is not just translation—it is cultural fluency, seamless system integration, and consistent brand representation across every touchpoint.'
    }
  });

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Insurance BPO Case Study',
      title: 'Scale Your Operations with High-Performing Bilingual Teams',
      text: 'Partner with Centro to deliver exceptional, high-touch support across English and Spanish customer bases.',
      buttonLabel: 'Explore Bilingual Solutions',
      buttonLink: JSON.stringify([
        {
          name: 'Explore Bilingual Solutions',
          url: 'https://centrocdx.com/solution/bpo',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// 9. JOURNEY OF CENTRO
function parseJourneyPost(html) {
  const blocks = [];
  const clean = html.replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '').replace(/<!--[\s\S]*?-->/g, '').trim();

  // Hero
  const heroM = clean.match(/<section class="cj-hero"[\s\S]*?<\/section>/i);
  if (heroM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cj-wrap">\n${heroM[0]}\n</div>` }
    });
  }

  // Intro
  const introM = clean.match(/<section class="cj-intro"[\s\S]*?<\/section>/i);
  if (introM) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cj-wrap">\n${introM[0]}\n</div>` }
    });
  }

  // Eras
  const eraRegex = /<section class="cj-era"[\s\S]*?<\/section>/gi;
  let em;
  while ((em = eraRegex.exec(clean)) !== null) {
    blocks.push({
      type: 'blogRichTextBlock',
      values: { text: `<div class="cj-wrap">\n${em[0]}\n</div>` }
    });
  }

  // CTA
  blocks.push({
    type: 'blogCtaBlock',
    values: {
      eyebrow: 'Our Journey',
      title: 'Be Part of the Centro Growth Story',
      text: 'Discover how Centro empowers businesses and people to achieve extraordinary results across the globe.',
      buttonLabel: 'Learn More About Us',
      buttonLink: JSON.stringify([
        {
          name: 'Learn More About Us',
          url: 'https://centrocdx.com/about',
          target: '_blank',
          queryString: null
        }
      ])
    }
  });

  return blocks;
}

// MAIN EXECUTION
const dir = 'uSync/v17/Content';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.config'));
let convertedCount = 0;

for (const file of files) {
  const filePath = path.join(dir, file);
  const content = fs.readFileSync(filePath, 'utf8');
  if (!content.includes('<ContentType>blogDetailPage</ContentType>')) continue;

  const m = content.match(/<contentBlocks>[\s\S]*?<!\[CDATA\[([\s\S]*?)\]\]>[\s\S]*?<\/contentBlocks>/);
  if (!m) continue;

  let blockListJson;
  try {
    blockListJson = JSON.parse(m[1]);
  } catch (e) {
    continue;
  }

  const rawHtml = blockListJson.contentData?.find(c => c.contentTypeKey === CONTENT_KEYS.blogHtmlBlock)
    ?.values?.find(v => v.alias === 'html')?.value || '';

  let blocksToUse = null;

  if (file === 'contact-center-staffing-calculator-how-many-support-agents-do-you-need.config') {
    blocksToUse = parseStaffingCalculatorPost();
  } else if (file === 'healthcare-revenue-cycle-management-trends-in-2026-denials-staffing-and-the-ai-roi-gap.config') {
    blocksToUse = parseHealthcareRcmPost();
  } else if (file === 'supporting-hope-centro-visits-children-s-cancer-hospital-egypt-57357-on-world-humanitarian-day.config') {
    blocksToUse = parseHospital57357Post(rawHtml);
  } else if (file === 'centro-named-iaop-global-outsourcing-100.config') {
    blocksToUse = parseIaopPost(rawHtml);
  } else if (file === 'how-to-design-a-consistent-student-experience.config') {
    blocksToUse = parseStudentExperiencePost(rawHtml);
  } else if (file === 'maximize-revenue-minimize-delays-why-centros-rcm-solutions-matter.config') {
    blocksToUse = parseRcmSolutionsPost(rawHtml);
  } else if (file === 'what-buyers-should-ask-before-outsourcing-cx.config') {
    blocksToUse = parseBuyersGuidePost(rawHtml);
  } else if (file === 'what-we-heard-at-ccw-las-vegas-2026-the-future-of-cx-is-built-on-better-operations.config') {
    blocksToUse = parseCcwReportPost(rawHtml);
  } else if (file === '2026-contact-center-trends-what-to-watch-this-year.config') {
    blocksToUse = parseTrends2026Post(rawHtml);
  } else if (file === 'bilingual-excellence-at-scale-how-we-became-a-top-insurance-brokerages-most-trusted-partner.config') {
    blocksToUse = parseBilingualPost(rawHtml);
  } else if (file === 'from-inception-to-a-global-leader-the-journey-of-centro-cdx.config') {
    blocksToUse = parseJourneyPost(rawHtml);
  } else if (!rawHtml.includes('<style')) {
    // Standard blog post
    blocksToUse = parseStandardPost(rawHtml);
  }

  if (blocksToUse && blocksToUse.length > 0) {
    convertedCount++;
    const newJson = buildBlockListJson(blocksToUse);
    const formattedJson = JSON.stringify(newJson, null, 2);

    const updatedContent = content.replace(
      /<contentBlocks>[\s\S]*?<\/contentBlocks>/,
      `<contentBlocks>\n      <Value Culture="en-US"><![CDATA[${formattedJson}]]></Value>\n    </contentBlocks>`
    );

    fs.writeFileSync(filePath, updatedContent, 'utf8');
    console.log(`[CONVERTED] ${file} -> ${blocksToUse.length} modular components`);
  } else {
    console.log(`[KEPT AS MIRROR] ${file} (complex custom layout)`);
  }
}

console.log(`\nSuccessfully processed ${convertedCount} blog posts into modular components!`);
