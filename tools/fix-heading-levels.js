const fs = require('fs');
const path = require('path');

const dir = 'uSync/v17/Content';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.config'));
let updatedCount = 0;

for (const file of files) {
  const filePath = path.join(dir, file);
  const content = fs.readFileSync(filePath, 'utf8');
  if (!content.includes('<ContentType>blogDetailPage</ContentType>')) continue;

  const m = content.match(/<contentBlocks>[\s\S]*?<!\[CDATA\[([\s\S]*?)\]\]>[\s\S]*?<\/contentBlocks>/);
  if (!m) continue;

  let json;
  try {
    json = JSON.parse(m[1]);
  } catch (e) {
    continue;
  }

  let modified = false;
  if (json.contentData) {
    for (const item of json.contentData) {
      if (item.contentTypeKey === 'a7010006-aaaa-4bbb-8ccc-000000000004') { // blogHeadingBlock
        const levelProp = item.values?.find(v => v.alias === 'level');
        if (levelProp) {
          const current = levelProp.value;
          let norm = 'h2';
          if (typeof current === 'string') {
            if (current.includes('h3')) norm = 'h3';
          }
          const formatted = JSON.stringify([norm]);
          if (levelProp.value !== formatted) {
            levelProp.value = formatted;
            modified = true;
          }
        }
      }
    }
  }

  if (modified) {
    updatedCount++;
    const formattedJson = JSON.stringify(json, null, 2);
    const updatedContent = content.replace(
      /<contentBlocks>[\s\S]*?<\/contentBlocks>/,
      `<contentBlocks>\n      <Value Culture="en-US"><![CDATA[${formattedJson}]]></Value>\n    </contentBlocks>`
    );
    fs.writeFileSync(filePath, updatedContent, 'utf8');
    console.log(`Updated heading levels in: ${file}`);
  }
}

console.log(`Fixed heading levels in ${updatedCount} files.`);
