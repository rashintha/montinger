using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/v1/health", () => Results.Ok(new { status = "ok", service = "montinger-api" }));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
