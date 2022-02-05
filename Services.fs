namespace Fun.Blazor.Sample

open System.Threading.Tasks
open Microsoft.JSInterop

module Services =
  type Player =
      abstract play: string -> ValueTask
      abstract pause: unit -> ValueTask
      
  type FileManager =
      abstract getFiles: unit -> ValueTask<string[]>
      abstract loadFiles: unit -> ValueTask

  
  
  let GetFunPlayer (js: IJSRuntime) =
      { new Player with
          override _.play name =
              js.InvokeVoidAsync("FunPlayer.play", name)
          override _.pause() = js.InvokeVoidAsync("FunPlayer.pause") }

  let GetFunFiles (js: IJSRuntime) =
      { new FileManager with
          override _.getFiles() = js.InvokeAsync("FunPlayer.getFiles")
          override _.loadFiles() = js.InvokeVoidAsync("FunPlayer.loadFiles") }
