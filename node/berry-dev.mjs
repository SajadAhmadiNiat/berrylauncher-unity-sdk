#!/usr/bin/env node
import { createHash } from 'node:crypto'
import { createReadStream } from 'node:fs'

export const DEFAULT_SITE = 'https://berrylauncher.ir'

export function parseArgs(argv) {
  const out = { _: [], flags: {} }
  const multi = new Set(['dep'])
  const bool = new Set(['mandatory', 'publish', 'help'])
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i]
    if (!a.startsWith('--')) { out._.push(a); continue }
    const k = a.slice(2)
    if (bool.has(k)) { out.flags[k] = true; continue }
    const v = argv[++i]
    if (v === undefined || v.startsWith('--')) throw new Error(`--${k} needs a value`)
    if (multi.has(k)) (out.flags[k] ??= []).push(v)
    else out.flags[k] = v
  }
  return out
}

export function parseDep(text) {
  const [url, sha256, kind = 'zip', path = '', name = ''] = String(text).split('|').map((x) => x.trim())
  if (!/^https:\/\//.test(url ?? '')) throw new Error(`dependency "${text}": the address must start with https://`)
  if (!/^[0-9a-fA-F]{64}$/.test(sha256 ?? '')) throw new Error(`dependency "${text}": the SHA-256 must be 64 hex characters`)
  if (!['zip', 'file'].includes(kind)) throw new Error(`dependency "${text}": the type is zip or file`)
  if (kind === 'file' && !name) throw new Error(`dependency "${text}": a file needs its name as the 5th part`)
  return { url, sha256: sha256.toLowerCase(), kind, path, ...(name ? { name } : {}) }
}

export function buildRelease(flags) {
  for (const k of ['version', 'url', 'sha256']) if (!flags[k]) throw new Error(`--${k} is required`)
  if (flags.vt && !/^[0-9a-fA-F]{32,128}$/.test(String(flags.vt))) throw new Error('--vt must be the VirusTotal hash (hex)')
  const body = { version: flags.version, url: flags.url, sha256: String(flags.sha256).toLowerCase(), kind: flags.kind || 'zip', mandatory: !!flags.mandatory, publish: !!flags.publish }
  if (flags.exe) body.exe = flags.exe
  if (flags.vt) body.vt_hash = String(flags.vt).toLowerCase()
  if (flags['vt-url']) body.vt_url = flags['vt-url']
  if (flags.folder) body.folder = flags.folder
  if (flags['size-mb']) body.size_mb = Number(flags['size-mb'])
  if (flags.notes) body.notes_fa = flags.notes
  if (flags['notes-en'] || flags.notes) body.notes_en = flags['notes-en'] || flags.notes
  if (flags.dep) body.deps = flags.dep.map(parseDep)
  return body
}

export function sha256File(path) {
  return new Promise((resolve, reject) => {
    const h = createHash('sha256')
    createReadStream(path).on('data', (d) => h.update(d)).on('end', () => resolve(h.digest('hex'))).on('error', reject)
  })
}

export async function api(site, token, method, path, body) {
  const r = await fetch(site.replace(/\/+$/, '') + '/api/dev' + path, { method, headers: { Authorization: 'Bearer ' + token, ...(body ? { 'Content-Type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined })
  const text = await r.text()
  let json = null
  try { json = JSON.parse(text) } catch {  }
  if (!r.ok) throw new Error(`${r.status} ${json?.error ?? text.slice(0, 120)}`)
  return json
}

export async function run(argv, env = process.env, log = console.log) {
  const { _: pos, flags } = parseArgs(argv)
  const cmd = pos[0]
  if (!cmd || flags.help) { log('berry-dev: me | games | sha256 <file> | release <gameId> ... [--vt <hash>] | sdk <gameId> | releases <gameId> | publish <gameId> <releaseId> | revoke <gameId> <releaseId> | stats | economy\n(see the comment at the top of this file)'); return 0 }
  if (cmd === 'sha256') { if (!pos[1]) throw new Error('give a file'); log(await sha256File(pos[1])); return 0 }
  const token = env.BERRY_DEV_TOKEN
  if (!token || !token.startsWith('bdt_')) throw new Error('set BERRY_DEV_TOKEN (your developer token from the website dashboard)')
  const site = env.BERRY_SITE || DEFAULT_SITE
  const id = (v) => { if (!/^[a-z0-9-]{3,40}$/.test(v ?? '')) throw new Error('a game id is needed (lowercase letters, digits, -)'); return v }
  const num = (v) => { if (!/^\d{1,9}$/.test(v ?? '')) throw new Error('a release number is needed'); return v }
  let r
  if (cmd === 'me') r = await api(site, token, 'GET', '/me')
  else if (cmd === 'games') r = await api(site, token, 'GET', '/games')
  else if (cmd === 'sdk') r = await api(site, token, 'GET', `/games/${id(pos[1])}/sdk`)
  else if (cmd === 'releases') r = await api(site, token, 'GET', `/games/${id(pos[1])}/releases`)
  else if (cmd === 'release') r = await api(site, token, 'POST', `/games/${id(pos[1])}/releases`, buildRelease(flags))
  else if (cmd === 'publish' || cmd === 'revoke') r = await api(site, token, 'POST', `/games/${id(pos[1])}/releases/${num(pos[2])}/${cmd}`, {})
  else if (cmd === 'stats') r = await api(site, token, 'GET', `/stats?game=${encodeURIComponent(flags.game ?? '')}&days=${Number(flags.days) || 30}&page=${Number(flags.page) || 1}`)
  else if (cmd === 'economy') r = await api(site, token, 'GET', `/economy?page=${Number(flags.page) || 1}`)
  else throw new Error('unknown command: ' + cmd)
  log(JSON.stringify(r, null, 2))
  return 0
}

if (import.meta.url === `file://${process.argv[1]}`) {
  run(process.argv.slice(2)).then((c) => process.exit(c), (e) => { console.error('berry-dev: ' + e.message); process.exit(1) })
}
