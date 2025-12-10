using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
var app = builder.Build();
app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseDefaultFiles();
app.UseStaticFiles();

var users = new List<User> { new("1", "demo", "demo", "admin", "demo@example.com") };
var tokens = new List<Token>();
var equipments = new List<Equipment>
{
    new("AquaTherm-100", "AquaTherm-100", "Active", 87, DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), DateOnly.FromDateTime(DateTime.Today.AddDays(5))),
    new("WaveSpear-PA-250", "WaveSpear PA-250", "Active", 82, DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), DateOnly.FromDateTime(DateTime.Today.AddDays(10))),
    new("TideRotor-T-2000", "TideRotor T-2000", "Observation", 75, DateOnly.FromDateTime(DateTime.Today.AddDays(-50)), DateOnly.FromDateTime(DateTime.Today.AddDays(20)))
};
var orders = new List<Order>();
var transactions = new List<Transaction>();
var ecoMetrics = new EcosystemMetrics(8.1, 0.12, 35.0, 18.5);
var ecoTrends = Enumerable.Range(1, 6).Select(i => new EcosystemTrend(i, 8.1 + i * 0.01, 0.12 - i * 0.005, 35.0, 18.5 + i * 0.2)).ToList();
var observations = new List<Observation>();

app.MapPost("/api/auth/register", (RegisterBody body) =>
{
    if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password)) return Results.Json(new Api(null, new ApiError("INVALID", "username and password required"), null), statusCode: 400);
    if (users.Any(u => u.Username == body.Username)) return Results.Json(new Api(null, new ApiError("EXISTS", "user exists"), null), statusCode: 409);
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    users.Add(new User(id, body.Username, body.Password, body.Role ?? "user", body.Email ?? ""));
    return Results.Json(new Api(new { id }, null, null));
});

app.MapPost("/api/auth/login", (LoginBody body) =>
{
    var u = users.FirstOrDefault(x => x.Username == body.Username && x.Password == body.Password);
    if (u is null) return Results.Json(new Api(null, new ApiError("UNAUTHORIZED", "invalid credentials"), null), statusCode: 401);
    var token = $"token-{u.Username}";
    return Results.Json(new Api(new { token }, null, null));
});

app.MapGet("/api/me", (HttpRequest req) =>
{
    var token = req.Headers["Authorization"].FirstOrDefault();
    var u = users.FirstOrDefault(x => $"token-{x.Username}" == token);
    if (u is null) return Results.Json(new Api(null, new ApiError("UNAUTHORIZED", "invalid token"), null), statusCode: 401);
    return Results.Json(new Api(u, null, null));
});

app.MapPut("/api/me", (HttpRequest req, UpdateMeBody body) =>
{
    var token = req.Headers["Authorization"].FirstOrDefault();
    var idx = users.FindIndex(x => $"token-{x.Username}" == token);
    if (idx < 0) return Results.Json(new Api(null, new ApiError("UNAUTHORIZED", "invalid token"), null), statusCode: 401);
    var cur = users[idx];
    users[idx] = cur with { Email = body.Email ?? cur.Email, Role = body.Role ?? cur.Role };
    return Results.Json(new Api(users[idx], null, null));
});

app.MapGet("/api/equipments", () => Results.Json(new Api(equipments, null, new { total = equipments.Count })));
app.MapGet("/api/equipments/{id}", (string id) =>
{
    var e = equipments.FirstOrDefault(x => x.Id == id);
    if (e is null) return Results.Json(new Api(null, new ApiError("NOT_FOUND", "equipment not found"), null), statusCode: 404);
    return Results.Json(new Api(e, null, null));
});
app.MapPost("/api/equipments", (EquipmentCreateBody body) =>
{
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var row = new Equipment(id, body.Name, body.Status, body.Health, DateOnly.Parse(body.LastServiced), DateOnly.Parse(body.NextPredictedFailure));
    equipments.Add(row);
    return Results.Json(new Api(row, null, null));
});
app.MapPut("/api/equipments/{id}", (string id, EquipmentUpdateBody body) =>
{
    var i = equipments.FindIndex(x => x.Id == id);
    if (i < 0) return Results.Json(new Api(null, new ApiError("NOT_FOUND", "equipment not found"), null), statusCode: 404);
    var cur = equipments[i];
    equipments[i] = cur with
    {
        Name = body.Name ?? cur.Name,
        Status = body.Status ?? cur.Status,
        Health = body.Health ?? cur.Health,
        LastServiced = string.IsNullOrWhiteSpace(body.LastServiced) ? cur.LastServiced : DateOnly.Parse(body.LastServiced!),
        NextPredictedFailure = string.IsNullOrWhiteSpace(body.NextPredictedFailure) ? cur.NextPredictedFailure : DateOnly.Parse(body.NextPredictedFailure!)
    };
    return Results.Json(new Api(equipments[i], null, null));
});
app.MapDelete("/api/equipments/{id}", (string id) =>
{
    var i = equipments.FindIndex(x => x.Id == id);
    if (i < 0) return Results.Json(new Api(null, new ApiError("NOT_FOUND", "equipment not found"), null), statusCode: 404);
    var removed = equipments[i];
    equipments.RemoveAt(i);
    return Results.Json(new Api(removed, null, null));
});

