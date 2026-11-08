import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
const root = join(dirname(fileURLToPath(import.meta.url)), '..')
let failed = 0
const ok = (c, n) => { console.log(`${c ? '  ok  ' : '  FAIL'} ${n}`); if (!c) failed++ }
const walk = (d) => readdirSync(d).flatMap((f) => (statSync(join(d, f)).isDirectory() ? walk(join(d, f)) : [join(d, f)]))
const files = walk(join(root, 'Assets')).filter((f) => f.endsWith('.cs'))
const sdk = walk(join(root, 'Packages', 'com.berry.sdk', 'Runtime')).filter((f) => f.endsWith('.cs'))
const strip = (s) => s.replace(/@"(?:[^"]|"")*"/g, '""').replace(/"(?:\\.|[^"\\\n])*"/g, '""')
function balance(src) {
  const st = []; const pairs = { ')': '(', '}': '{', ']': '[' }
  for (let i = 0; i < src.length; i++) {
    const c = src[i]
    if (c === "'") { i++; while (i < src.length && src[i] !== "'") { if (src[i] === '\\') i++; i++ } continue }
    if ('({['.includes(c)) st.push(c); else if (')}]'.includes(c) && st.pop() !== pairs[c]) return 'unbalanced ' + c
  }
  return st.length ? 'unclosed ' + st.at(-1) : ''
}
const text = new Map(files.map((f) => [f, strip(readFileSync(f, 'utf8'))]))
ok(files.length >= 16, `found ${files.length} C# files`)
for (const [f, s] of text) ok(balance(s) === '', `${f.replace(root, '')}: brackets balance ${balance(s)}`)
const all = [...text.values()].join('\n'), sdkAll = sdk.map((f) => strip(readFileSync(f, 'utf8'))).join('\n')
function members(cls, src) {
  const m = new RegExp(`(?:class|enum|struct)\\s+${cls}\\b`).exec(src)
  if (!m) return null
  let i = src.indexOf('{', m.index), depth = 0, j = i
  for (; j < src.length; j++) { if (src[j] === '{') depth++; else if (src[j] === '}' && --depth === 0) break }
  const body = src.slice(i, j + 1)
  return new Set([...body.matchAll(/\b([A-Za-z_]\w*)\s*(?=[({=;,<]|\s*[{}])/g)].map((x) => x[1]))
}
const statics = { Ui: all, Sfx: all, Proto: all, Mats: all, Fonts: all, TextUtil: all, Settings: all, ArenaLayout: all, BerryUtil: sdkAll, Berry: sdkAll, World: all, EnemyState: all, GameSession: all, Role: all, GameState: all, Page: all, Tr: all, BerryHub: all, Persian: all, ArenaFactory: all, Bullet: all }
for (const [cls, src] of Object.entries(statics)) {
  const mem = members(cls, src); if (!mem) { ok(false, `class ${cls} not found`); continue }
  const used = new Set([...all.matchAll(new RegExp(`\\b${cls}\\.(\\w+)`, 'g'))].map((x) => x[1]))
  const missing = [...used].filter((u) => !mem.has(u) && !['Quit', 'Instance', 'ToString'].includes(u))
  ok(missing.length === 0, `${cls}.* members exist ${missing.join(', ')}`)
}
for (const [v, cls] of [['hub', 'BerryHub'], ['session', 'GameSession']]) {
  const mem = members(cls, all)
  const used = new Set([...all.matchAll(new RegExp(`\\b${v}\\.(\\w+)`, 'g'))].map((x) => x[1]))
  const missing = [...used].filter((u) => !mem.has(u))
  ok(missing.length === 0, `${v}.* members exist in ${cls} ${missing.join(', ')}`)
}
ok(!/namespace Berry\b/.test(all), 'no namespace named "Berry"')
const raw = files.map((f) => [f, readFileSync(f, 'utf8')])
for (const [f, src] of raw) {
  const noStrings = src.replace(/@"(?:[^"]|"")*"/g, '""').replace(/"(?:\\.|[^"\\\n])*"/g, '""').replace(/'(?:\\.|[^'\\])'/g, "''")
  ok(!/\/\/|\/\*/.test(noStrings), `${f.replace(root, '')}: no comments`)
}
for (const [f, src] of raw) { const a = (src.match(/^\s*#if/gm) || []).length, b = (src.match(/^\s*#endif/gm) || []).length; ok(a === b, `${f.replace(root, '')}: #if/#endif balanced`) }
const faText = readFileSync(join(root, 'Assets/BerryShooter/Scripts/TrFa.cs'), 'utf8')
const faKeys = [...faText.matchAll(/\{ "((?:[^"\\]|\\.)*)", "/g)].map((m) => m[1])
ok(new Set(faKeys).size === faKeys.length, `Persian dictionary has no duplicate keys (${faKeys.length})`)
const faSet = new Set(faKeys)
const used = new Set()
for (const [, src] of raw) {
  for (const m of src.matchAll(/Tr\.[TF]\(\s*"((?:[^"\\]|\\.)*)"/g)) used.add(m[1])
  for (const m of src.matchAll(/Line\("((?:[^"\\]|\\.)*)"/g)) used.add(m[1])
  for (const m of src.matchAll(/Tr\.[TF]\([^;]*?\?\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"/g)) { used.add(m[1]); used.add(m[2]) }
}
const lacking = [...used].filter((k) => !faSet.has(k) && !/^[^A-Za-z]*$/.test(k) && !k.startsWith('<'))
ok(lacking.length === 0, `every UI text has a Persian translation ${lacking.join(' | ')}`)
const fmtBad = faKeys.filter((k) => { const a = new Set([...k.matchAll(/\{(\d)\}/g)].map((m) => m[1])); const v = faText.match(new RegExp('\\{ "' + k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '", "((?:[^"\\\\]|\\\\.)*)" \\}')); if (!v) return false; const b = new Set([...v[1].matchAll(/\{(\d)\}/g)].map((m) => m[1])); return a.size !== b.size || [...a].some((x) => !b.has(x)) })
ok(fmtBad.length === 0, `Persian translations keep the {0} placeholders ${fmtBad.join(' | ')}`)
ok(!/Tester/.test(all), 'no hard-coded "Tester" name')
ok(!/FindObjectOfType\b/.test(all.replace(/#if[\s\S]*?#else([\s\S]*?)#endif/g, '')), 'no obsolete FindObjectOfType outside #if guards')
ok(raw.some(([, t]) => /Resources\.Load<Shader>\("BSFlat"\)/.test(t)), 'custom shader is loaded from Resources')
ok(!/\bScreen\b\s*[.=]/.test(all.replace(/UnityEngine\.Screen/g, '')), 'no use of Screen (clashes with UnityEngine.Screen)')
console.log(failed ? `shooter lint: ${failed} FAILED` : 'shooter lint: all ok'); process.exit(failed ? 1 : 0)
