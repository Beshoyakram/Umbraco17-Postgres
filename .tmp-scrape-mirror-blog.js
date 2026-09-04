const fs = require("fs");
const https = require("https");
const path = require("path");
const { URL } = require("url");
const vm = require("vm");
const zlib = require("zlib");

const outDir = ".tmp-prod-blog";
fs.mkdirSync(path.join(outDir, "images"), { recursive: true });

function decodeBody(res, raw) {
  const enc = (res.headers["content-encoding"] || "").toLowerCase();
  if (enc.includes("gzip")) return zlib.gunzipSync(raw);
  if (enc.includes("br")) return zlib.brotliDecompressSync(raw);
  if (enc.includes("deflate")) return zlib.inflateSync(raw);
  return raw;
}

function request(url, headers = {}) {
  return new Promise((resolve, reject) => {
    const u = new URL(url);
    const req = https.request(
      {
        hostname: u.hostname,
        path: u.pathname + u.search,
        method: "GET",
        headers: {
          "User-Agent":
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
          Accept: "text/html,application/xhtml+xml",
          "Accept-Encoding": "gzip, deflate, br",
          ...headers,
        },
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          const raw = Buffer.concat(chunks);
          let bodyBuf;
          try {
            bodyBuf = decodeBody(res, raw);
          } catch {
            bodyBuf = raw;
          }
          resolve({
            status: res.statusCode,
            headers: res.headers,
            body: bodyBuf.toString("utf8"),
            buf: bodyBuf,
          });
        });
      }
    );
    req.on("error", reject);
    req.end();
  });
}

function solveSucuri(html) {
  const m = html.match(/S='([^']+)'/);
  if (!m) return null;
  const sandbox = {
    String,
    document: { cookie: "" },
    location: { reload() {} },
    window: {},
  };
  sandbox.window = sandbox;
  const code = `
    var s={},u,c,U,r,i,l=0,a,e=eval,w=String.fromCharCode,sucuri_cloudproxy_js='',S='${m[1]}';
    L=S.length;U=0;r='';
    var A='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
    for(u=0;u<64;u++){s[A.charAt(u)]=u;}
    for(i=0;i<L;i++){c=s[S.charAt(i)];U=(U<<6)+c;l+=6;while(l>=8){((a=(U>>>(l-=8))&0xff)||(i<(L-2)))&&(r+=w(a));}}
    e(r);
  `;
  try {
    vm.runInNewContext(code, sandbox, { timeout: 2000 });
  } catch (_) {}
  const cookie = sandbox.document.cookie || "";
  const pair = cookie.split(";")[0];
  return pair || null;
}

async function fetchPage(url, cookie) {
  const headers = cookie ? { Cookie: cookie } : {};
  let res = await request(url, headers);
  if (res.body.includes("sucuri_cloudproxy_js")) {
    const c = solveSucuri(res.body);
    if (!c) throw new Error("Sucuri solve failed for " + url);
    res = await request(url, { Cookie: c });
    return { res, cookie: c };
  }
  return { res, cookie: cookie || null };
}

function decode(s) {
  return String(s || "")
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&nbsp;/g, " ")
    .trim();
}

