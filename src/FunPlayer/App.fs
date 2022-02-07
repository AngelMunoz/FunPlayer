namespace FunPlayer

open System.Threading.Tasks
open FSharp.Data.Adaptive
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.JSInterop
open FSharp.Control.Reactive
open MudBlazor
open Fun.Blazor
open FunPlayer.Services

[<Struct>]
type PlayState =
  | Ready
  | Paused
  | Playing
  | Stoped

[<Struct>]
type PlayerState =
  { songName: string
    duration: float
    currentTime: float
    playState: PlayState }


module PlayerState =

  let (|IsReady|IsPlaying|IsPaused|ItStoped|) str =
    match str with
    | "is-playing"
    | "IsPlaying" -> IsPlaying
    | "is-paused"
    | "IsPaused" -> IsPaused
    | "is-ready"
    | "IsReady" -> IsReady
    | "it-stoped"
    | "ItStoped" -> ItStoped
    | _ -> failwith "olv"

  let state = cval (ValueNone)

  let setSong (song: PlayerState voption) = state.Publish song

  [<JSInvokable>]
  let updatePlayStatus status =
    let playState =
      match status with
      | IsPlaying -> Ready
      | IsPaused -> Paused
      | IsReady -> Ready
      | ItStoped -> Stoped

    state.Publish (fun value ->
      value
      |> ValueOption.map (fun value -> { value with playState = playState }))

  [<JSInvokable>]
  let durationChanged (newDuration: float) =
    state.Publish (fun value ->
      value
      |> ValueOption.map (fun value -> { value with duration = newDuration }))

  [<JSInvokable>]
  let timeUpdate (newTime: float) =
    state.Publish (fun value ->
      value
      |> ValueOption.map (fun value -> { value with currentTime = newTime }))

  [<JSInvokable>]
  let playerEnded () =
    state.Publish (fun value ->
      value
      |> ValueOption.map (fun value -> { value with playState = Stoped }))

module App =

  let private player =
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

        let playSong song =
          PlayerState.state.Publish (fun _ ->
            { songName = song
              duration = 0.
              currentTime = 0.
              playState = Stoped }
            |> ValueSome)

          player.play(song).AsTask()
          |> Async.AwaitTask
          |> Async.Start

        adaptiview () {
          let! songs = hook.UseAVal _songs

          nav {
            MudButton'() {
              onclick onSelectFiles

              "Select Directory"
            }
          }

          MudList'() {
            class' "playlist"
            Clickable true

            Virtualize'() {
              Items songs

              ChildContent (fun song ->
                MudListItem'() {
                  ondblclick (fun _ -> playSong song)
                  $"{song}"
                })
            }
          }

        })

  let View () =
    let defaultTheme =
      MudTheme(Palette = Palette(Primary = "#8D6E63", Secondary = "#26A69A"))

    adaptiview () {
      let! isMenuOpen, setMenuOpen = cval(false).WithSetter()
      let! playerState = PlayerState.state
      MudThemeProvider'() { Theme defaultTheme }
      MudDialogProvider'()
      MudSnackbarProvider'()

      MudLayout'() {
        MudAppBar'() {
          id "appbar"
          Color Color.Primary
          Elevation 2
          Dense true
          Fixed true

          MudIconButton'() {
            id "menu-button"
            onclick (fun _ -> isMenuOpen |> not |> setMenuOpen)
            Icon Icons.Material.Filled.Menu
            Color Color.Inherit
            Edge Edge.Start
          }

          "FunPlayer"
        }

        MudDrawer'() {
          Open isMenuOpen
          ClipMode DrawerClipMode.Always
          Elevation 2

          MudDrawerHeader'() {
            MudHidden'() {
              Breakpoint Breakpoint.MdAndUp

              MudIconButton'() {
                onclick (fun _ -> isMenuOpen |> not |> setMenuOpen)
                Icon Icons.Material.Filled.Menu
                Color Color.Primary
                Edge Edge.Start
              }
            }

            MudText'() {
              Typo Typo.h5
              "Fun Player"
            }
          }

          MudNavMenu'() {
            href "/"
            "Home"
          }
        }

        MudMainContent'() {
          MudContainer'() {
            MaxWidth MaxWidth.Large

            main {
              MudText'() {
                Typo Typo.h5
                "Welcome to Fun Player!"
              }

              p { sprintf $"%A{playerState}" }

              player
            }
          }
        }
      }

    }
