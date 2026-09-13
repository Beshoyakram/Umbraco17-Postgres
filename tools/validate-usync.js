const fs = require('fs');
const path = require('path');

const dir = 'uSync/v17/Content';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.config'));

let errors = 0;
let postCount = 0;

for (const f of files) {
  const content = fs.readFileSync(path.join(dir, f), 'utf8');
  if (!content.includes('<ContentType>blogDetailPage</ContentType>')) continue;
  postCount++;

  const m = content.match(/<contentBlocks>[\s\S]*?<!\[CDATA\[([\s\S]*?)\]\]>[\s\S]*?<\/contentBlocks>/);
  if (!m) {
    console.error(`ERROR: No contentBlocks found in ${f}`);
    errors++;
    continue;
  }

  try {
    const json = JSON.parse(m[1]);
    if (!json.contentData || !Array.isArray(json.contentData) || json.contentData.length === 0) {
      console.error(`ERROR: contentData is empty in ${f}`);
      errors++;
    }
  } catch (err) {
    console.error(`ERROR: Invalid JSON in ${f}:`, err.message);
    errors++;
  }
}

console.log(`Validation complete: Checked ${postCount} blog posts. Errors found: ${errors}`);
