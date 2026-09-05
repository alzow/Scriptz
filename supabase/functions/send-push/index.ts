import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

// ---------------------------------------------------------------
// Service account + Google OAuth2
// ---------------------------------------------------------------

interface ServiceAccount {
  client_email: string;
  private_key: string;
  token_uri: string;
  project_id: string;
}

const serviceAccount: ServiceAccount = JSON.parse(
  Deno.env.get("FCM_SERVICE_ACCOUNT") ?? "{}",
);

// Cached across invocations while the instance stays warm.
let cachedToken: { value: string; expiresAt: number } | null = null;

function base64Url(input: ArrayBuffer | string): string {
  const bytes = typeof input === "string"
    ? new TextEncoder().encode(input)
    : new Uint8Array(input);
  let binary = "";
  for (const b of bytes) binary += String.fromCharCode(b);
  return btoa(binary)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}

async function importPrivateKey(pem: string): Promise<CryptoKey> {
  // The JSON stores newlines escaped; restore them before parsing.
  const normalised = pem.replace(/\\n/g, "\n");
  const body = normalised
    .replace(/-----BEGIN PRIVATE KEY-----/, "")
    .replace(/-----END PRIVATE KEY-----/, "")
    .replace(/\s/g, "");
  const raw = Uint8Array.from(atob(body), (c) => c.charCodeAt(0));

  return await crypto.subtle.importKey(
    "pkcs8",
    raw.buffer,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["sign"],
  );
}

async function getAccessToken(): Promise<string> {
  const now = Math.floor(Date.now() / 1000);

  // Refresh a minute early to avoid using a token that expires mid-flight.
  if (cachedToken && cachedToken.expiresAt > now + 60) {
    return cachedToken.value;
  }

  const header = base64Url(JSON.stringify({ alg: "RS256", typ: "JWT" }));
  const claims = base64Url(JSON.stringify({
    iss: serviceAccount.client_email,
    scope: "https://www.googleapis.com/auth/firebase.messaging",
    aud: serviceAccount.token_uri,
    iat: now,
    exp: now + 3600,
  }));

  const key = await importPrivateKey(serviceAccount.private_key);
  const signature = await crypto.subtle.sign(
    "RSASSA-PKCS1-v1_5",
    key,
    new TextEncoder().encode(`${header}.${claims}`),
  );

  const jwt = `${header}.${claims}.${base64Url(signature)}`;

  const res = await fetch(serviceAccount.token_uri, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "urn:ietf:params:oauth:grant-type:jwt-bearer",
      assertion: jwt,
    }),
  });

  if (!res.ok) {
    throw new Error(`Token exchange failed: ${res.status} ${await res.text()}`);
  }

  const data = await res.json();
  cachedToken = {
    value: data.access_token,
    expiresAt: now + (data.expires_in ?? 3600),
  };
  return cachedToken.value;
}

// ---------------------------------------------------------------
// FCM send
// ---------------------------------------------------------------

interface SendResult {
  ok: boolean;
  unregistered: boolean;
  error?: string;
}

async function sendToToken(
  accessToken: string,
  fcmToken: string,
  title: string,
  body: string,
  data: Record<string, string>,
): Promise<SendResult> {
  const url =
    `https://fcm.googleapis.com/v1/projects/${serviceAccount.project_id}/messages:send`;

  const res = await fetch(url, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      message: {
        token: fcmToken,
        notification: { title, body },
        data,
        android: {
          priority: "high",
          notification: { channel_id: "queue_updates" },
        },
      },
    }),
  });

  if (res.ok) return { ok: true, unregistered: false };

  const text = await res.text();

  // A dead token: uninstalled app, cleared data, or rotated token.
  // Distinguish from transient failures so we only delete real corpses.
  const isDead = res.status === 404 ||
    text.includes("UNREGISTERED") ||
    text.includes("INVALID_ARGUMENT");

  return { ok: false, unregistered: isDead, error: `${res.status}: ${text}` };
}

// ---------------------------------------------------------------
// Handler
// ---------------------------------------------------------------

Deno.serve(async (req) => {
  try {
    const payload = await req.json();

    // Accepts either a webhook envelope ({ type, record, ... }) or a bare
    // notification row, so the curl test and the webhook share one path.
    const notification = payload.record ?? payload;

    const {
      id: notificationId,
      user_id: userId,
      title,
      body,
      action,
      action_params: actionParams,
    } = notification;

    if (!userId || !title || !body) {
      return new Response(
        JSON.stringify({ error: "missing user_id, title or body" }),
        { status: 400, headers: { "Content-Type": "application/json" } },
      );
    }

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
    );

    const { data: tokens, error: tokenError } = await supabase
      .from("device_tokens")
      .select("id, fcm_token")
      .eq("user_id", userId);

    if (tokenError) throw tokenError;

    if (!tokens || tokens.length === 0) {
      // Not an error. A user with no device simply has nothing to receive.
      return new Response(
        JSON.stringify({ sent: 0, reason: "no devices registered" }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }

    const accessToken = await getAccessToken();

    // FCM data values must all be strings.
    const data: Record<string, string> = {
      action: action ?? "",
      action_params: JSON.stringify(actionParams ?? {}),
      notification_id: String(notificationId ?? ""),
    };

    let sent = 0;
    let failed = 0;
    const deadTokenIds: string[] = [];
    const deliveries: Record<string, unknown>[] = [];

    for (const t of tokens) {
      const result = await sendToToken(
        accessToken,
        t.fcm_token,
        title,
        body,
        data,
      );

      if (result.ok) sent++;
      else failed++;

      if (result.unregistered) deadTokenIds.push(t.id);

      if (notificationId) {
        deliveries.push({
          notification_id: notificationId,
          device_token_id: t.id,
          status: result.ok
            ? "sent"
            : result.unregistered
            ? "unregistered"
            : "failed",
          error: result.error ?? null,
          sent_at: result.ok ? new Date().toISOString() : null,
        });
      }
    }

    if (deliveries.length > 0) {
      const { error } = await supabase
        .from("notification_deliveries")
        .insert(deliveries);
      // Logging failure must not fail the send — the push already went out.
      if (error) console.error("delivery log failed:", error);
    }

    if (deadTokenIds.length > 0) {
      // device_token_id is ON DELETE SET NULL, so delivery history survives.
      const { error } = await supabase
        .from("device_tokens")
        .delete()
        .in("id", deadTokenIds);
      if (error) console.error("stale token cleanup failed:", error);
    }

    return new Response(
      JSON.stringify({ sent, failed, cleaned: deadTokenIds.length }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  } catch (err) {
    console.error("send-push failed:", err);
    return new Response(
      JSON.stringify({ error: String(err) }),
      { status: 500, headers: { "Content-Type": "application/json" } },
    );
  }
});