using Microsoft.EntityFrameworkCore;
using mp3_storage.Services;
using Mp3Storage.Core;
using Mp3Storage.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

//EFcore
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<Mp3StorageContext>(x => x.UseNpgsql(connectionString));

//Автомиграция 1 версия
//var context = builder.Services.BuildServiceProvider().GetService<Mp3StorageContext>();
//context.Database.Migrate(); 

//Dapper
FileRepository.ConectionString = connectionString;

#region DI
builder.Services.AddTransient<IDownloadService, DownloadService>();
#endregion

//Automapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

// Автомиграции 2й вариант
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<Mp3StorageContext>();
//     context.Database.Migrate(); //EnsureCreated(); //либо то либо другое Migrate - EnsureCreated
// }

app.Run();