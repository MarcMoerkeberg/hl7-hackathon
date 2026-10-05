using Hl7Hackathon.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddHl7Hackathon();

builder.Build().Run();
