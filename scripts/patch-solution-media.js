const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', 'uSync', 'v17', 'Content');

const pages = {
  bpo: {
    banner: 'a7010002-2222-4222-8222-211111111701',
    intro: 'a7010002-2222-4222-8222-211111111702',
    features: [
      'a7010002-2222-4222-8222-211111111703',
      'a7010002-2222-4222-8222-211111111704',
      'a7010002-2222-4222-8222-211111111705',
      'a7010002-2222-4222-8222-211111111706',
    ],
  },
  health: {
    banner: 'a7010002-2222-4222-8222-211111111711',
    intro: 'a7010002-2222-4222-8222-211111111712',
    features: [
      'a7010002-2222-4222-8222-211111111713',
      'a7010002-2222-4222-8222-211111111714',
      'a7010002-2222-4222-8222-211111111715',
      'a7010002-2222-4222-8222-211111111716',
    ],
  },
  digital: {
    banner: 'a7010002-2222-4222-8222-211111111721',
    intro: 'a7010002-2222-4222-8222-211111111722',
    features: [
      'a7010002-2222-4222-8222-211111111723',
      'a7010002-2222-4222-8222-211111111724',
      'a7010002-2222-4222-8222-211111111725',
      'a7010002-2222-4222-8222-211111111726',
      'a7010002-2222-4222-8222-211111111727',
      'a7010002-2222-4222-8222-211111111728',
    ],
  },
};

const slugCodes = { bpo: '01', health: '02', digital: '03' };

function pickerKey(prefix, code, index = 1) {
  const idx = String(index).padStart(2, '0');
  return `${prefix}-${code}000000000${idx}`;
}

function mediaPick(mediaKey, pickKey) {
  return [
    {
      key: pickKey,
      mediaKey,
      mediaTypeAlias: 'Image',
      crops: [],
      focalPoint: null,
    },
  ];
}

function mediaPropXml(mediaKey, pickKey) {
  const json = JSON.stringify(mediaPick(mediaKey, pickKey), null, 2);
  return `      <Value Culture="en-US"><![CDATA[${json}]]></Value>`;
}

for (const [slug, cfg] of Object.entries(pages)) {
  const file = path.join(root, `${slug}.config`);
  let xml = fs.readFileSync(file, 'utf8');

  const featMatch = xml.match(
    /<features>\s*<Value Culture="en-US"><!\[CDATA\[([\s\S]*?)\]\]><\/Value>\s*<\/features>/
  );
  if (!featMatch) throw new Error(`no features in ${slug}`);

  const feat = JSON.parse(featMatch[1]);
  const code = slugCodes[slug];
  feat.contentData.forEach((item, idx) => {
    const mediaKey = cfg.features[idx];
    if (!mediaKey) return;
    const pickKey = pickerKey(
      `c7010002-${String(idx + 1).padStart(4, '0')}-4002-8002`,
      code,
      idx + 1
    );
    item.values = item.values.filter((v) => v.alias !== 'imagePath');
    const imageVal = item.values.find((v) => v.alias === 'image');
    if (imageVal) imageVal.value = mediaPick(mediaKey, pickKey);
  });

  xml = xml.replace(
    featMatch[0],
    `<features>\n      <Value Culture="en-US"><![CDATA[${JSON.stringify(feat, null, 2)}]]></Value>\n    </features>`
  );

  const bannerPick = pickerKey('b7010002-1111-4111-8111', code);
  const introPick = pickerKey('b7010002-2222-4222-8222', code);
  const bannerBlock = `    <bannerImage>\n${mediaPropXml(cfg.banner, bannerPick)}\n    </bannerImage>`;
  const introBlock = `    <introImage>\n${mediaPropXml(cfg.intro, introPick)}\n    </introImage>`;

  if (/<bannerImage>[\s\S]*?<\/bannerImage>/.test(xml)) {
    xml = xml.replace(/<bannerImage>[\s\S]*?<\/bannerImage>/, bannerBlock);
  } else {
    xml = xml.replace('<bannerTitle>', `${bannerBlock}\n    <bannerTitle>`);
  }

  if (/<introImage>[\s\S]*?<\/introImage>/.test(xml)) {
    xml = xml.replace(/<introImage>[\s\S]*?<\/introImage>/, introBlock);
  } else {
    xml = xml.replace('<introHeading>', `${introBlock}\n    <introHeading>`);
  }

  xml = xml.replace(/\s*<introImagePath>[\s\S]*?<\/introImagePath>\s*/g, '\n');

  fs.writeFileSync(file, xml, 'utf8');
  console.log('updated', slug);
}
