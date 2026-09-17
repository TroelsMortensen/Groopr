using BlazorUI;
using BlazorUI.Data;
using BlazorUI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<InputConfiguration>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<IDialogService>(sp => sp.GetRequiredService<DialogService>());
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IStudentCsvFileService, StudentCsvFileService>();

await builder.Build().RunAsync();
