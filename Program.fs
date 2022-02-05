open System
open Fun.Blazor.Sample.Services
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Fun.Blazor.Sample
open Microsoft.JSInterop

let builder =
  WebAssemblyHostBuilder.CreateDefault(Environment.GetCommandLineArgs())

builder
  .AddFunBlazor("#main", App.View())
  .Services
    .AddFunBlazorWasm()
    .AddScoped<Player>(fun services -> services.GetService<IJSRuntime>() |> GetFunPlayer)
    .AddScoped<FileManager>(fun services -> services.GetService<IJSRuntime>() |> GetFunFiles)

|> ignore

builder.Build().RunAsync()
|> Async.AwaitTask
|> Async.Start
