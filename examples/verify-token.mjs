import { verifySdkToken } from '../../server/src/sdksvc.js'
const [token, audience, game, base = 'http://127.0.0.1:8080'] = process.argv.slice(2)
const { key } = await (await fetch(`${base}/api/public/sdk-key`)).json()
const seen = new Set()
try { console.log('valid:', verifySdkToken(token, key, { game, audience, seen })) } catch (e) { console.log('REJECTED:', e.message); process.exit(1) }
