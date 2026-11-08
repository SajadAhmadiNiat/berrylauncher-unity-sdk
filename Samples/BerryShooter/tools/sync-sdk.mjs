import fs from 'node:fs'; import path from 'node:path'; import { fileURLToPath } from 'node:url';
const here = path.dirname(fileURLToPath(import.meta.url));
const src = path.join(path.resolve(process.argv[2] || ''), 'sdk', 'unity', 'Packages', 'com.berry.sdk');
const dst = path.join(here, '..', 'Packages', 'com.berry.sdk');
if (!process.argv[2] || !fs.existsSync(src)) { console.error('Usage: node tools/sync-sdk.mjs <path-to-berry-repo>\nNot found: ' + src); process.exit(1); }
fs.rmSync(dst, { recursive: true, force: true });
fs.cpSync(src, dst, { recursive: true });
console.log('SDK copied:', src, '->', dst);
