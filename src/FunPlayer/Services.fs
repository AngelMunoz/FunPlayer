namespace FunPlayer

open System.Threading.Tasks
open Microsoft.JSInterop

module Services =
  type Player =
    abstract play: string -> ValueTask
    abstract pause: unit -> ValueTask
    abstract unpause: unit -> ValueTask

  type FileManager =
    abstract getFiles: unit -> ValueTask<string []>
    abstract loadFiles: unit -> ValueTask

  type BrowserSupport =
    abstract supportsWindowControlsOverlay: unit -> ValueTask<bool>
    abstract supportsFileSystemAccess: unit -> ValueTask<bool>


  let GetFunPlayer (js: IJSRuntime) =
    { new Player with
        override _.play name =
          js.InvokeVoidAsync("FunPlayer.play", name)

        override _.unpause() = js.InvokeVoidAsync("FunPlayer.unpause")

        override _.pause() = js.InvokeVoidAsync("FunPlayer.pause") }

  let GetFunFiles (js: IJSRuntime) =
    { new FileManager with
        override _.getFiles() = js.InvokeAsync("FunPlayer.getFiles")

        override _.loadFiles() =
          js.InvokeVoidAsync("FunPlayer.loadFiles") }

  let GetBrowserSupport (js: IJSRuntime) =
    { new BrowserSupport with
        override _.supportsFileSystemAccess() : ValueTask<bool> =
          js.InvokeAsync("FunPlayer.supportsFileSystemAccess")

        override _.supportsWindowControlsOverlay() : ValueTask<bool> =
          js.InvokeAsync("FunPlayer.supportsWindowControlsOverlay") }
