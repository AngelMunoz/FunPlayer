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
  | Off
  | One
  | All
  | Random

  member this.AsString =
    match this with
    | Off -> "Stop"
    | One -> "One"
    | All -> "All"
    | Random -> "Random"

  static member FromString value =
    match value with
    | "off"
    | "Off" -> ValueSome Off
    | "one"
    | "One" -> ValueSome One
    | "all"
    | "All" -> ValueSome All
    | "random"
    | "Random" -> ValueSome Random
    | value ->
      printfn $"Invalid Loop State: {value}"
      ValueNone

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
      loopState = Off }

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
    | status -> failwith $"Invalid Status: {status}"

  let (|Off|All|One|Random|) value =
    match value with
    | "off"
    | "Off" -> Off
    | "one"
    | "One" -> One
    | "all"
    | "All" -> All
    | "random"
    | "Random" -> Random
    | value -> failwith $"Invalid Loop State: {value}"

  let state = cval ValueNone
  let playlist = cval Array.empty<Song>
  let setSong (song: PlayerState voption) = state.Publish song

  let setSongs (songs: Song []) = playlist.Publish songs

  let updateLoopState loopState =
    let loopState = LoopState.FromString loopState

    state.Publish (fun state ->
      ValueOption.map
        (fun state ->
          { state with
              loopState =
                loopState
                |> ValueOption.defaultValue state.loopState })
        state)

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
    let playlist (songs: cval<Song []>) (onplaySong: Song -> unit) =
      html.inject (
        "fun-playlist",
        fun () ->
          adaptiview () {
            let! songs = songs

            aside {
              class' "fun-playlist"

              Virtualize'() {
                Items songs

                childContent (fun song ->
                  li {
                    class' "fun-playlist-item"
                    ondblclick (fun _ -> onplaySong song)
                    song.name
                  })
              }
            }
          }
      )

    let mediaBar
      (state: PlayerState aval)
      (onPlay: unit -> Task)
      (onPause: unit -> Task)
      (onUnpause: unit -> Task)
      (onNext: unit -> Task)
      (onBack: unit -> Task)
      (onLoopChange: LoopState -> unit)
      =

      let mediaButtons (state: PlayState) =
        section {
          class' "fun-mb-state-btns"

          match state with
          | Paused ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onBack ())
              "Back"
            }

            button {
              class' "fun-media-btn"
              onclick (fun _ -> onUnpause ())
              "Play"
            }
          | Playing ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onBack ())
              "Back"
            }

            button {
              class' "fun-media-btn"
              onclick (fun _ -> onPause ())
              "Pause"
            }

            button {
              class' "fun-media-btn"
              onclick (fun _ -> onNext ())
              "onNext"
            }
          | Ready
          | Stopped ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onPlay ())
              "Play"
            }
        }

      let loopButtons (state: LoopState) =
        section {
          class' "fun-mb-loop-btns"

          match state with
          | Off ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onLoopChange One)
              "Off"
            }
          | One ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onLoopChange All)
              "One"
            }
          | All ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onLoopChange Random)
              "All"
            }
          | Random ->
            button {
              class' "fun-media-btn"
              onclick (fun _ -> onLoopChange Off)
              "Random"
            }
        }


      html.inject (
        "fun-mb",
        fun () ->
          adaptiview () {
            let! playState = AVal.map (fun value -> value.playState) state
            let! loopState = AVal.map (fun value -> value.loopState) state
            let! currentTime = AVal.map (fun value -> value.currentTime) state
            let! duration = AVal.map (fun value -> value.duration) state
            let! songName = AVal.map (fun value -> value.name) state

            nav {
              class' "fun-mb"

              section {
                class' "fun-mb-media-group"

                h4 {
                  class' "song-title"
                  $"{songName}"
                }

                input {
                  class' "song-slider"
                  type' "range"
                  value currentTime
                  step "any"
                  min 0
                  max duration

                  onchange (fun event ->
                    printfn $"Seek %f{event.Value |> string |> float}")
                }
              }

              section {
                class' "fun-mb-media-buttons"
                mediaButtons playState
                loopButtons loopState
              }

            }
          }
      )



    let library = div { "library" }

  let Titlebar hasOverlay (onOpenDirectory: unit -> unit) : NodeRenderFragment =
    let titlebarClass withOverlay =
      match withOverlay with
      | false -> "fun-titlebar"
      | true -> "fun-titlebar with-overlay"

    html.inject (
      "fun-title-bar",
      fun _ ->
        adaptiview () {
          let! hasOverlay = hasOverlay

          menu {
            class' (titlebarClass hasOverlay)

            button {
              class' "fun-title-bar-menu-item"
              onclick (fun _ -> onOpenDirectory ())
              "Select Music Directory"
            }
          }
        }
    )

module App =
  open Components

  let View () =
    html.inject (
      "fun-player-app",
      fun ((hook: IComponentHook),
           (browser: BrowserSupport),
           (player: Player),
           (fileManager: FileManager)) ->
        let _withOverlay = hook.UseStore false

        hook.OnFirstAfterRender
        |> Observable.map (fun _ ->
          browser.supportsWindowControlsOverlay().AsTask())
        |> Observable.switchTask
        |> Observable.subscribe _withOverlay.Publish
        |> hook.AddDispose

        let onNext () = task { return () } :> Task
        let onBack () = task { return () } :> Task

        let onLoopChange (loopState: LoopState) =
          PlayerState.updateLoopState loopState.AsString

        adaptiview () {
          let! playlist = PlayerState.playlist
          let _withOverlay = hook.UseCVal _withOverlay
          let! withOverlay = _withOverlay

          let playerState =
            PlayerState.state
            |> AVal.map (fun value -> defaultValueArg value PlayerState.empty)

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
    )
