using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LockboxDbContext>(options =>
    options.UseSqlite("Data Source=lockbox.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/health" , () =>{
    
string status = "Ok";

return status;

})
.WithName("GetHealth");

app.MapPost("/upload", async (IFormFile file, LockboxDbContext db) =>
{
    string guid = Guid.NewGuid().ToString();
    string ending = Path.GetExtension(file.FileName);

    string result = $"{guid}{ending}";

    using var stream = new FileStream("uploads/" + result, FileMode.Create);
    await file.CopyToAsync(stream);

    var newRecord = new FileRecord {OriginalFileName = file.FileName, GUIDFileName = result, UploadTime = DateTime.Now};

    db.FileRecords.Add(newRecord);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "File received", fileName = file.FileName });
})
.WithName("UploadFile")
.DisableAntiforgery();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
