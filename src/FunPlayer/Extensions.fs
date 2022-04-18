[<AutoOpen>]
module FunPlayer.Extensions

open Fun.Blazor
open Fun.Blazor.Operators
open Microsoft.AspNetCore.Components.Web.Virtualization

type VirtualizeBuilder<'FunBlazorGeneric, 'TItem when 'FunBlazorGeneric :> Microsoft.AspNetCore.Components.IComponent>
  () =
  inherit ComponentWithDomAndChildAttrBuilder<'FunBlazorGeneric>()

  [<CustomOperation("childContent")>]
  member inline _.childContent
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      fn: 'TItem -> NodeRenderFragment
    ) =
    render
    ==> html.renderFragment ("ChildContent", fn)

  [<CustomOperation("ItemContent")>]
  member inline _.ItemContent
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      fn: 'TItem -> NodeRenderFragment
    ) =
    render ==> html.renderFragment ("ItemContent", fn)

  [<CustomOperation("Placeholder")>]
  member inline _.Placeholder
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      fn: PlaceholderContext -> NodeRenderFragment
    ) =
    render ==> html.renderFragment ("Placeholder", fn)

  [<CustomOperation("ItemSize")>]
  member inline _.ItemSize
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      x: System.Single
    ) =
    render ==> ("ItemSize" => x)

  [<CustomOperation("ItemsProvider")>]
  member inline _.ItemsProvider
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      x: ItemsProviderDelegate<'TItem>
    ) =
    render ==> ("ItemsProvider" => x)

  [<CustomOperation("Items")>]
  member inline _.Items
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      x: System.Collections.Generic.ICollection<'TItem>
    ) =
    render ==> ("Items" => x)

  [<CustomOperation("OverscanCount")>]
  member inline _.OverscanCount
    (
      [<InlineIfLambda>] render: AttrRenderFragment,
      x: System.Int32
    ) =
    render ==> ("OverscanCount" => x)

type Virtualize'<'TItem>() =
  inherit VirtualizeBuilder<Virtualize<'TItem>, 'TItem>()