app.MapGet("/api/health/summary", () =>
{
    var hs = equipments.Select(x => new { id = x.Id, health = x.Health, status = x.Status }).ToList();
    var avg = hs.Average(x => Convert.ToDouble(x.health));
    return Results.Json(new Api(hs, null, new { avg }));
});
app.MapGet("/api/health/predictions", () =>
{
    var d = equipments.Select(x => new { id = x.Id, nextPredictedFailure = x.NextPredictedFailure.ToString() }).ToList();
    return Results.Json(new Api(d, null, null));
});

app.MapGet("/api/market/orders", () => Results.Json(new Api(orders, null, new { total = orders.Count })));
app.MapPost("/api/market/orders", (OrderCreateBody body) =>
{
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var row = new Order(id, body.Type, body.Amount_kwh, body.Price_per_kwh, DateTime.UtcNow, body.User ?? "demo");
    orders.Add(row);
    return Results.Json(new Api(row, null, null));
});
app.MapPost("/api/market/execute", (ExecuteBody body) =>
{
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var total = (body.Amount_kwh) * (body.Price_per_kwh);
    var row = new Transaction(id, body.Type, body.Amount_kwh, total, DateTime.UtcNow, body.User ?? "demo");
    transactions.Add(row);
    return Results.Json(new Api(row, null, null));
});
app.MapGet("/api/transactions", () => Results.Json(new Api(transactions, null, new { total = transactions.Count })));

app.MapPost("/api/tokens/calculate", (TokenCalcBody body) =>
{
    var baseline = body.Baseline_kg_per_kwh ?? 0.7;
    var energy = body.Energy_kwh ?? 0;
    var tonnes = (baseline * energy) / 1000.0;
    var tokensCount = Math.Floor(tonnes);
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var row = new Token(id, body.Owner ?? "demo", tonnes, DateTime.UtcNow);
    tokens.Add(row);
    return Results.Json(new Api(new { id, tokens = tokensCount, tonnes }, null, null));
});
app.MapGet("/api/tokens", () => Results.Json(new Api(tokens, null, new { total = tokens.Count })));

app.MapGet("/api/marketplace", () => Results.Json(new Api(new[]
{
    new { id = "coat", name = "Coat", price = 120 },
    new { id = "jacket", name = "Jacket", price = 90 },
    new { id = "detergent", name = "Eco Detergent", price = 30 },
    new { id = "snack", name = "Snack", price = 8 }
}, null, null)));

app.MapGet("/api/ecosystem/metrics", () => Results.Json(new Api(ecoMetrics, null, null)));
app.MapGet("/api/ecosystem/trends", () => Results.Json(new Api(ecoTrends, null, null)));

app.MapPost("/api/observations", (ObservationCreateBody body) =>
{
    var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    var row = new Observation(id, body.User ?? "demo", body.Description ?? "", body.Photo ?? "", DateTime.UtcNow);
    observations.Add(row);
    return Results.Json(new Api(row, null, null));
});
app.MapGet("/api/observations", () => Results.Json(new Api(observations, null, new { total = observations.Count })));

app.Run();

record Api(object? Data, ApiError? Error, object? Meta);
record ApiError(string Code, string Message);
record User(string Id, string Username, string Password, string Role, string Email);
record Token(string Id, string Owner, double Amount_tonnes, DateTime Created);
record Equipment(string Id, string Name, string Status, int Health, DateOnly LastServiced, DateOnly NextPredictedFailure);
record Order(string Id, string? Type, double Amount_kwh, double Price_per_kwh, DateTime Created, string User);
record Transaction(string Id, string? Type, double Amount_kwh, double Total_price, DateTime Time, string User);
record EcosystemMetrics(double pH, double pollution, double salinity, double temp);
record EcosystemTrend(int month, double pH, double pollution, double salinity, double temp);
record Observation(string Id, string User, string Description, string Photo, DateTime Created);

record RegisterBody(string Username, string Password, string? Role, string? Email);
record LoginBody(string Username, string Password);
record UpdateMeBody(string? Email, string? Role);
record EquipmentCreateBody(string Name, string Status, int Health, string LastServiced, string NextPredictedFailure);
record EquipmentUpdateBody(string? Name, string? Status, int? Health, string? LastServiced, string? NextPredictedFailure);
record OrderCreateBody(string? Type, double Amount_kwh, double Price_per_kwh, string? User);
record ExecuteBody(string? Type, double Amount_kwh, double Price_per_kwh, string? User);
record TokenCalcBody(double? Energy_kwh, double? Baseline_kg_per_kwh, string? Owner);
record ObservationCreateBody(string? User, string? Description, string? Photo);
