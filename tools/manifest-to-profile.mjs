#!/usr/bin/env node
import { readFileSync, writeFileSync } from 'node:fs'

export const PERMISSIONS = ['identity', 'room', 'players', 'presence', 'achievements', 'scores', 'token', 'network', 'lan', 'friends', 'profile']
export const CONNECT = ['lan', 'vlan', 'wireguard', 'ip', 'direct', 'dedicated', 'directplay', 'steam']
const SAFE_ARG = /^[^\u0000-\u001f"]{1,200}$/
const SAFE_NAME = /^[A-Za-z0-9 _.()+-]{1,80}$/

export function manifestToProfile(m) {
  const problems = []
  const bad = (msg) => problems.push(msg)
  if (!m || typeof m !== 'object' || Array.isArray(m)) return { profile: null, problems: ['the manifest must be a JSON object'] }
  if (m.schemaVersion !== 1) bad('schemaVersion must be 1')
  if (typeof m.id !== 'string' || !/^[a-z0-9-]{3,40}$/.test(m.id)) bad('id: 3-40 characters, lowercase letters, digits and "-"')
  if (typeof m.displayName !== 'string' || m.displayName.trim().length < 2 || m.displayName.length > 60) bad('displayName: 2-60 characters')
  if (typeof m.executable !== 'string' || !SAFE_NAME.test(m.executable) || !/\.exe$/i.test(m.executable)) bad('executable: the program file name, e.g. "MyGame.exe" (no folders)')
  const maxPlayers = m.maxPlayers ?? 4
  if (!Number.isInteger(maxPlayers) || maxPlayers < 2 || maxPlayers > 64) bad('maxPlayers: 2-64')
  const gamePort = m.gamePort ?? 0
  if (!Number.isInteger(gamePort) || gamePort < 0 || gamePort > 65535) bad('gamePort: 0-65535')
  const protocol = m.protocol ?? 'udp'
  if (!['udp', 'tcp', 'both'].includes(protocol)) bad('protocol: udp, tcp or both')
  const connect = m.connect ?? ['lan', 'vlan']
  if (!Array.isArray(connect) || !connect.length || connect.some((c) => !CONNECT.includes(c))) bad('connect: a list of ' + CONNECT.join(', '))
  const args = (key, allowed) => {
    const v = m[key] ?? []
    if (!Array.isArray(v) || v.length > 12 || v.some((a) => typeof a !== 'string' || !SAFE_ARG.test(a))) { bad(`${key}: at most 12 plain text items`); return [] }
    for (const a of v) for (const tok of a.match(/\{[^}]*\}/g) ?? []) if (!allowed.includes(tok)) bad(`${key}: "${tok}" is not a known placeholder (${allowed.join(' ') || 'none'})`)
    return v
  }
  const hostArgs = args('hostArgs', [])
  const joinArgs = args('joinArgs', ['{host}', '{port}'])
  const sdk = m.sdk ?? null
  let permissions = []
  if (sdk !== null) {
    if (typeof sdk !== 'object' || Array.isArray(sdk)) bad('sdk must be an object')
    else {
      permissions = sdk.permissions ?? []
      if (!Array.isArray(permissions) || permissions.some((p) => !PERMISSIONS.includes(p))) bad('sdk.permissions: only ' + PERMISSIONS.join(', '))
    }
  }
  if (m.version !== undefined && (typeof m.version !== 'string' || m.version.length > 20)) bad('version: short text')
  if (problems.length) return { profile: null, problems }
  const profile = {
    schemaVersion: 1, id: m.id, displayName: m.displayName.trim(),
    executable: { names: [m.executable], installFolders: [m.displayName.replace(/[^A-Za-z0-9 _.()+-]/g, '').trim() || m.id], staleNames: [], defaultPaths: [], args: [], hostArgs, joinArgs },
    network: { protocol, discovery: 'direct', maxPlayers, ...(gamePort ? { gamePort } : {}), modes: [...new Set(connect.flatMap((c) => (c === 'lan' ? ['lan'] : c === 'direct' || c === 'ip' ? ['direct'] : ['private'])))], support: 'experimental', connect },
    ...(sdk !== null ? { sdk: { permissions } } : {}),
  }
  return { profile, problems: [] }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const [inFile, outFile] = process.argv.slice(2)
  if (!inFile) { console.error('usage: node manifest-to-profile.mjs berry-game.json [out.json]'); process.exit(2) }
  let m
  try { m = JSON.parse(readFileSync(inFile, 'utf8')) } catch (e) { console.error('cannot read the manifest: ' + e.message); process.exit(1) }
  const { profile, problems } = manifestToProfile(m)
  if (problems.length) { console.error('The manifest has problems:\n - ' + problems.join('\n - ')); process.exit(1) }
  const text = JSON.stringify(profile, null, 2) + '\n'
  if (outFile) { writeFileSync(outFile, text); console.log('written: ' + outFile) } else process.stdout.write(text)
}
