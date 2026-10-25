using Adapter.Azure;
using Shared.Azure;
using Shared.Impl;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services.AddSharedModule();
builder.Services.AddSharedAzureModule();
builder.Services.AddAdapterAzureModule();
builder.Build().Run();
