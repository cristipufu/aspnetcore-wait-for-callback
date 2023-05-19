using StackExchange.Redis;
using WaitForCallback.Infrastructure;
using WaitForCallback.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IRequestsQueue<JobModel>, RequestsQueue<JobModel>>();

/* Multi-node with Redis PubSub
 *
var redisOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
var connectionMultiplexer = ConnectionMultiplexer.Connect(redisOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => connectionMultiplexer);

builder.Services.AddSingleton<IRequestsQueue<JobModel>, RedisRequestsQueu<JobModel>>();
*/

// Web Api

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseAuthorization();

app.MapControllers();

app.Run();
