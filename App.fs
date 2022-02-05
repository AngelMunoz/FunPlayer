namespace Fun.Blazor.Sample

open System.Threading.Tasks
open FSharp.Data.Adaptive
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open FSharp.Control.Reactive
open MudBlazor
open Fun.Blazor
open Fun.Blazor.Sample.Services

module App =

  let private player () =
    html.inject
      (fun (player: Player, files: FileManager, hook: IComponentHook) ->
        let _songs = hook.UseStore Array.empty<string>

        let onSelectFiles _ =
          task {
            do! files.loadFiles ()
            let! files = files.getFiles ()
            _songs.Publish files
          }
          |> ignore


        adaptiview () {
          let! songs = hook.UseAVal _songs

          nav {
            button {
              onclick onSelectFiles

              "Select Directory"
            }
          }

          ul {
            Virtualize' {
              Items songs
              ChildContent
                (fun song ->
                  li {
                    style' "cursor: pointer;"
                    ondblclick (fun _ -> player.play song |> ignore)
                    $"{song}"
                  })
            }

          }

        })

  let View () =
    MudPaper'() {
      class' "app"

      main {
        h1 { "Welcome to Fun.Blazor!" }
        player ()
        counter 0
      }

      footer {
        class' "app-footer"
        "Fun.Blazor.Sample"
      }
    }
