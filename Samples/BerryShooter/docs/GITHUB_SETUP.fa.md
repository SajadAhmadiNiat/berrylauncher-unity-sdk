# راه‌اندازی صفحهٔ GitHub پروژه (قدم‌به‌قدم)

## ۱) ساخت مخزن
1. در github.com وارد شوید > دکمهٔ **New repository**.
2. Name: `berry-shooter` — Public — **تیک README و .gitignore و License را نزنید** (خودمان داریم).
3. Create repository.

## ۲) بارگذاری پروژه (ویندوز، PowerShell)
اگر Git ندارید: git-scm.com را نصب کنید. بعد:
```powershell
cd D:\Projects\BerryShooter          # پوشه‌ای که zip را در آن باز کردید (README.md همین‌جاست)
git init
git add .
git commit -m "First release"
git branch -M main
git remote add origin https://github.com/<USERNAME>/berry-shooter.git
git push -u origin main
```
(به‌جای `<USERNAME>` نام کاربری خودتان.) اگر رمز خواست، از Personal Access Token استفاده کنید (GitHub > Settings > Developer settings > Tokens).

## ۳) صفحهٔ اصلی چه می‌شود؟
خودِ `README.md` صفحهٔ اصلی مخزن است (آیکون، ویژگی‌ها، راه نصب، کنترل‌ها). متن فارسی در `README.fa.md` است؛ در بالای README.md می‌توانید خط زیر را اضافه کنید:
```
[فارسی](README.fa.md)
```

## ۴) تنظیمات مخزن
* **About** (چرخ‌دندهٔ سمت راست): Description: `Co-op shooter for Berry SDK (Unity)`، Topics: `unity`, `berry`, `multiplayer`, `shooter`.
* **Releases** > Draft a new release > Tag `v1.1.0` > فایل بیلد (`BerryShooter` zip شدهٔ پوشهٔ `Builds/BerryShooter`) را پیوست کنید > Publish.
* **Issues** فعال بماند؛ قالب باگ آماده است (`.github/ISSUE_TEMPLATE`).
* اگر لازم دارید صفحهٔ وب هم داشته باشد: Settings > Pages > Source: `Deploy from a branch` > `main` / `/docs` (README خودکار صفحه نمی‌شود؛ یک `docs/index.md` بسازید).

## ۵) استفاده از متن‌ها
* `README.md` انگلیسی = ویترین مخزن. `README.fa.md` = توضیح فارسی.
* `docs/GUIDE.fa.md` = راهنمای کامل یونیتی. `docs/SDK-COVERAGE.md` = کدام امکان SDK کجا استفاده شده.
* هر بار نسخهٔ تازه دادید: `CHANGELOG.md` را بروز کنید، `git add . && git commit -m "..." && git push`.
* آپدیت SDK: `node tools/sync-sdk.mjs D:\Projects\Berry` (پوشهٔ اصلی بری) و سپس commit.

نکتهٔ صادقانه: کد این پروژه در محیط ساخت کامپایل نشده؛ پیش از انتشار عمومی یک بار در یونیتی باز کنید و بیلد بگیرید.
