using Dehb.WebApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddWebApiDiConfiguration(builder.Configuration);
builder.Services.AddExceptionHandler<WebApiExceptionHandler>();

WebApplication app = builder.Build();

//await app.Services.EnsureCreatedAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.AddEndpointsConfiguration();

app.Run();
