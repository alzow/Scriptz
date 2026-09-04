# QueueApp — engineering standards

A .NET MAUI app (net9.0, MPowerKit.Navigation, PropertyChanged.Fody, Refit over Supabase
PostgREST/RPC). Everything below is a rule, not a suggestion. When you write or change code in this
repository, match it.

## 1. Where things live

```
QueueApp/
  Constants/                 app-wide constants only (navigation paths/keys, config)
  Converters/                IValueConverter implementations
  Features/<Feature>/        one folder per feature
    <Page>/                  one folder per page inside the feature
      <Page>Page.xaml(.cs)
      <Page>PageViewModel.cs
    Constants/               <Feature>Constants.cs — every literal the feature uses
    Helpers/                 <Feature>Helper.cs (statics) + ContentView partials for the feature
    Models/                  view-facing models owned by the feature
    Sheets/                  bottom sheets belonging to the feature
  Framework/                 base classes, navigation, theming, DI wiring
  Services/                  see §6
  Shared/                    cross-feature domain, helpers and templates
  Resources/Styles/          the only place a colour, font size or thickness may be defined
```

**A page lives in its own folder, named after itself.** The operator-hours page in business settings
is `Features/BusinessSettings/OperatorHours/OperatorHoursPage.xaml` +
`Features/BusinessSettings/OperatorHours/OperatorHoursPageViewModel.cs`. Not a shared `Settings`
bucket, not a flat file next to nine siblings.

**Folder name, namespace and type name agree.** A view in `Shared/Templates/AlzowLoadingButton`
is in namespace `QueueApp.Shared.Templates.AlzowLoadingButton`. Never leave a namespace behind after
renaming a folder.

## 2. View model layout

Members appear in exactly this order, top to bottom. No `#region` markers — the order *is* the
structure, and a region is a lid on a class that has outgrown itself.

```
1. Constants          private const / static readonly literals
2. Properties         bindable state (public get/set — Fody weaves the notification)
3. Fields             private mutable state
4. Services           private readonly injected dependencies
5. Constructor
6. Lifecycle          OnLoadedAsync, InitializeAsync, OnAppearingAsync, OnDisappearingAsync
7. Everything else    commands and methods
```

Inside "everything else", order by the flow a reader follows: the entry point first, then what it
calls. A method used by one caller sits under that caller.

### View model rules

- **Every method is `public`.** These are the surface a page and a test bind to; `private` on a view
  model method buys nothing and costs testability. Private is for *fields*.
- **`partial` where `[RelayCommand]` is used** — the CommunityToolkit generator needs it.
- **Properties are plain `public T Prop { get; set; }`.** PropertyChanged.Fody weaves the raise. Do
  not hand-write `SetProperty`, and do not add `[ObservableProperty]` backing fields.
- **A computed property that depends on a collection needs an explicit
  `OnPropertyChanged(nameof(X))`** after the collection is mutated — Fody cannot see through
  `ObservableCollection`.
- **A view model over ~400 lines is a defect.** Split the work out into collaborators (§4), not into
  more regions.

## 3. Error handling

- **Every method that can fail is wrapped.** Async methods `await HandleExceptionAsync(exception)`;
  synchronous ones `_ = HandleExceptionAsync(exception)`.
- A method that only reads its own already-validated state (a pure `switch` over an enum, a string
  format over non-null fields) does not need a wrapper. Anything touching a service, a collection
  built elsewhere, parsing, or platform APIs does.
- **`HandleExceptionAsync` itself must never throw.** It runs inside every other catch block.
- **Catch specifically where the recovery differs**: `ApiException` with
  `HttpStatusCode.Conflict` means someone took the slot, and says so; the general catch says
  "couldn't do that".
- **`finally` resets the flag you set.** `IsSubmitting`/`IsLoading` are turned off in `finally`,
  never at the end of the `try`.
- Swallowing is allowed only where the alternative is worse and the reason is written down (a
  best-effort name stamp on a booking that already succeeded). Log it.

## 4. Size and decomposition

| Artefact | Split when |
|---|---|
| View model | it passes ~400 lines, or serves two unrelated concerns |
| XAML page | it passes ~150 lines, or a block repeats |
| Service | it has two reasons to change |

Split a view model by **extracting a collaborator first**. A collaborator owns its own state and
has a name for what it does — `FlowScheduleLoader` owns the slot cache and the debounce,
`FlowIntakeCoordinator` owns the answer list, `FlowStepPresenter` computes the chrome and footer,
`FlowSubmissionCoordinator` owns the three ways a flow commits. The view model composes them.

Only once the logic is out may a view model that drives one screen's whole bound surface be spread
across partial files, named `<Type>.<Concern>.cs` (`FlowPageViewModelBase.Steps.cs`,
`.Schedule.cs`, `.Selection.cs`, `.Submit.cs`). That keeps the bound property names flat, so the
XAML does not change, while each file stays under a couple of hundred lines. A partial file that
is just a bag of leftovers is a region by another name — do not add one.

Split XAML by **extracting a `ContentView` with bindable properties only**. A helper view never
binds to a view model type and never reaches a parent `BindingContext`: everything it needs arrives
through `BindableProperty` declarations and is bound with `Source={x:Reference Root}`. That is what
makes it reusable and what stops a `x:DataType` from silently going stale.

