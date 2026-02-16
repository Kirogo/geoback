using Microsoft.EntityFrameworkCore;
using geoback.Data;
using geoback.Models;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GeoDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 23))));

// 2. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGeoFront",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// 2.1 Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "ThisIsASecretKeyForDevelopmentOnly12345!MakeSureItIsLongEnough";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey))
    };
});

// 3. Controllers
builder.Services.AddControllers();

// 4. OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowGeoFront");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<GeoDbContext>();
        context.Database.EnsureCreated();

        // Create Users table manually if it doesn't exist (because EnsureCreated doesn't update existing DBs)
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id CHAR(36) NOT NULL,
                Email VARCHAR(255) NOT NULL,
                PasswordHash LONGTEXT NOT NULL,
                FirstName LONGTEXT NOT NULL,
                LastName LONGTEXT NOT NULL,
                Role LONGTEXT NOT NULL,
                CreatedAt DATETIME(6) NOT NULL,
                UpdatedAt DATETIME(6) NOT NULL,
                PRIMARY KEY (Id),
                UNIQUE INDEX IX_Users_Email (Email)
            );
        ");

        // Ensure Clients table has ProjectName column (manual migration for EnsureCreated limitation)
        try 
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Clients ADD ProjectName LONGTEXT NULL;");
        }
        catch (Exception ex)
        {
            // Ignore if column already exists (Error 1060 in MySQL)
            if (!ex.Message.Contains("Duplicate column name") && !ex.Message.Contains("1060"))
            {
                Console.WriteLine($"Note: Client table update attempted but failed: {ex.Message}");
            }
        }

        // Seed a test user if none exists
        if (!context.Users.Any(u => u.Email == "test@geobuild.com"))
        {
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@geobuild.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(testUser);
            context.SaveChanges();
            Console.WriteLine("Test user created: test@geobuild.com / Password123!");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();
