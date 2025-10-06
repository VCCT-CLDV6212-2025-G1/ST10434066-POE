using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;

var builder = FunctionsApplication.CreateBuilder(args);
    builder.ConfigureFunctionsWebApplication();
 var app = builder.Build();
app.Run();
      
   