function extractListingPosts(html) {
  const posts = [];
  const seen = new Set();
  const blocks = html.split(/<article class="blog_item">/i).slice(1);
  for (const block of blocks) {
    const slug = (block.match(/href="https:\/\/centrocdx\.com\/([^"\/]+)\//i) || [])[1];
    if (!slug || seen.has(slug)) continue;
    seen.add(slug);
    const href = `https://centrocdx.com/${slug}/`;
    const title = decode((block.match(/<h2>([\s\S]*?)<\/h2>/i) || [])[1] || "").replace(/<[^>]+>/g, "");
    const excerpt = decode(
      (block.match(/<div class="blog_details">[\s\S]*?<p>([\s\S]*?)<\/p>/i) || [])[1] || ""
    ).replace(/<[^>]+>/g, "");
    const category = decode((block.match(/fa-user"><\/i>\s*([^<]+)/i) || [])[1] || "");
    const day = decode((block.match(/blog_item_date[\s\S]*?<h3>([^<]+)<\/h3>/i) || [])[1] || "");
    const month = decode((block.match(/blog_item_date[\s\S]*?<p>([^<]+)<\/p>/i) || [])[1] || "");
    const img = (block.match(/<img[^>]+src="([^"]+)"/i) || [])[1] || "";
    posts.push({ slug, href, title, excerpt, category, day, month, img });
  }
  return posts;
}

function extractBody(html) {
  const detailsIdx = html.search(/<div class="blog_details">/i);
  if (detailsIdx < 0) return "";
  let chunk = html.slice(detailsIdx);
  const sideIdx = chunk.search(/<div class="col-lg-4">/i);
  if (sideIdx > 0) chunk = chunk.slice(0, sideIdx);

  // Production pattern: excerpt then custom/prose content
  const excert = chunk.match(/<p class="excert">[\s\S]*?<\/p>/i);
  let after = chunk;
  if (excert) {
    after = chunk.slice(chunk.indexOf(excert[0]) + excert[0].length);
  } else {
    const ul = chunk.match(/<ul class="blog-info-link[\s\S]*?<\/ul>/i);
    if (ul) after = chunk.slice(chunk.indexOf(ul[0]) + ul[0].length);
  }

  after = after.trim();
  // Prefer full custom document if present
  const docStart = after.search(/<!doctype html|<html[\s>]/i);
  if (docStart >= 0) {
    let body = after.slice(docStart);
    const end = body.lastIndexOf("</html>");
    if (end >= 0) body = body.slice(0, end + 7);
    return body.trim();
  }

  // Prose: keep remaining content, strip trailing wrappers
  return after
    .replace(/<\/div>\s*<\/div>\s*$/i, "")
    .replace(/<\/div>\s*$/i, "")
    .trim();
}

function extractDetail(html, slug) {
  const featureImg =
    (html.match(/feature-img[\s\S]*?<img[^>]+src="([^"]+)"/i) || [])[1] || "";
  const title = decode(
    (html.match(/<div class="blog_details">[\s\S]*?<h2>([\s\S]*?)<\/h2>/i) || [])[1] || ""
  ).replace(/<[^>]+>/g, "");
  const category = decode((html.match(/fa-user"><\/i>\s*([^<]+)/i) || [])[1] || "");
  const views = parseInt(
    ((html.match(/post-views[\s\S]*?([\d,]+)\s*Views/i) || [])[1] || "0").replace(/,/g, ""),
    10
  );
  let excerpt = "";
  const excerptMatch = html.match(/<p class="excert">([\s\S]*?)<\/p>/i);
  if (excerptMatch) {
    excerpt = decode(excerptMatch[1].replace(/<[^>]+>/g, " ")).replace(/\s+/g, " ").trim();
  }
  const published =
    (html.match(/article:published_time" content="([^"]+)"/i) ||
      html.match(/datePublished":"([^"]+)"/i) ||
      [])[1] || "";
  const body = extractBody(html);
  return {
    slug,
    title,
    category,
    views,
    excerpt,
    featureImg,
    body,
    bodyLen: body.length,
    publishDate: published ? published.slice(0, 10) : null,
  };
}

(async () => {
  let cookie = null;
  const all = [];
  const seen = new Set();

  for (let page = 1; page <= 12; page++) {
    const url =
      page === 1
        ? "https://centrocdx.com/blog/"
        : `https://centrocdx.com/blog/page/${page}/`;
    const { res, cookie: c } = await fetchPage(url, cookie);
    cookie = c || cookie;
    console.log("listing", page, res.status, res.body.length, "search", /search_widget/.test(res.body));
    if (res.status !== 200 || res.body.length < 5000) break;
    if (page === 1) fs.writeFileSync(path.join(outDir, "listing.html"), res.body);
    const posts = extractListingPosts(res.body);
    let added = 0;
    for (const p of posts) {
      if (seen.has(p.slug)) continue;
      seen.add(p.slug);
      all.push(p);
      added++;
    }
    if (added === 0) break;
  }
  console.log("listing posts", all.length);

  const details = [];
  for (const p of all) {
    const { res, cookie: c } = await fetchPage(p.href, cookie);
    cookie = c || cookie;
    fs.writeFileSync(path.join(outDir, `${p.slug}.html`), res.body);
    const d = extractDetail(res.body, p.slug);
    const merged = {
      ...p,
      ...d,
      title: d.title || p.title,
      category: d.category || p.category,
      excerpt: d.excerpt || p.excerpt,
      featureImg: d.featureImg || p.img,
      img: d.featureImg || p.img,
    };
    const ext = ((merged.featureImg || "").match(/\.(png|jpe?g|webp|gif)/i) || [])[1] || "png";
    merged.imageFile = `${merged.slug}.${ext.toLowerCase().replace("jpeg", "jpg")}`;
    if (merged.featureImg) {
      try {
        const imgRes = await request(merged.featureImg, cookie ? { Cookie: cookie } : {});
        if (imgRes.status === 200 && imgRes.buf.length > 100) {
          fs.writeFileSync(path.join(outDir, "images", merged.imageFile), imgRes.buf);
          fs.mkdirSync(path.join("wwwroot", "media", "blog"), { recursive: true });
          fs.copyFileSync(
            path.join(outDir, "images", merged.imageFile),
            path.join("wwwroot", "media", "blog", merged.imageFile)
          );
          console.log("img", merged.imageFile, imgRes.buf.length);
        }
      } catch (e) {
        console.log("img fail", merged.slug, e.message);
      }
    }
    details.push(merged);
    console.log("ok", merged.slug, "views", merged.views, "body", merged.bodyLen);
  }

  fs.writeFileSync(path.join(outDir, "posts.json"), JSON.stringify(details, null, 2));
  console.log("saved", details.length);
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
