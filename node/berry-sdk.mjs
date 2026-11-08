import { readFileSync } from 'node:fs'
import { join } from 'node:path'

export const SDK_VERSION = '1.4.0'

export function discover(gameIdHint = '', env = process.env, read = (p) => readFileSync(p, 'utf8')) {
  const port = Number(env.BERRY_SDK_PORT)
  if (env.BERRY_SDK_TOKEN && Number.isInteger(port) && port > 0 && port < 65536) return { port, token: env.BERRY_SDK_TOKEN, gameId: env.BERRY_SDK_GAME || gameIdHint }
  if (!/^[A-Za-z0-9_.-]{1,64}$/.test(gameIdHint) || gameIdHint.includes('..')) return null
  const dir = env.BERRY_CONFIG_DIR || (env.APPDATA ? join(env.APPDATA, 'Berry') : env.XDG_CONFIG_HOME ? join(env.XDG_CONFIG_HOME, 'berry') : join(env.HOME || '.', '.config', 'berry'))
  try {
    const j = JSON.parse(read(join(dir, 'sdk', gameIdHint + '.json')))
    return Number.isInteger(j.port) && typeof j.token === 'string' && j.token ? { port: j.port, token: j.token, gameId: gameIdHint } : null
  } catch { return null }
}

export class BerryError extends Error { constructor(code) { super(code); this.code = code } }

