using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Montinger.Api.Data;
using Montinger.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Pg") ?? builder.Configuration["ConnectionStrings:Pg"]));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/v1/health", () => Results.Ok(new { status = "ok", service = "montinger-api" }));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapChecks();
app.MapResults();
app.MapIncidents();

app.Run();
