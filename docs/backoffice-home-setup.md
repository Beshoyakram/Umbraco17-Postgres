# Backoffice setup (Home CMS + Global Settings + Media)

## 1) Import document types
Umbraco backoffice → **Settings → uSync → Import** (or Import All).

This creates:
- **Global Site Setting Holder** / **Site Main Information** / **Footer**
- **Home-Page** fields (Hero, About, Stats, AI, Partners, Solutions, Testimonials, Contact, SEO)
- Element types for Stats / Solutions / Testimonials blocks

## 2) Media folders (auto-seeded on app start)
On first run after deploy, media is created under:

```
Media
└── Centro
    ├── Brand          (Logo, Footer Logo, Favicon)
    ├── Home           (About, Hero video/fallback, AI/Testimonials/Footer backgrounds, Contact)
    ├── Partners       (Partner 1…10)
    └── Solutions      (BPO, Healthcare, Digital Transformation)
```

Source files are copied from `wwwroot/assets/centro` **once**. After that, manage files only in **Media**.

## 3) Create Global Site Settings content
Content tree (root):

1. Create **Global Site Setting Holder** → name it `Global Site Settings`
2. Under it create **Site Main Information** → set Site Title, Site Logo, Fav Icon, Header CTA
3. Under it create **Footer** → set Footer Logo, CTA texts, Phone, Email, Copyright

Logo is **not** on the Home page — only in Site Main Information (reusable everywhere).

## 4) Fill Home page
Open **Home** content and fill tabs:
- Hero / About / Stats / AI / Partners / Solutions / Testimonials / Contact / SEO Settings
- Pick media from `Media / Centro / …`

Until fields are filled, the front-end still shows sensible fallbacks.
