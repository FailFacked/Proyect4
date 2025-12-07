using Api.Data;
using Api.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.GetConnectionString("DefaultConnection");
// Cargar JWT desde appsettings o variables de entorno
var jwtSettings = new JwtSettings
{
    Key = builder.Configuration["Jwt:Key"] ?? "DefaultKey123!",
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "DefaultIssuer",
    Audience = builder.Configuration["Jwt:Audience"] ?? "DefaultAudience"
};

builder.Services.AddSingleton(jwtSettings);

// DB CONTEXT

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

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

// autorization
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Función para generar JWT
string GenerateJwt(string username, JwtSettings jwt)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username)
    };

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwt.Issuer,
        audience: jwt.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds
    );

    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

// LOGIN
app.MapPost("/login", (Api.Models.User u, JwtSettings jwt) =>
{
    if (u.Username == "Admin" && u.Password == "Admin")
    {
        return Results.Ok(new
        {
            token = GenerateJwt(u.Username, jwt)
        });
    }

    return Results.Unauthorized();
});

// GET USERS
app.MapGet("/users", async (AppDbContext db) =>
    await db.Users.ToListAsync()
);

// ADD USER
app.MapPost("/users", async (Api.Models.User newUser, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Username) ||
        string.IsNullOrWhiteSpace(newUser.Password))
    {
        return Results.BadRequest("Username and password are required.");
    }

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{newUser.Id}", newUser);
});

app.Run();
