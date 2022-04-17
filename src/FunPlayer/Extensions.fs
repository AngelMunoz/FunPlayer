[<AutoOpen>]
module FunPlayer.Extensions

open Fun.Blazor
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web.Virtualization
open System.Collections.Generic


let inline virtualize<'T>
  (
    items: ICollection<'T>,
    [<InlineIfLambda>] template: 'T -> NodeRenderFragment
  ) =
  html.blazor (fun ctx ->
    Virtualize(
      Items = items,
      ChildContent = RenderFragment<'T>(fun item -> ctx.Render(template item))
    ))
