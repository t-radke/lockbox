using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//SQLite DB
builder.Services.AddDbContext<LockboxDbContext>(options =>
    options.UseSqlite("Data Source=lockbox.db"));

//Password hasher to hand over and create an instance
builder.Services.AddScoped<PasswordHasher<User>, PasswordHasher<User>>();

//authentication system with the app 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

//activate checking on every request
app.UseAuthentication();
app.UseAuthorization();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


//Initial health check make sure we can communicate with backend
app.MapGet("/health" , () =>{
    
string status = "Ok";

return status;

})
.WithName("GetHealth");

app.MapPost("/upload", async (IFormFile file, LockboxDbContext db) =>
{
    //generating GUID and attaching it to file name extension, concatinating them afterwards
    string guid = Guid.NewGuid().ToString();
    string ending = Path.GetExtension(file.FileName);

    string result = $"{guid}{ending}";

    //creating new file stream and placing file in uploads folder
    using var stream = new FileStream("uploads/" + result, FileMode.Create);
    await file.CopyToAsync(stream);

    //creating new record AFTER file is uploaded and then putting it in the DB
    var newRecord = new FileRecord {OriginalFileName = file.FileName, GUIDFileName = result, UploadTime = DateTime.Now};

    db.FileRecords.Add(newRecord);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "File received", fileName = file.FileName });
})
.WithName("UploadFile")
.RequireAuthorization()
.DisableAntiforgery();

app.MapGet("/download/{id}", async (int id, LockboxDbContext db) =>
{
    //assigning record id to var record
    var record = await db.FileRecords.FindAsync(id);

    // guard clause
    if (record == null)
    {
        return Results.NotFound();
    } 
    
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", record.GUIDFileName);
    return Results.File(filePath, "application/octet-stream", record.OriginalFileName);
})
.WithName("DownloadFile")
.RequireAuthorization()
.DisableAntiforgery();


app.MapPost("/register", async (RegisterRequest request, LockboxDbContext db, PasswordHasher<User> hasher) =>
{
    var hashedPassword = hasher.HashPassword(null!, request.Password);
    var newUser = new User { Email = request.Email, PasswordHash = hashedPassword};
    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "User created successfuly!"});

});

app.MapPost("/login", async (LoginRequest request, LockboxDbContext db, PasswordHasher<User> hasher) =>
{

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Results.Json(new { message = "Username or password is not correct or cannot be found" }, statusCode: 401);
        }

    var verifyPassword = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verifyPassword == PasswordVerificationResult.Failed)
        {
            return Results.Json(new { message = "Username or password is not correct or cannot be found" }, statusCode: 401);
        }

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        }),
        Expires = DateTime.UtcNow.AddHours(24),
        Issuer = builder.Configuration["Jwt:Issuer"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);
    

    return Results.Ok(new { token = jwt });
}
)
.WithName("Login")
.DisableAntiforgery();


app.Run();


public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);

