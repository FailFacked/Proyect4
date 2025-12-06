using Api.Data;
using Api.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// JWT CONFIG
var jwtSettings = new JwtSettings();
builder.Services.AddSingleton(jwtSettings);

// DB CONTEXT
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AUTHENTICATION
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

// ⬅️ AGREGA ESTO
builder.Services.AddAuthorization();
// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// LOGIN ENDPOINT
string GenerateJwt(string username, JwtSettings jwtSettings)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username),
    };

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwtSettings.Issuer,
        audience: jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/login", (Api.Models.User u, JwtSettings jwt) =>
{
    if (u.Username == "Admin" && u.Password == "Admin")
    {
        var token = GenerateJwt(u.Username, jwt);
        return Results.Ok(new { token });
    }

    return Results.Unauthorized();
});

app.MapGet("/users", async (AppDbContext db) =>
{
    return await db.Users.ToListAsync();
});
Console.WriteLine($"KEY: {jwtSettings.Key}");
Console.WriteLine($"ISSUER: {jwtSettings.Issuer}");
Console.WriteLine($"AUDIENCE: {jwtSettings.Audience}");
app.Run();
