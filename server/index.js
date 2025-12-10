const express = require('express')
const cors = require('cors')
const path = require('path')
const app = express()
app.use(cors())
app.use(express.json())
app.use(express.static(path.join(__dirname, '../TideFlowNexus.Web/wwwroot')))

const state = {
  users: [{ id: '1', username: 'demo', password: 'demo', role: 'admin', email: 'demo@example.com' }],
  tokens: [],
  equipments: [
    { id: 'AquaTherm-100', name: 'AquaTherm-100', status: 'Active', health: 87, lastServiced: String(new Date(Date.now()-30*86400000)).slice(0,10), nextPredictedFailure: String(new Date(Date.now()+5*86400000)).slice(0,10) },
    { id: 'WaveSpear-PA-250', name: 'WaveSpear PA-250', status: 'Active', health: 82, lastServiced: String(new Date(Date.now()-20*86400000)).slice(0,10), nextPredictedFailure: String(new Date(Date.now()+10*86400000)).slice(0,10) },
    { id: 'TideRotor-T-2000', name: 'TideRotor T-2000', status: 'Observation', health: 75, lastServiced: String(new Date(Date.now()-50*86400000)).slice(0,10), nextPredictedFailure: String(new Date(Date.now()+20*86400000)).slice(0,10) }
  ],
  orders: [],
  transactions: [],
  ecosystem: { metrics: { pH: 8.1, pollution: 0.12, salinity: 35.0, temp: 18.5 }, trends: Array.from({length:6}, (_,i)=>({ month:i+1, pH: 8.1+(i+1)*0.01, pollution: 0.12-(i+1)*0.005, salinity: 35.0, temp: 18.5+(i+1)*0.2 })) },
  observations: []
}

function ok(data, meta){ return { data, error: null, meta: meta||null } }
function err(res, code, message, status){ res.status(status||400).json({ data: null, error: { code, message }, meta: null }) }
function auth(req){ const t = req.headers['authorization']; if(!t) return null; const u = state.users.find(u => `token-${u.username}` === t); return u||null }

app.post('/api/auth/register', (req, res) => { const { username, password, role, email } = req.body||{}; if(!username || !password) return err(res,'INVALID','username and password required',400); if(state.users.find(u=>u.username===username)) return err(res,'EXISTS','user exists',409); const id = String(Date.now()); state.users.push({ id, username, password, role: role||'user', email: email||'' }); res.json(ok({ id })) })
app.post('/api/auth/login', (req, res) => { const { username, password } = req.body||{}; const u = state.users.find(u=>u.username===username && u.password===password); if(!u) return err(res,'UNAUTHORIZED','invalid credentials',401); const token = `token-${username}`; res.json(ok({ token })) })
app.get('/api/me', (req,res)=>{ const u = auth(req); if(!u) return err(res,'UNAUTHORIZED','invalid token',401); res.json(ok(u)) })
app.put('/api/me', (req,res)=>{ const u = auth(req); if(!u) return err(res,'UNAUTHORIZED','invalid token',401); const { email, role } = req.body||{}; u.email = email||u.email; u.role = role||u.role; res.json(ok(u)) })

app.get('/api/equipments', (req,res)=>{ res.json(ok(state.equipments, { total: state.equipments.length })) })
app.get('/api/equipments/:id', (req,res)=>{ const e = state.equipments.find(x=>x.id===req.params.id); if(!e) return err(res,'NOT_FOUND','equipment not found',404); res.json(ok(e)) })
app.post('/api/equipments', (req,res)=>{ const { name, status, health, lastServiced, nextPredictedFailure } = req.body||{}; const id = String(Date.now()); const row = { id, name, status, health: Number(health||0), lastServiced: lastServiced||'', nextPredictedFailure: nextPredictedFailure||'' }; state.equipments.push(row); res.json(ok(row)) })
app.put('/api/equipments/:id', (req,res)=>{ const i = state.equipments.findIndex(x=>x.id===req.params.id); if(i===-1) return err(res,'NOT_FOUND','equipment not found',404); const cur = state.equipments[i]; const b = req.body||{}; state.equipments[i] = { ...cur, name: b.name||cur.name, status: b.status||cur.status, health: b.health!=null?Number(b.health):cur.health, lastServiced: b.lastServiced||cur.lastServiced, nextPredictedFailure: b.nextPredictedFailure||cur.nextPredictedFailure }; res.json(ok(state.equipments[i])) })
app.delete('/api/equipments/:id', (req,res)=>{ const i = state.equipments.findIndex(x=>x.id===req.params.id); if(i===-1) return err(res,'NOT_FOUND','equipment not found',404); const [removed] = state.equipments.splice(i,1); res.json(ok(removed)) })

