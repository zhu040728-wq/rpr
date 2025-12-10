# TideFlow Nexus API (Visual Studio)

## Run

- 使用 Visual Studio 打开 `TideFlowNexus.Web/TideFlowNexus.Web.csproj`
- 选择启动配置 `http`，直接按 F5 运行
- 浏览器自动打开 `http://localhost:5000/`，前端页面位于 `wwwroot/index.html`

## Endpoints

- Auth: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/me`
- Equipments: `GET /api/equipments`, `GET /api/equipments/:id`, `POST /api/equipments`, `PUT /api/equipments/:id`, `DELETE /api/equipments/:id`
- Health: `GET /api/health/summary`, `GET /api/health/predictions`
- Market: `GET /api/market/orders`, `POST /api/market/orders`, `POST /api/market/execute`
- Transactions: `GET /api/transactions`
- Tokens: `POST /api/tokens/calculate`, `GET /api/tokens`
- Marketplace: `GET /api/marketplace`
- Ecosystem: `GET /api/ecosystem/metrics`, `GET /api/ecosystem/trends`
- Observations: `POST /api/observations`, `GET /api/observations`

## Frontend Demo

运行后直接访问主页即可。
