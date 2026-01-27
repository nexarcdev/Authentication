var builder = DistributedApplication.CreateBuilder(args);

var apiApp = builder.AddProject<Projects.Authentication_Examples_Api>("api");
var auth0 = builder.AddProject<Projects.Authentication_Examples_Client_Auth0B2C>("auth0");
var azure = builder.AddProject<Projects.Authentication_Examples_Client_AzureB2C>("azure");
var device = builder.AddProject<Projects.Authentication_Examples_Client_DevicePairing>("device");
var gsuite = builder.AddProject<Projects.Authentication_Examples_Client_GoogleWorkspace>("gsuite");
var magic = builder.AddProject<Projects.Authentication_Examples_Client_MagicLink>("magic");
var m365 = builder.AddProject<Projects.Authentication_Examples_Client_Microsoft365>("m365");

 auth0.WaitFor(apiApp);
 azure.WaitFor(apiApp);
 device.WaitFor(apiApp);
 gsuite.WaitFor(apiApp);
 magic.WaitFor(apiApp);
 m365.WaitFor(apiApp);

builder.Build().Run();
