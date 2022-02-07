open System
open MudBlazor.Services
open FunPlayer.Services
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open FunPlayer
open Microsoft.JSInterop

let builder =
  WebAssemblyHostBuilder.CreateDefault(Environment.GetCommandLineArgs())

builder
  .AddFunBlazor("#main", App.View())
  .Services.AddFunBlazorWasm()
  .AddMudServices()
  .AddScoped<Player>(fun services ->
    services.GetService<IJSRuntime>() |> GetFunPlayer)
  .AddScoped<FileManager>(fun services ->
    services.GetService<IJSRuntime>() |> GetFunFiles)
  .AddScoped<BrowserSupport>(fun services ->
    services.GetService<IJSRuntime>()
    |> GetBrowserSupport)

|> ignore

builder.Build().RunAsync()
|> Async.AwaitTask
|> Async.Start