## 5. Constants and helpers

- **Every literal that is not a one-off goes in a constants file.** Statuses, mode strings, copy,
  cache keys, tuning numbers, format strings. `Features/<Feature>/Constants/<Feature>Constants.cs`
  for feature-local ones; `QueueApp/Constants/` for app-wide ones; a `*Statuses`/`*Types` static
  class next to the model for values the API defines.
- **Static functions belong in a helper class**, not loose on a view model:
  `Features/<Feature>/Helpers/<Feature>Helper.cs`. If two features need the same one it moves to
  `Shared/Domain/` (domain logic) or `Framework/Extensions/` (general-purpose).
- Never copy a static helper between features. Move it up instead.

## 6. Services

```
Services/Api/<Area>/I<Area>Api.cs        Refit interface — the wire shape, nothing else
Services/Api/<Area>/I<Area>Service.cs    what the app asks for
Services/Api/<Area>/<Area>Service.cs     : BaseService — maps wire -> app
Services/Api/<Area>/Models/              requests and responses for that area
```

- **Services derive from `BaseService`** and put every call through `ExecuteApiCallAsync`, which
  logs the failure and rethrows so the view model's `HandleExceptionAsync` still owns the message.
- **A service never shows UI, never navigates, and never swallows.**
- **PostgREST RPC arguments are `p_`-prefixed** and go in a request record under `Models/`, never as
  an anonymous object at the call site.
- **Fetch in parallel.** Independent reads are started together and awaited with `Task.WhenAll`;
  awaiting three calls in sequence to build one screen is a bug, not a style choice.
- **Hand data forward instead of re-reading it.** When a page already has what the next page needs,
  pass a snapshot through navigation parameters (see `BusinessSnapshot`) rather than paying for the
  same round trips again.
- **Cache what cannot change while the screen is open**, keyed by everything that invalidates it,
  and clear the cache when a selection upstream of that key changes.
- Duplicated mapping between two services means the mapping belongs on the model (`X.From(response)`)
  or in `Shared/Domain/`.

## 7. XAML and styling

- **Only `StaticResource`.** No inline `TextColor`, `FontSize`, `FontFamily`, `BackgroundColor`,
  `Stroke`, `StrokeShape` or corner radius on a control. If the resource you need does not exist,
  add it to the right file in `Resources/Styles/` and use it.
  - `Colors.xaml` — palette, light/dark pairs
  - `LabelStyle.xaml` — `lbl<size>_<weight>_<role>`
  - `BorderStyle.xaml` — `brd<radius>_<role>`
  - `ButtonStyle.xaml`, `EntryStyle.xaml`, `ShapeStyle.xaml`, `FrameStyle.xaml`
  - Layout-only values (`Spacing`, `Padding`, `Grid` definitions) stay inline.
- **Theme through `AppThemeBinding`** inside the style, never at the usage site.
- **Text input is `QueueEntry`** (`Shared/Templates/QueueEntry`) — the ALZOW entry. It carries the
  border, the focus and error states, the clear/reveal buttons, the left icon and the validator
  hook. A bare `<Entry>` in a page or helper view is wrong; it will not match the app and it will
  not show validation. Use `Editor` inside `brd12_Cream` only for genuinely multi-line free text.
- **Buttons are `AlzowLoadingButton`** with `ButtonStyle` set to one of the shared button styles.
- **Sub-page headers are `AlzowSubPageHeader`.**
- `x:DataType` on every `DataTemplate` and page — compiled bindings are not optional.

## 8. Comments

Delete them. The exceptions, and there are only two:

1. `// TODO:` — a real, actionable, still-outstanding item.
2. A short note on genuinely non-obvious machinery: a realtime subscription's lifetime, a database
   rule the code has to mirror, a platform quirk with no readable workaround, a race and how it is
   closed.

A comment that restates the code, narrates what a method does, or labels a section is noise and gets
removed on sight. Name the method well instead.

## 9. Naming

| Thing | Pattern | Example |
|---|---|---|
| Page | `<Name>Page` | `OperatorHoursPage` |
| View model | `<Name>PageViewModel` | `OperatorHoursPageViewModel` |
| Helper view | `<Name>View` | `FlowFooterBarView` |
| Bottom sheet | `<Name>Sheet` | `BlockTimeSheet` |
| Shared template | `Alzow<Name>` | `AlzowLoadingButton` |
| Feature constants | `<Feature>Constants` | `FlowConstants` |
| Feature helper | `<Feature>Helper` | `FlowHelper` |
| Service | `<Area>Service` / `I<Area>Service` | `BookingService` |
| Refit client | `I<Area>Api` | `IBookingApi` |

## 10. Checklist before you finish

- [ ] New page in its own folder, named `<Page>Page` + `<Page>PageViewModel`
- [ ] View model members in the §2 order, no regions, all methods public
- [ ] No literal that belongs in a constants file
- [ ] No static function loose on a view model
- [ ] Try/catch on everything that can fail; flags reset in `finally`
- [ ] No comments except TODOs and the two exceptions in §8
- [ ] XAML uses only static resources; new ones added to `Resources/Styles/`
- [ ] Text input uses `QueueEntry`, buttons use `AlzowLoadingButton`
- [ ] Independent service calls run under `Task.WhenAll`
- [ ] Nothing re-fetched that the previous screen could hand over
