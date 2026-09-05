# Push notifications

Status: working end to end on Android (verified: a real push was delivered to a real device via
the live webhook path). iOS is not implemented — see §9.

## 1. Overview

A database event a user should be told about — their turn is next, a booking changed, an item is
ready for collection — writes a row to a `notifications` table. A database trigger fires, and a
Supabase Database Webhook (Integrations → Webhooks) calls the `send-push` Edge Function with that
row. The function looks up every device the user is currently registered on, gets a fresh FCM
OAuth2 access token, and sends one HTTP v1 message per device. Firebase delivers it to the device
even if the app is closed.

```
 event on some table (queue_entries, bookings, ...)
          │
          ▼
   trigger function ──► create_notification(...) ──► INSERT INTO notifications
          │                                                    │
          │                                     Database Webhook (on INSERT)
          │                                                    │
          │                                                    ▼
          │                                     supabase/functions/send-push
          │                                       ├─ SELECT device_tokens WHERE user_id = ...
          │                                       ├─ OAuth2 JWT → Google → access token (cached)
          │                                       ├─ POST fcm.googleapis.com/.../messages:send
          │                                       │     (once per device_token row)
          │                                       ├─ INSERT notification_deliveries (per send)
          │                                       └─ DELETE dead device_tokens (UNREGISTERED etc.)
          │                                                    │
          ▼                                                    ▼
  in-app live update                              FCM → device (closed or open)
  (Supabase Realtime,                                          │
   see §2)                                                     ▼
                                            Plugin.Firebase.CloudMessaging
                                            (background/killed: system tray automatically;
                                             foreground: plugin raises a local notification)
                                                                 │
                                                                 ▼
                                                    tap → PushNotificationRouter
                                                    (Manage tab / VisitPage — see §5b)
```

The database objects (`notifications`, `device_tokens`, `notification_deliveries`,
`create_notification`, the trigger functions, and the two RPCs) are **not version-controlled in
this repository** — they exist only in the live Supabase project. This is a gap, not a design
choice; see §4 and §10.

## 2. Why not Supabase Realtime

Realtime (used elsewhere in the app for in-app live updates, e.g. queue position) only delivers to
a socket the app currently has open. It cannot wake a closed app or notify a phone sitting in
someone's pocket. Queue's core premise is a customer waiting at home rather than standing in line,
so a delivery path that works with the app closed was mandatory — that's what FCM provides.

Realtime and push are not a replacement for each other; they coexist and serve different states of
the app (open vs. closed/backgrounded).

## 3. Components

| Component | File / location | Responsibility |
|---|---|---|
| `notifications` table | live Supabase project only | Outbox row: one per notification-worthy event |
| `device_tokens` table | live Supabase project only | One row per (user, device): current FCM token |
| `notification_deliveries` table | live Supabase project only | Per-device send attempt log |
| `create_notification` | live Supabase project only | Helper that inserts a `notifications` row |
| Trigger functions | live Supabase project only | Call `create_notification` on the events that should notify |
| `upsert_device_token` RPC | live Supabase project only | Called by the client to register/refresh a token |
| `remove_device_token` RPC | live Supabase project only | Called by the client to deregister on logout |
| Database Webhook | Supabase dashboard → Integrations → Webhooks | Fires `send-push` on `notifications` INSERT |
| Edge Function | [`supabase/functions/send-push/index.ts`](../supabase/functions/send-push/index.ts) | OAuth2, fan-out send, delivery logging, stale-token cleanup |
| `IDeviceTokenApi` | [`QueueApp/Services/Api/Auth/IDeviceTokenApi.cs`](../QueueApp/Services/Api/Auth/IDeviceTokenApi.cs) | Refit client for the two RPCs |
| `IPushRegistrationService` / `PushRegistrationService` | [`QueueApp/Services/Auth/PushRegistrationService.cs`](../QueueApp/Services/Auth/PushRegistrationService.cs) | Permission request, token acquisition, upsert/remove calls |
| `IDeviceIdentityService` / `DeviceIdentityService` | [`QueueApp/Services/Auth/DeviceIdentityService.cs`](../QueueApp/Services/Auth/DeviceIdentityService.cs) | Client-generated device id, persisted in secure storage |
| `AuthService` | [`QueueApp/Services/Auth/AuthService.cs`](../QueueApp/Services/Auth/AuthService.cs) | Calls `RegisterAsync`/`UnregisterAsync` at the right points in the session lifecycle |
| Firebase init | [`QueueApp/MauiProgram.cs`](../QueueApp/MauiProgram.cs) | `CrossFirebase.Initialize`, `TokenChanged` wiring |
| Notification channel | [`QueueApp/Platforms/Android/MainActivity.cs`](../QueueApp/Platforms/Android/MainActivity.cs), [`QueueApp/Constants/NotificationChannels.cs`](../QueueApp/Constants/NotificationChannels.cs) | Creates the `queue_updates` Android notification channel |
| `google-services.json` | [`QueueApp/Platforms/Android/google-services.json`](../QueueApp/Platforms/Android/google-services.json) | Firebase Android app config |
| `PushNotificationRoute` | [`QueueApp/Services/Notifications/PushNotificationRoute.cs`](../QueueApp/Services/Notifications/PushNotificationRoute.cs) | Turns an `action` + `action_params` payload into a destination |
| `IPushNotificationRouter` / `PushNotificationRouter` | [`QueueApp/Services/Notifications/PushNotificationRouter.cs`](../QueueApp/Services/Notifications/PushNotificationRouter.cs) | Holds a tap until the tabs exist, then routes it |
| Tap receivers | [`QueueApp/Features/Main/MainTabbedPageViewModel.cs`](../QueueApp/Features/Main/MainTabbedPageViewModel.cs) | Selects the Manage tab, or pushes `VisitPage` over the tabs |

