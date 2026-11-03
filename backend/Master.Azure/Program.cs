using DataAccess.EntityFramework.SqLite;
using Master.Impl;
using Shared.Azure;
using Shared.Impl;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services.AddSharedModule();
builder.Services.AddSharedAzureModule();
builder.Services.AddEntityFrameworkSqLiteModule();
builder.Services.AddMasterModule();
builder.Build().Run();
