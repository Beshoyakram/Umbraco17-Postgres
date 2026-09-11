const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..");
const contentDir = path.join(root, "uSync", "v17", "Content");
const seoTypePath = path.join(
  root,
  "uSync",
  "v17",
  "ContentTypes",
  "seosettings.config"
);
const redirectsPath = path.join(root, "seo", "legacy-redirects.json");
const productionOrigin = "https://centrocdx.com";

const errors = [];
const warnings = [];
const keys = new Map();
const blogs = [];

function match(xml, expression) {
  return xml.match(expression)?.[1]?.trim() ?? "";
}

function propertyValue(xml, alias) {
  const expression = new RegExp(
    `<${alias}>[\\s\\S]*?<Value(?:\\s+[^>]*)?><!\\[CDATA\\[([\\s\\S]*?)\\]\\]><\\/Value>[\\s\\S]*?<\\/${alias}>`,
    "i"
  );
  return match(xml, expression);
}

function plainPropertyValue(xml, alias) {
  return (
    propertyValue(xml, alias) ||
    match(
      xml,
      new RegExp(
        `<${alias}>[\\s\\S]*?<Value(?:\\s+[^>]*)?>([\\s\\S]*?)<\\/Value>[\\s\\S]*?<\\/${alias}>`,
        "i"
      )
    )
  );
}

for (const fileName of fs.readdirSync(contentDir).filter((x) => x.endsWith(".config"))) {
  const filePath = path.join(contentDir, fileName);
  const xml = fs.readFileSync(filePath, "utf8");
  const key = match(xml, /<Content Key="([^"]+)"/i).toLowerCase();
  const contentType = match(xml, /<ContentType>([^<]+)<\/ContentType>/i);

  if (key) {
    if (keys.has(key)) {
      errors.push(`Duplicate content key ${key}: ${keys.get(key)} and ${fileName}`);
    } else {
      keys.set(key, fileName);
    }
  }

  if (contentType !== "blogDetailPage") continue;
  blogs.push(fileName);

  const title =
    propertyValue(xml, "postTitle") ||
    match(xml, /<NodeName Default="([^"]+)"/i) ||
    fileName;
  const excerpt = propertyValue(xml, "excerpt");
  const seoDescription = propertyValue(xml, "sEODescription");
  const publishDate = plainPropertyValue(xml, "publishDate");
  const featuredImage = propertyValue(xml, "featuredImage");
  const blockJson = propertyValue(xml, "contentBlocks");

  if (!excerpt && !seoDescription) {
    warnings.push(`${fileName}: missing excerpt/SEO description`);
  }
  if (!publishDate) errors.push(`${fileName}: missing publishDate`);
  if (!featuredImage) errors.push(`${fileName}: missing featuredImage`);
  if (!blockJson) {
    errors.push(`${fileName}: missing Advanced HTML content`);
    continue;
  }

  try {
    const blocks = JSON.parse(blockJson);
    const contentData = blocks.contentData ?? [];
    if (contentData.length !== 1) {
      errors.push(`${fileName}: expected one Advanced HTML block, found ${contentData.length}`);
      continue;
    }

    const htmlValue =
      contentData[0].values?.find((x) => x.alias === "html")?.value ?? "";
    if (!htmlValue.trim()) {
      errors.push(`${fileName}: Advanced HTML is empty`);
    }

    const canonicalMatches = [
      ...htmlValue.matchAll(
        /<link[^>]+rel=["']canonical["'][^>]+href=["']([^"']+)["']/gi
      )
    ];
    for (const canonical of canonicalMatches) {
      if (
        /^https?:\/\//i.test(canonical[1]) &&
        !canonical[1].startsWith(productionOrigin)
      ) {
        warnings.push(
          `${fileName}: imported HTML contains foreign canonical ${canonical[1]} (renderer strips it)`
        );
      }
    }

    const images = [...htmlValue.matchAll(/<img\b([^>]*)>/gi)];
    const missingAlt = images.filter(
      (image) => !/\balt\s*=\s*["'][^"']*["']/i.test(image[1])
    ).length;
    if (missingAlt > 0) {
      warnings.push(`${fileName}: ${missingAlt} embedded image(s) have no alt attribute`);
    }
  } catch (error) {
    errors.push(`${fileName}: invalid contentBlocks JSON (${error.message})`);
  }

  if (title.length > 75) {
    warnings.push(`${fileName}: title is ${title.length} characters`);
  }
}

const requiredSeoAliases = [
  "sEOTitle",
  "sEODescription",
  "ogImage",
  "canonicalOverride",
  "noIndex",
  "excludeFromSitemap"
];
const seoType = fs.readFileSync(seoTypePath, "utf8");
for (const alias of requiredSeoAliases) {
  if (!seoType.includes(`<Alias>${alias}</Alias>`)) {
    errors.push(`SEO Settings is missing ${alias}`);
  }
}

const redirectConfig = JSON.parse(fs.readFileSync(redirectsPath, "utf8"));
const redirects = redirectConfig.LegacyRedirects ?? {};
const normalizedSources = new Set(
  Object.keys(redirects).map((x) => x.toLowerCase().replace(/\/+$/, "") || "/")
);
for (const [source, target] of Object.entries(redirects)) {
  const normalizedTarget = target.toLowerCase().replace(/\/+$/, "") || "/";
  if (normalizedSources.has(normalizedTarget)) {
    errors.push(`Redirect chain detected: ${source} -> ${target}`);
  }
  if (!source.startsWith("/") || !target.startsWith("/")) {
    errors.push(`Redirect paths must be root-relative: ${source} -> ${target}`);
  }
}

console.log(`SEO audit: ${blogs.length} blog posts, ${keys.size} content documents`);
for (const warning of warnings) console.warn(`WARN: ${warning}`);
for (const error of errors) console.error(`ERROR: ${error}`);
console.log(`SEO audit complete: ${errors.length} error(s), ${warnings.length} warning(s)`);
process.exitCode = errors.length ? 1 : 0;