## 4. Database layer

**Not in this repository.** No SQL, migration, or schema file for `notifications`,
`device_tokens`, `notification_deliveries`, `create_notification`, the trigger functions, or the
two RPCs exists anywhere in the repo — only the Edge Function that consumes them
([`send-push/index.ts`](../supabase/functions/send-push/index.ts)) and
[`QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md`](../QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md),
which predates this feature and doesn't mention these tables. Everything below is inferred from
how the Edge Function and client code use these objects, not read from a definition.

**TODO: verify** — the actual column lists, constraints, and trigger definitions all need
exporting from the live database (see §10). What follows is what the code proves must be true,
not a schema dump.

- **`device_tokens`** — the Edge Function selects `id, fcm_token` filtered `.eq("user_id", userId)`
  ([`index.ts:183-186`](../supabase/functions/send-push/index.ts#L183-L186)) and deletes rows by
  `id` when FCM reports a dead token
  ([`index.ts:249-256`](../supabase/functions/send-push/index.ts#L249-L256)). The client's
  `upsert_device_token` RPC is called with `p_device_id`, `p_fcm_token`, `p_platform`
  ([`UpsertDeviceTokenRequest.cs`](../QueueApp/Services/Api/Auth/Models/UpsertDeviceTokenRequest.cs)).
  Design intent (unverified against a live schema): the client-generated `device_id` is the upsert
  key, so a token refresh on the same device updates the existing row instead of inserting a
  duplicate — a user with several devices then has several rows, and a rotated token on one device
  doesn't create a new row for that device.
- **`notifications`** — the outbox row. The Edge Function reads `id`, `user_id`, `title`, `body`,
  `action`, `action_params` off it (or off a webhook envelope's `.record`)
  ([`index.ts:160-169`](../supabase/functions/send-push/index.ts#L160-L169)). It rejects a payload
  missing `user_id`, `title`, or `body` with a 400
  ([`index.ts:171-176`](../supabase/functions/send-push/index.ts#L171-L176)).
- **`notification_deliveries`** — one row inserted per (notification, device) send attempt, with
  `status` one of `sent` / `unregistered` / `failed`, an `error` message, and `sent_at`
  ([`index.ts:226-238`](../supabase/functions/send-push/index.ts#L226-L238)). `device_token_id` is
  stated in a code comment to be `ON DELETE SET NULL`
  ([`index.ts:250`](../supabase/functions/send-push/index.ts#L250)) — **TODO: verify** this
  constraint against the live schema; the comment is the only evidence in this repo.
- **`create_notification`** — not present in this repo in any form (no SQL, no reference in
  application code). Its existence, its null-`user_id` early-return behaviour for walk-in queue
  entries, and its calling convention are asserted by the task background but **could not be
  verified against any file** — TODO: verify directly against the live database.
- **Trigger functions** that call `create_notification` on relevant events — **not in this repo**.
  TODO: verify which tables/events currently have a trigger attached, and what `action` /
  `action_params` each one sets.
- **RPCs are used instead of direct PostgREST table writes** for both `upsert_device_token` and
  `remove_device_token` — confirmed by the client only calling
  `[Post("/rpc/upsert_device_token")]` / `[Post("/rpc/remove_device_token")]`
  ([`IDeviceTokenApi.cs`](../QueueApp/Services/Api/Auth/IDeviceTokenApi.cs)), never a direct table
  endpoint. This matches the RLS pattern documented elsewhere in the repo
  ([`SUPABASE-SCHEMA-VERIFIED.md`](../QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md)) of using
  `security definer` RPCs so `auth.uid()` is taken server-side rather than trusted from the client.

**Schema facts noted during the build (unverified in this repo, carried forward as context for
whoever writes the trigger logic):**

- `QueueEntryStatuses` in this repo defines `Done = "done"`
  ([`QueueEntryStatuses.cs:10`](../QueueApp/Services/Api/Queue/Models/QueueEntryStatuses.cs#L10))
  as a terminal status — there is no `"completed"` value defined for queue entries.
- `BookingStatuses` defines **both** `Completed = "completed"`
  ([`BookingStatuses.cs:14`](../QueueApp/Services/Api/Booking/Models/BookingStatuses.cs#L14)) and,
  per its own header comment, the live `booking_status` enum's actual labels were never confirmed
  by schema verification — the file states `InProgress` and `NoShow` may not exist server-side yet.
  Which of `done` / `completed` is the one actually returned by the live `booking_status` enum is
  unresolved — TODO: verify before writing a trigger on `bookings`.
- `queue_entries` has no `position` column in the documented schema
  ([`SUPABASE-SCHEMA-VERIFIED.md`](../QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md)) —
  position is computed (`v_queue_positions`, `my_queue_status`). A "you're next" trigger has to
  derive that state (e.g. find the earliest-joined remaining `waiting` entry when one leaves),
  not read a stored value.
- `customer_id` and `operator_id` are nullable on several tables per
  [`SUPABASE-SCHEMA-VERIFIED.md`](../QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md) — a
  trigger calling `create_notification` must handle a null `user_id` without erroring, e.g. for
  walk-in entries with a `customer_name` but no `customer_id`.
- FK references throughout the schema point at `profiles(id)`, not `auth.users`, per the same
  document.
- `rls_auto_enable()` is a documented schema-wide event trigger that turns RLS on for any new
  table automatically ([`SUPABASE-SCHEMA-VERIFIED.md:69`](../QueueApp/Documentation/SUPABASE-SCHEMA-VERIFIED.md#L69)) —
  relevant if `notifications` / `device_tokens` / `notification_deliveries` were created after that
  trigger existed, since RLS would already be on for them without an explicit `ENABLE ROW LEVEL
  SECURITY` statement.

## 5. Client layer (Android)

**Firebase init** — [`MauiProgram.cs`](../QueueApp/MauiProgram.cs). Registered through a
`RegisterFirebaseServices` extension called from `CreateMauiApp()`:

```csharp
events.AddAndroid(android => android.OnCreate((activity, _) =>
    Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(
        activity,
        () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)));
```

inside `builder.ConfigureLifecycleEvents(...)`. After `builder.Build()`, the app subscribes to
token refreshes:

```csharp
var pushRegistration = app.Services.GetRequiredService<Services.Auth.IPushRegistrationService>();
Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.TokenChanged += pushRegistration.OnTokenRefreshed;
```

**Permission handling and token acquisition** —
[`PushRegistrationService.RegisterAsync`](../QueueApp/Services/Auth/PushRegistrationService.cs#L21):
requests `Permissions.PostNotifications`, returns early if not granted, calls
`CrossFirebaseCloudMessaging.Current.CheckIfValidAsync()` (throws on failure rather than returning
a status — the surrounding `try/catch` is what handles that), then
`CrossFirebaseCloudMessaging.Current.GetTokenAsync()`, and if a token comes back, upserts it.

**Device id** —
[`DeviceIdentityService.GetDeviceIdAsync`](../QueueApp/Services/Auth/DeviceIdentityService.cs#L14):
a `Guid.NewGuid().ToString("N")` generated once and persisted via `ISecureStorageService` under
the key `"queue_device_id"` — not a hardware identifier.

**Refit client** —
[`IDeviceTokenApi`](../QueueApp/Services/Api/Auth/IDeviceTokenApi.cs):

```csharp
public interface IDeviceTokenApi
{
    [Post("/rpc/upsert_device_token")]
    Task UpsertAsync([Body] UpsertDeviceTokenRequest request);

    [Post("/rpc/remove_device_token")]
    Task RemoveAsync([Body] RemoveDeviceTokenRequest request);
}
```

Registered in [`RefitConfiguration.cs`](../QueueApp/Services/Api/RefitConfiguration.cs) against
`SupabaseConfig.RestUrl` (`https://<project>.supabase.co/rest/v1`) with `SupabaseAuthHeaderHandler`
attached — the same handler every other authenticated Refit client uses, so the RPC carries the
signed-in user's bearer token and `auth.uid()` resolves server-side.

**Where registration is invoked from** —
[`AuthService.cs`](../QueueApp/Services/Auth/AuthService.cs), not from any view model:

- `PersistSessionAsync` (private, called by both `SignInAsync` and `SignUpAsync` after the session
  tokens are stored) fires `_ = _pushRegistration.RegisterAsync();` — covers login and
  registration in one place.
- `EnsureValidSessionAsync` (the splash-screen session-restore check) fires the same call when the
  session is confirmed valid — covers a session restored on app start, and re-registers on every
  splash check in case the token rotated since the last launch.
- `ClearSessionAsync` awaits `_pushRegistration.UnregisterAsync()` **before** clearing the stored
  session — covers logout. The ordering is load-bearing: `remove_device_token` depends on
  `auth.uid()`, so running it after the tokens are cleared would silently delete nothing.

Registration is fire-and-forget (`_ = ...RegisterAsync()`) in both call sites so a slow permission
prompt or token fetch never stalls sign-in or app start. `RegisterAsync` has its own top-level
`try/catch` and never throws out to the caller. Unregistration is awaited, since it's expected to
be fast and completing it matters for the shared-device case (see §8).

**Notification channel** — created in
[`MainActivity.OnCreate`](../QueueApp/Platforms/Android/MainActivity.cs#L12), using the id/name/
description constants in
[`Constants/NotificationChannels.cs`](../QueueApp/Constants/NotificationChannels.cs). `High`
importance, public lockscreen visibility, vibration and badge enabled. Guarded by
`Build.VERSION.SdkInt < BuildVersionCodes.O` since channels are an API 26+ concept. See §8 for why
this exists and why it can't be changed later.

## 5b. Tap routing

Tapping a notification opens the screen the notification is about. This is client-only work — the
Edge Function already forwards `action` and `action_params` untouched (§6), and nothing on the
server needs to know that the client now acts on them.

**What the payload has to carry.** The FCM `data` payload's `action` names the destination and
`action_params` carries the record id as a JSON *string* (FCM data values are always strings):

```jsonc
{
  "action": "queue_status",
  "action_params": "{\"entry_id\":\"c43081e1-723c-4f5c-a7ab-55b90cf7e73d\",\"business_id\":\"...\",\"operator_id\":\"...\"}"
}
```

| `action` | Opens | Needs |
|---|---|---|
| `operator_queue` | the Manage tab, on the queue board | nothing |
| `operator_bookings` | the Manage tab, on the bookings agenda | nothing |
| `queue_status` | `VisitPage` for that queue entry | `entry_id` |
| `booking_detail` | `VisitPage` for that booking | `booking_id` |

An unknown `action`, or a visit action with no usable id, routes nowhere — the app just opens
where it would have anyway. The ids are read out of `action_params` first and off the top level of
`data` as a fallback, so a trigger that flattens them instead of nesting them still works.

The manage tab is picked from the `action` rather than from another read of the owned business: a
shop only gets `operator_bookings` in booking mode and `operator_queue` in queue mode, so the
action already says which board exists. `business_id` and `operator_id` are ignored — the Manage
tab is the signed-in owner's own business either way.

**The pieces.**

- [`Constants/PushNotificationActions.cs`](../QueueApp/Constants/PushNotificationActions.cs) and
  [`PushNotificationKeys.cs`](../QueueApp/Constants/PushNotificationKeys.cs) — the four action
  strings and the payload keys. A value added here has to match what the trigger writes exactly.
- [`Services/Notifications/PushNotificationRoute.cs`](../QueueApp/Services/Notifications/PushNotificationRoute.cs)
  — `From(data)` turns the payload into a route, or `null`. All the parsing lives here; it takes no
  platform types, so it is the piece worth testing.
- [`Services/Notifications/PushNotificationRouter.cs`](../QueueApp/Services/Notifications/PushNotificationRouter.cs)
  — a singleton that holds a tap until there is somewhere to put it, then sends a `SelectTabMessage`
  or an `OpenVisitMessage`.
- [`Features/Main/MainTabbedPageViewModel.cs`](../QueueApp/Features/Main/MainTabbedPageViewModel.cs)
  — receives both messages. `VisitPage` is pushed modally over the tabs with the same parameters
  the History row uses (`entryId`/`bookingId` + `openedFromTabs`), so the page behaves identically
  however it was opened, back chevron included.

**Why the router holds the tap.** On a cold start the tap is what launched the app, so it arrives
while the splash is still checking the session. Routing it there would be pointless: the splash
finishes with an *absolute* navigation that builds the tabs, and that replaces whatever was
pushed. So `PushNotificationRouter` keeps the route and both
[`QueueSplashPageViewModel`](../QueueApp/Features/QueueSplash/QueueSplashPageViewModel.cs) and
[`LoginPageViewModel`](../QueueApp/Features/Auth/LoginPageViewModel.cs) call `NotifyTabsReady()`
immediately after they land on the tabs. A tap arriving after that point is routed straight away.
Login is in that list because a tap while signed out lands on sign-in first — the tap survives it
and opens once there is a session. The splash also treats a held tap as a reason to skip the
welcome carousel, for the same reason a deep link would.

Anything sitting over the tabs is dismissed before the route is applied
(`MainTabbedNavigation.DismissAnythingOverTheTabsAsync`). Without it a tap taken while a visit was
already open would bury the one that was tapped under the one that was already there.

**Android wiring.** Two calls in
[`MainActivity`](../QueueApp/Platforms/Android/MainActivity.cs), both required — the plugin reads
the tap off the launching intent and raises nothing without them:

```csharp
protected override void OnCreate(Bundle? savedInstanceState)      // app was killed
    => FirebaseCloudMessagingImplementation.OnNewIntent(Intent);

protected override void OnNewIntent(Intent? intent)               // app was backgrounded
    => FirebaseCloudMessagingImplementation.OnNewIntent(intent);
```

`OnNewIntent` reaching the activity at all depends on `LaunchMode.SingleTop`, which the activity
already declares. The plugin holds a tap that arrives before anything has subscribed and replays it
to the first subscriber, so the `OnCreate` call landing before
[`MauiProgram`](../QueueApp/MauiProgram.cs) subscribes is not a race.

`MainActivity` also sets `FirebaseCloudMessagingImplementation.ChannelId` to the app's own channel
id. That is what a *foreground* push needs: Android never shows one itself, the plugin raises a
local notification instead, and left unset it builds that against a null channel and throws — which
is what made foreground pushes silent (§8).

**iOS.** The subscription in `MauiProgram` is deliberately `#if ANDROID || IOS`, not Android-only.
The plugin raises the same `NotificationTapped` event with the same `FCMNotification.Data` on iOS
(from `UNUserNotificationCenterDelegate.DidReceiveNotificationResponse`), and it replays a missed
tap the same way — so none of the routing above is Android-specific and none of it needs changing
for iOS. What iOS still needs is the Firebase/APNs setup and `FirebaseCloudMessagingImplementation.Initialize()`
in the AppDelegate, all of it listed in §9.

## 6. Edge Function

[`supabase/functions/send-push/index.ts`](../supabase/functions/send-push/index.ts).

**OAuth2 flow** — FCM's HTTP v1 API requires a Google OAuth2 access token, not a static server
key. `getAccessToken()` builds a JWT (`RS256`, signed with the service account's private key from
the `FCM_SERVICE_ACCOUNT` environment variable), POSTs it to the service account's `token_uri`
with grant type `urn:ietf:params:oauth:grant-type:jwt-bearer`, and gets back an access token.

**Token caching** — the resulting token is cached in a module-scope variable
(`cachedToken: { value, expiresAt }`) and reused across invocations while the function instance
stays warm, refreshed a minute before its stated expiry
([`index.ts:51-57`](../supabase/functions/send-push/index.ts#L51-L57)) rather than re-minted on
every call.

**The send loop** — for each row in `device_tokens` matching the notification's `user_id`, POSTs
one message to `https://fcm.googleapis.com/v1/projects/<project_id>/messages:send`
([`index.ts:108-148`](../supabase/functions/send-push/index.ts#L108-L148)) with:

```jsonc
{
  "message": {
    "token": "<fcm token>",
    "notification": { "title": "...", "body": "..." },
    "data": { "action": "...", "action_params": "<json string>", "notification_id": "..." },
    "android": {
      "priority": "high",
      "notification": { "channel_id": "queue_updates" }
    }
  }
}
```

Note `channel_id: "queue_updates"` here matches
[`NotificationChannels.QueueUpdatesId`](../QueueApp/Constants/NotificationChannels.cs) on the
client exactly — this pairing is what makes the client-created channel take effect instead of the
FCM SDK falling back to its own default channel.

A notification with no devices registered (`tokens.length === 0`) is treated as a success, not an
error — returns `{ sent: 0, reason: "no devices registered" }` with a 200.

**Delivery logging** — one `notification_deliveries` row per send attempt, `status` set to `sent`,
`unregistered`, or `failed` depending on outcome
([`index.ts:226-238`](../supabase/functions/send-push/index.ts#L226-L238)). A failure to write the
delivery log is caught and logged but does not fail the request — the comment at
[`index.ts:245`](../supabase/functions/send-push/index.ts#L245) notes the push has already gone
out by that point, so failing the whole request over a logging error would be wrong.

**Stale token cleanup** — a response is classed dead
(`res.status === 404 || text.includes("UNREGISTERED") || text.includes("INVALID_ARGUMENT")`,
[`index.ts:143-145`](../supabase/functions/send-push/index.ts#L143-L145)) and its `device_tokens`
row deleted. Any other failure is logged as a delivery row with `status: "failed"` but the token is
left alone, on the basis that it might be a transient failure rather than proof the token is dead.

## 7. Adding a new notification type

1. Identify the database event that should trigger it (an `INSERT`/`UPDATE` on some table).
2. Write or extend the relevant trigger function so it calls `create_notification` with the target
   `user_id`, a `title`, a `body`, and an `action` + `action_params` pair describing what the
   client should do when the notification is tapped (e.g. navigate to a specific queue entry or
   booking). **This step happens directly against the live Supabase project** — there is currently
   no migration file in this repo to add it to (see §10).
3. Confirm the trigger correctly handles a null `user_id` (walk-in entries, etc.) — it should skip
   creating a notification rather than erroring.
4. No client-side change is needed to *send* the notification — `send-push` is generic and reads
   `title`/`body`/`action`/`action_params` off whatever row triggers it.
5. If the new `action` is one of the four §5b already routes, there is nothing to do on the client
   — send the matching `action_params` and the tap opens the right screen. A genuinely new
   destination means a constant in
   [`PushNotificationActions`](../QueueApp/Constants/PushNotificationActions.cs), an arm in
   [`PushNotificationRoute.From`](../QueueApp/Services/Notifications/PushNotificationRoute.cs), and
   whatever the router needs to send for it.
6. Test by inserting a row into `notifications` directly (bypassing the trigger) or via
   `curl` against the deployed `send-push` function with a bare notification-shaped JSON body —
   the function accepts either a webhook envelope (`{ record: {...} }`) or a bare row
   ([`index.ts:158-160`](../supabase/functions/send-push/index.ts#L158-L160)), which is exactly
   what makes this manual test path possible.

## 8. Gotchas

**Plugin.Firebase v4 API surface.** Several plausible APIs do not exist in this version:
- No `UseFirebaseCloudMessaging()` builder extension — confirmed absent from this codebase; init
  goes through `CrossFirebase.Initialize` inside `ConfigureLifecycleEvents` instead (§5).
- The v4 `CrossFirebase.Initialize` signature takes **two** arguments — the activity and an
  `ActivityLocator` func — confirmed in [`MauiProgram.cs`](../QueueApp/MauiProgram.cs#L79-L81).
- `CrossFirebaseCloudMessaging.Current.CheckIfValidAsync()` returns `Task`, not `Task<bool>` —
  confirmed at [`PushRegistrationService.cs:32`](../QueueApp/Services/Auth/PushRegistrationService.cs#L32),
  where the result is not assigned to anything; failure surfaces as a thrown exception caught by
  the surrounding `try/catch`.
- No `RequestNotificationPermissionAsync()` method exists on the plugin; the app uses MAUI's own
  `Permissions.RequestAsync<Permissions.PostNotifications>()`
  ([`PushRegistrationService.cs:27`](../QueueApp/Services/Auth/PushRegistrationService.cs#L27)).
- v4 requires Android **minSdk 23**. **Discrepancy from the build's stated intent**: as of this
  writing, `QueueApp.csproj` still sets
  `<SupportedOSPlatformVersion ... 'android'>21.0</SupportedOSPlatformVersion>`. This was flagged
  during the fix pass as needing a bump to 23 but that change has not been applied in the current
  code — see §10.

**Android 13+ runtime permission.** The manifest's `POST_NOTIFICATIONS` entry alone is
insufficient — without the runtime `Permissions.RequestAsync` call, notifications never appear on
Android 13+, with no error surfaced anywhere.

**Notification channels.** A missing channel does not silently drop the notification — FCM falls
back to the manifest's `default_notification_channel_id` meta-data if set, then to its own
`fcm_fallback_notification_channel`. But the fallback's importance/sound are not under app control
and its name in Android's notification settings is generic. The app creates its own channel in
[`MainActivity.OnCreate`](../QueueApp/Platforms/Android/MainActivity.cs#L26) with `High` importance
specifically to avoid this. **Channel settings are immutable once created on a device** — changing
`NotificationImportance` in code has no effect on installs where the channel already exists;
changing it later requires a new channel id, leaving the old one visible but orphaned in the
user's settings.

**Foreground vs. background.** Android's system tray auto-displays an FCM notification when the
app is backgrounded or killed. When the app is in the **foreground** it does not: the message goes
to the app instead. The plugin ships its own `FirebaseMessagingService`, so that message already
reaches `CrossFirebaseCloudMessaging.Current.OnNotificationReceived`, which raises a *local*
notification on `FirebaseCloudMessagingImplementation.ChannelId`. That static was never set, so the
plugin was building a notification against a null channel, throwing, and swallowing it — which is
why foreground pushes appeared to have no handler at all. `MainActivity.OnCreate` now sets it to
the app's own `queue_updates` channel (§5b), so a foreground push shows and is tappable like any
other. There is still no *in-app* banner or toast — the notification is a system one either way.

**Refit route prefix.** The Refit client base address (`SupabaseConfig.RestUrl`) already includes
`/rest/v1`. Route attributes must be relative to that (`[Post("/rpc/name")]`) — repeating the
prefix in the attribute produces a doubled `/rest/v1/rest/v1/...` path and a 404. Confirmed correct
in the current [`IDeviceTokenApi.cs`](../QueueApp/Services/Api/Auth/IDeviceTokenApi.cs) — it uses
`/rpc/upsert_device_token` and `/rpc/remove_device_token`, following the same convention as every
other Refit interface in the app (e.g. `IQueueApi`).

**Call site ordering.** `RegisterAsync` must run after the session's tokens are stored, since
`upsert_device_token` relies on `auth.uid()`; it must also cover both login and registration, not
just one. `UnregisterAsync` must run before the session is cleared, since `remove_device_token`
also relies on `auth.uid()` — running it after clearing the session means the delete silently
matches nothing. Both are true in the current `AuthService` (§5). Registration is fire-and-forget
specifically so a slow token fetch never stalls the login/registration flow the user is waiting on.

**FCM HTTP v1 uses OAuth2, not a static server key.** The Edge Function signs and exchanges a JWT
for an access token rather than using a legacy server key — see §6.

**Stale token cleanup.** FCM's `UNREGISTERED` / `NOT_FOUND` (via HTTP 404) / `INVALID_ARGUMENT`
responses are treated as proof the token is dead and the corresponding `device_tokens` row is
deleted; other failures are logged but the token is left in place. `notification_deliveries` rows
referencing a deleted token are said (in a code comment, not verified against the live schema) to
have `device_token_id` set to `NULL` via `ON DELETE SET NULL`, so delivery history is not lost when
a token is cleaned up.

**Database Webhooks live under Integrations, not Database**, in the current Supabase dashboard.
The integration must be explicitly enabled before a webhook can be created — enabling it is what
creates the `supabase_functions` schema. A `schema "supabase_functions" does not exist` error means
the integration has not been enabled yet, not that something is broken.

### Known legacy issue — documented, not fixed

`ApplicationId` in [`QueueApp.csproj`](../QueueApp/QueueApp.csproj) is
`za.co.alzow.alzowmauicomponents`, inherited from the Alzow/Scriptz fork this project began from.
It matches `google-services.json`'s `package_name` exactly, so push registration and everything
else currently works. However:

- It is registered in Firebase, where the Android package name **cannot be changed** after the
  fact.
- It would become the permanent Google Play Store package name once published — there is no
  migration path for installed users if it changes after release.
- It is visible to anyone who inspects the installed APK.

This is a decision for the project owner, not a bug to silently fix — changing it requires
registering a new Android app in the Firebase console, downloading a fresh `google-services.json`,
and updating the manifest, and is only realistically possible before the app is published.

## 9. iOS — not yet implemented

Blocked on an Apple Developer Program membership. The server side
(`notifications`/`device_tokens`/`notification_deliveries`, the triggers, and `send-push`) is
entirely platform-agnostic — one FCM call reaches both Android and iOS devices — so **no database
or Edge Function change is needed for iOS**. Tap routing (§5b) is platform-agnostic for the same
reason: it is subscribed under `#if ANDROID || IOS` and reads the same `FCMNotification.Data` the
plugin raises on both platforms, so nothing in it needs writing again. The remaining work is
entirely client config and Firebase/Apple console setup.

**Apple Developer portal**
1. Enrol in the Apple Developer Program.
2. Certificates, Identifiers & Profiles → create an App ID matching the iOS bundle identifier, with
   the Push Notifications capability enabled.
3. Keys → create an APNs Auth Key, download the `.p8` file. It can only be downloaded once — store
   it alongside the Firebase service account key.
4. Note the Key ID and Team ID.
5. Create a provisioning profile with Push Notifications enabled.

**Firebase console**
6. Add an iOS app to the existing Firebase project using the bundle identifier.
7. Cloud Messaging tab → upload the `.p8` with its Key ID and Team ID, so FCM can forward to APNs.
8. Download `GoogleService-Info.plist`.

**MAUI project**
9. Add `GoogleService-Info.plist` to the iOS platform folder with Build Action `BundleResource`.
10. Add a `<BundleResource>` `ItemGroup` to `QueueApp.csproj` scoped to the iOS target framework.
11. Add `aps-environment` to `Entitlements.plist` (`development` for debug builds, `production` for
    release).
12. Extend the Firebase lifecycle wiring in [`MauiProgram.cs`](../QueueApp/MauiProgram.cs) with an
    iOS branch — `events.AddiOS(iOS => iOS.WillFinishLaunching(...))`. The iOS branch's signature
    differs from the Android `OnCreate` one currently there.
13. Widen the `Plugin.Firebase.CloudMessaging` package condition to include iOS. **Note**: as of
    this writing the package reference in `QueueApp.csproj` is still unconditional (applies to all
    target frameworks including iOS/MacCatalyst/Windows) — this step has not been done in either
    direction (neither scoped to Android-only nor deliberately widened to iOS). See §10.
14. Request notification authorisation via `UNUserNotificationCenter` and register for remote
    notifications.
15. `PushRegistrationService.SendAsync` already derives platform from `DeviceInfo.Platform`:
    ```csharp
    var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
    ```
    ([`PushRegistrationService.cs:82`](../QueueApp/Services/Auth/PushRegistrationService.cs#L82)) —
    confirmed in the current code. This line likely needs no change for iOS support.

**Testing constraints**
16. Push cannot be tested on the iOS simulator — a physical device is required.
17. Launch without the debugger attached; notifications may not arrive while debugging.
18. `aps-environment` must be `production` for App Store/TestFlight builds, or pushes silently fail
    in release.

**Also note:** the `queue_updates` notification channel (§5, §8) is an Android-only concept; the
`android` block in the FCM message payload
([`send-push/index.ts:129-133`](../supabase/functions/send-push/index.ts#L129-L133)) is ignored by
iOS devices entirely. If iOS-specific delivery options are wanted later (badge counts,
`interruption-level`, critical alerts), an `apns` block would be added alongside the existing
`android` block in the same Edge Function payload.

## 10. Open items

- **No SQL/migration file exists in this repo for any push-related database object.**
  `notifications`, `device_tokens`, `notification_deliveries`, `create_notification`, the trigger
  functions, and both RPCs exist only in the live Supabase project. This blocks reviewing them in
  a PR, reproducing the schema in a fresh environment, and confirming the Refit DTO parameter names
  (`p_device_id`, `p_fcm_token`, `p_platform`) actually match the live RPC signatures. Exporting
  these definitions into `supabase/` (migrations or a `functions`-adjacent SQL folder, matching
  whatever the project settles on) is unresolved.
- **No *in-app* foreground presentation.** A push arriving while the app is open now shows as a
  system notification (§8) and routes on tap like any other, but there is no in-app banner or toast
  and no live badge. Whether that's worth building is undecided.
- **Tap routing is untested on a device.** §5b is written and wired but has not been run against a
  real push on real hardware, on either platform; nor has the foreground channel fix. The four
  actions and their `action_params` shapes also still need confirming against what the live
  triggers actually write (§4).
- **Discrepancy — Android `SupportedOSPlatformVersion` is still 21.0**, not the 23 that Plugin.
  Firebase v4 requires. This was identified as a required fix during the build but has not been
  applied in the code as of this document.
- **Discrepancy — `Plugin.Firebase.CloudMessaging` package reference is still unconditional**
  (applies to iOS/MacCatalyst/Windows targets too), not scoped to Android-only as previously
  planned. Needs a decision on approach (`#if ANDROID` guards vs. a platform-partial service vs. a
  no-op default implementation) once iOS work actually begins, since `PushRegistrationService`
  references the plugin's types unconditionally in shared code.
- **`ApplicationId` legacy value** (`za.co.alzow.alzowmauicomponents`) — see §8. Needs an owner
  decision before publishing; not a code fix.
- **`booking_status` enum labels unresolved** — whether the terminal status a booking-completion
  trigger should watch for is `"done"` or `"completed"` is not confirmed against the live schema
  (see §4).
- iOS setup (§9) — not started, blocked on Apple Developer Program enrolment.
- The permission-priming explainer before the system permission prompt — not built; worth doing
  before a pilot rollout, per earlier discussion, but not urgent now.
