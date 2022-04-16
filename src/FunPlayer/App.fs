namespace FunPlayer

open System
open System.Threading.Tasks
open FSharp.Data.Adaptive
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.AspNetCore.Components.Web.Virtualization
open Microsoft.JSInterop
open FSharp.Control.Reactive
open Fun.Blazor
open FunPlayer
open FunPlayer.Services

type Song =
  { name: string
    fullName: string }
  static member FromString(value: string) =
    { fullName = value
      name = value[.. value.LastIndexOf('.')] }

[<Struct>]
type PlayState =
  | Ready
  | Paused
  | Playing
  | Stopped

[<Struct>]
type LoopState =
  | Stop
  | One
  | All
  | Random

[<Struct>]
type PlayerState =
  { name: string
    duration: float
    currentTime: float
    playState: PlayState
    loopState: LoopState }


module PlayerState =

  let empty =
    { name = ""
      duration = 0.
      currentTime = 0.
      playState = Stopped
      loopState = Stop }

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

  let state = cval ValueNone
  let playlist = cval Array.empty<Song>
  let setSong (song: PlayerState voption) = state.Publish song
  let setSongs (songs: Song []) = playlist.Publish songs

  [<JSInvokable>]
  let updatePlayStatus status =
    let playState =
      match status with
      | IsPlaying -> Ready
      | IsPaused -> Paused
      | IsReady -> Ready
      | ItStoped -> Stopped

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
      |> ValueOption.map (fun value -> { value with playState = Stopped }))

module Components =
  module Player =
    let playlist (songs: Song [] cval) (onplaySong: Song -> unit) =
      let inline view () =

        adaptiview () {
          let! songs = songs

          aside {
            class' "fun-playlist"

            Virtualize'() {
              Items songs

              childContent (fun song ->
                li {
                  ondblclick (fun _ -> onplaySong song)
                  song.name
                })
            }
          }

        }

      html.inject ("fun-playlist", view)

    let mediaBar
      (state: PlayerState cval)
      (onPlay: unit -> Task)
      (onPause: unit -> Task)
      (onUnpause: unit -> Task)
      (onNext: unit -> Task)
      (onBack: unit -> Task)
      (onLoopChange: LoopState -> unit)
      =
      let view () =
        adaptiview () {
          let! state = state

          let mediaButtons =
            section {
              class' "fun-mb-state-btns"

              match state.playState with
              | Paused ->
                button {
                  onclick (fun _ -> onBack ())
                  "Back"
                }

                button {
                  onclick (fun _ -> onUnpause ())
                  "Play"
                }
              | Playing ->
                button {
                  onclick (fun _ -> onBack ())
                  "Back"
                }

                button {
                  onclick (fun _ -> onPause ())
                  "Pause"
                }

                button {
                  onclick (fun _ -> onNext ())
                  "onNext"
                }
              | Ready
              | Stopped ->
                button {
                  onclick (fun _ -> onPlay ())
                  "Play"
                }
            }

          let loopButtons =
            section {
              class' "fun-mb-loop-btns"

              match state.loopState with
              | Stop ->
                button {
                  onclick (fun _ -> onLoopChange One)
                  "Off"
                }
              | One ->
                button {
                  onclick (fun _ -> onLoopChange All)
                  "One"
                }
              | All ->
                button {
                  onclick (fun _ -> onLoopChange Random)
                  "All"
                }
              | Random ->
                button {
                  onclick (fun _ -> onLoopChange Stop)
                  "Random"
                }
            }

          nav {
            class' "fun-mb"

            section {
              class' "fun-mb-media-group"

              input {
                class' "fun-mb-slider"
                type' "range"
                value state.currentTime
                step "any"
                min 0
                max state.duration

                onchange (fun event ->
                  printfn $"Seek %f{event.Value |> string |> float}")
              }

              mediaButtons
            }

            loopButtons
          }
        }

      html.inject ("fun-mb", view)



    let library = div { "library" }

  let Titlebar hasOverlay (onOpenDirectory: unit -> unit) : NodeRenderFragment =
    let titlebarClass withOverlay =
      match withOverlay with
      | false -> "fun-titlebar"
      | true -> "fun-titlebar with-overlay"

    let inline view () =
      adaptiview () {
        let! hasOverlay = hasOverlay

        menu {
          class' (titlebarClass hasOverlay)

          li {
            class' "fun-title-bar-menu-item"
            onclick (fun _ -> onOpenDirectory ())
            b { "Open Directory" }
          }
        }
      }

    html.inject ("fun-title-bar", view)

module App =
  open Components

  let View () =
    let inline view
      (
        hook: IComponentHook,
        browser: BrowserSupport,
        player: Player,
        fileManager: FileManager
      ) =
      let _withOverlay = hook.UseStore false

      hook.OnFirstAfterRender
      |> Observable.map (fun _ ->
        printfn "Olv"
        browser.supportsWindowControlsOverlay().AsTask())
      |> Observable.switchTask
      |> Observable.subscribe _withOverlay.Publish
      |> hook.AddDispose

      let onNext () = task { return () } :> Task
      let onBack () = task { return () } :> Task
      let onLoopChange loopState = ()

      adaptiview () {
        let! playlist = PlayerState.playlist
        let _withOverlay = hook.UseCVal _withOverlay
        let! withOverlay = _withOverlay
        let! playerState = PlayerState.state

        let playerState =
          playerState
          |> ValueOption.defaultValue PlayerState.empty
          |> cval

        let loadSongs () =
          task {
            do! fileManager.loadFiles ()
            let! files = fileManager.getFiles ()

            files
            |> Array.map Song.FromString
            |> PlayerState.setSongs
          }

        let onPlaySong (song: Song voption) =
          task {
            match song with
            | ValueSome song ->
              { PlayerState.empty with name = song.name }
              |> ValueSome
              |> PlayerState.setSong

              do! player.play (song.fullName)
            | ValueNone ->
              match playlist |> Array.tryHead with
              | Some song ->
                { PlayerState.empty with name = song.name }
                |> ValueSome
                |> PlayerState.setSong

                do! player.play (song.fullName)
              | None -> printfn "No songs available"
          }
          :> Task

        let titlebarClass =
          match withOverlay with
          | false -> "fun-app"
          | true -> "fun-app with-overlay"

        main {
          class' titlebarClass
          Titlebar _withOverlay (fun _ -> loadSongs () |> ignore)

          Player.playlist PlayerState.playlist (fun song ->
            song |> ValueSome |> onPlaySong |> ignore)

          Player.mediaBar
            playerState
            (fun _ -> onPlaySong ValueNone)
            (fun _ -> player.pause().AsTask())
            (fun _ -> player.unpause().AsTask())
            onNext
            onBack
            onLoopChange
        }
      }

    html.inject view
