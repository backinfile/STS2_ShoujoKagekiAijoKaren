// Retained route for existing links; relay traffic now uses trusted domain HTTPS directly.
// This Worker deliberately opens no origin sockets, so it cannot bypass origin per-IP budgets.
export default {
  async fetch(request) {
    const url = new URL(request.url);
    if (url.pathname === "/rooms") return new Response(JSON.stringify({error: "client_update_required",
      endpoint: "wss://karen-multiplayer-relay-c7a91e5d.17181718.xyz/rooms"}), {status: 410,
      headers: {"Content-Type": "application/json", "Cache-Control": "no-store"}});
    if (!["GET", "HEAD"].includes(request.method)) return new Response("Read only", {status: 405});
    return new Response("Not found", {status: 404, headers: {"Cache-Control":"no-store"}});
  }
};