export class Berry extends EventTarget {
  #ws; #n = 0; #pending = new Map()
  context = null
  permissions = []
  async connect(session = discover(), opts = {}) {
    if (!session) throw new BerryError('not_started_by_berry')
    this.#ws = new WebSocket(`ws://127.0.0.1:${session.port}/`)
    this.#ws.onmessage = (e) => this.#onMessage(JSON.parse(e.data))
    this.#ws.onclose = () => { for (const p of this.#pending.values()) p.reject(new BerryError('connection_lost')); this.#pending.clear(); this.dispatchEvent(new CustomEvent('close')) }
    await new Promise((res, rej) => { this.#ws.onopen = res; this.#ws.onerror = () => rej(new BerryError('connect_failed')) })
    const hello = { token: session.token, sdk: SDK_VERSION, game: session.gameId }
    if (opts.gameCode) { hello.gameCode = String(opts.gameCode); hello.gameNumber = Number(opts.gameNumber) || 0 }
    const r = await this.#request('hello', hello)
    this.context = r.context; this.permissions = r.permissions
    return r
  }
  #onMessage(m) {
    const p = m.id != null ? this.#pending.get(m.id) : null
    if (p) { this.#pending.delete(m.id); return p.resolve(m) }
    if (m.type === 'ctx') { this.context = m.payload; this.dispatchEvent(new CustomEvent('context', { detail: m.payload })) }
    else if (m.type === 'push') this.dispatchEvent(new CustomEvent('push', { detail: m.payload }))
    else if (m.type === 'bye') this.dispatchEvent(new CustomEvent('bye', { detail: m.payload }))
  }
  async #request(type, payload, ms = 15000) {
    const id = ++this.#n
    const reply = new Promise((resolve, reject) => { this.#pending.set(id, { resolve, reject }); setTimeout(() => { if (this.#pending.delete(id)) reject(new BerryError('timeout')) }, ms) })
    this.#ws.send(JSON.stringify({ v: 1, type, id, payload }))
    const m = await reply
    if (m.type === 'error') throw new BerryError(m.payload.code)
    return m.payload
  }
  #emit(name, data) { return this.#request('event', { name, data }) }
  setPresence(text) { return this.#emit('presence.set', { text }) }
  unlockAchievement(id, title = '') { return this.#emit('achievement.unlock', { id, title }) }
  submitScore(board, value) { return this.#emit('score.submit', { board, value }) }
  leaveRoom() { return this.#emit('room.leave', {}) }
  custom(name, data = {}) { return this.#emit(name, data) }
  async getIdentityToken(audience) {
    const m = await this.#requestRes('token', { audience })
    return m
  }
  async #requestRes(name, data) {
    const id = ++this.#n
    const reply = new Promise((resolve, reject) => { this.#pending.set(id, { resolve, reject }); setTimeout(() => { if (this.#pending.delete(id)) reject(new BerryError('timeout')) }, 15000) })
    this.#ws.send(JSON.stringify({ v: 1, type: 'req', id, payload: { name, data } }))
    const m = await reply
    if (m.type === 'error') throw new BerryError(m.payload.code)
    if (!m.payload.ok) throw new BerryError(m.payload.error || 'failed')
    return m.payload.data
  }
  request(name, data = {}) { return this.#requestRes(name, data) }
  lanStatus() { return this.#requestRes('lan.status', {}) }
  hostLan({ name, maxPlayers = 8, password, visibility = 'private' } = {}) {
    const d = { name, maxPlayers, visibility }
    if (password) d.password = password
    return this.#requestRes('lan.host', d)
  }
  joinLan(code, password) { return this.#requestRes('lan.join', password ? { code, password } : { code }) }
  leaveLan() { return this.#requestRes('lan.leave', {}) }
  getNetwork() { return this.#requestRes('network.get', {}) }
  listRooms(all = false) { return this.#requestRes('rooms.list', { all }).then((r) => r.rooms ?? []) }
  joinRoom(roomId, password) { return this.#requestRes('lan.join', { roomId, ...(password ? { password } : {}) }) }
  async getFriends() { return (await this.#requestRes('friends.list', {})).friends }
  inviteFriend(username) { return this.#requestRes('friends.invite', { username }) }
  getProfile(username) { return this.#requestRes('profile.get', username ? { username } : {}) }
  checkFollow(follower, target) { return this.#requestRes('follow.check', follower ? { a: follower, b: target } : { b: target }) }
  async doesFollow(follower, target) { return !!(await this.checkFollow(follower, target)).follows }
  async hasAchievement(id, username) { return !!(await this.#requestRes('achievements.has', username ? { id, username } : { id })).has }
  async listAchievements() { return (await this.#requestRes('achievements.list', {})).achievements ?? [] }
  getClub(handle) { return this.#requestRes('club.get', handle ? { handle } : {}) }
  async listTournaments() { return (await this.#requestRes('tournament.list', {})).tournaments ?? [] }
  async getTournament(id) { return (await this.#requestRes('tournament.get', { id })).tournament }
  getAccount() { return this.#requestRes('account.get', {}) }
  getSettings() { return this.#requestRes('settings.get', {}) }
  kick(username) { return this.#requestRes('room.kick', { username }) }
  mute(username, muted = true) { return this.#requestRes('room.mute', { username, muted }) }
  setRoomAdmin(username, level) { return this.#requestRes('room.setAdmin', { username, level }) }
  setRoomPassword(password) { return this.#requestRes('room.setPassword', { password }) }
  setRoomStatus(inGame) { return this.#requestRes('room.setStatus', { status: inGame ? 'in_game' : 'waiting' }) }
  setMaxPing(ms) { return this.#requestRes('room.setMaxPing', { ms }) }
  setRequireReady(on) { return this.#requestRes('room.setRequireReady', { on: !!on }) }
  setLaunchMode(auto) { return this.#requestRes('room.setLaunchMode', { mode: auto ? 'auto' : 'menu' }) }
  closeRoom() { return this.#requestRes('room.close', {}) }
  showLauncher() { return this.#requestRes('launcher.show', {}) }
  openLauncherPage(page) { return this.#requestRes('launcher.open', { page }) }
  openLogin() { return this.#requestRes('account.openLogin', {}) }
  openForgotPassword() { return this.#requestRes('account.openForgot', {}) }
  logout() { return this.#requestRes('account.logout', {}) }
  ping() { return this.#request('ping', {}) }
  close() { this.#ws?.close() }
}

export async function probeLauncher({ configDir = process.env.BERRY_CONFIG_DIR || (process.env.APPDATA ? process.env.APPDATA + '/Berry' : (process.env.HOME || '.') + '/.config/berry'), game = '', timeoutMs = 1500 } = {}) {
  const { readFileSync } = await import('node:fs')
  let port = 0
  try { port = Number(JSON.parse(readFileSync(configDir + '/sdk/launcher.json', 'utf8')).port) } catch { return { open: false } }
  if (!(port >= 1024 && port < 65536)) return { open: false }
  return await new Promise((resolve) => {
    const ws = new WebSocket(`ws://127.0.0.1:${port}`)
    const timer = setTimeout(() => { try { ws.close() } catch {} resolve({ open: false }) }, timeoutMs)
    ws.onopen = () => ws.send(JSON.stringify({ v: 1, type: 'probe', id: 1, payload: { game } }))
    ws.onmessage = (e) => { clearTimeout(timer); try { const m = JSON.parse(e.data); resolve(m.type === 'probe.ok' ? m.payload : { open: false }) } catch { resolve({ open: false }) } ws.close() }
    ws.onerror = () => { clearTimeout(timer); resolve({ open: false }) }
  })
}