app.get('/api/health/summary', (req,res)=>{ const hs = state.equipments.map(x=>({ id:x.id, health:x.health, status:x.status })); const avg = hs.reduce((s,x)=>s+x.health,0)/hs.length; res.json(ok(hs, { avg })) })
app.get('/api/health/predictions', (req,res)=>{ const d = state.equipments.map(x=>({ id:x.id, nextPredictedFailure:x.nextPredictedFailure })); res.json(ok(d)) })

app.get('/api/market/orders', (req,res)=>{ res.json(ok(state.orders, { total: state.orders.length })) })
app.post('/api/market/orders', (req,res)=>{ const { type, amount_kwh, price_per_kwh, user } = req.body||{}; const id = String(Date.now()); const row = { id, type, amount_kwh: Number(amount_kwh||0), price_per_kwh: Number(price_per_kwh||0), created: new Date().toISOString(), user: user||'demo' }; state.orders.push(row); res.json(ok(row)) })
app.post('/api/market/execute', (req,res)=>{ const { type, amount_kwh, price_per_kwh, user } = req.body||{}; const id = String(Date.now()); const total = Number(amount_kwh||0) * Number(price_per_kwh||0); const row = { id, type, amount_kwh: Number(amount_kwh||0), total_price: total, time: new Date().toISOString(), user: user||'demo' }; state.transactions.push(row); res.json(ok(row)) })
app.get('/api/transactions', (req,res)=>{ res.json(ok(state.transactions, { total: state.transactions.length })) })

app.post('/api/tokens/calculate', (req,res)=>{ const { energy_kwh, baseline_kg_per_kwh, owner } = req.body||{}; const baseline = Number(baseline_kg_per_kwh||0.7); const energy = Number(energy_kwh||0); const tonnes = (baseline * energy)/1000; const tokens = Math.floor(tonnes); const id = String(Date.now()); const row = { id, owner: owner||'demo', amount_tonnes: tonnes, created: new Date().toISOString() }; state.tokens.push(row); res.json(ok({ id, tokens, tonnes })) })
app.get('/api/tokens', (req,res)=>{ res.json(ok(state.tokens, { total: state.tokens.length })) })

app.get('/api/marketplace', (req,res)=>{ const items = [ {id:'coat',name:'Coat',price:120}, {id:'jacket',name:'Jacket',price:90}, {id:'detergent',name:'Eco Detergent',price:30}, {id:'snack',name:'Snack',price:8} ]; res.json(ok(items)) })

app.get('/api/ecosystem/metrics', (req,res)=>{ res.json(ok(state.ecosystem.metrics)) })
app.get('/api/ecosystem/trends', (req,res)=>{ res.json(ok(state.ecosystem.trends)) })

app.post('/api/observations', (req,res)=>{ const { user, description, photo } = req.body||{}; const id = String(Date.now()); const row = { id, user: user||'demo', description: description||'', photo: photo||'', created: new Date().toISOString() }; state.observations.push(row); res.json(ok(row)) })
app.get('/api/observations', (req,res)=>{ res.json(ok(state.observations, { total: state.observations.length })) })

const port = process.env.PORT || 4000
app.listen(port, ()=>{ console.log(`API on http://localhost:${port}`) })

