# Hue Entertainment Pro

This add-on runs **Hue Entertainment Pro**, a web app that connects to multiple
Philips Hue bridges over your local network. It uses the Hue Entertainment API to
update the lights almost instantly, and lets you merge several entertainment areas
into one so you can drive large setups (20+ lights) as a single group.

## Features

- Combine multiple entertainment areas into one
- Control multiple entertainment areas from multiple bridges at the same time
- Built-in effects, BPM input, 2D and 3D previews
- Random mode and Auto (party) mode
- DEMO mode to try it without a Hue bridge
- SQLite storage (persisted to the add-on `/data` volume)

## Setup

1. Install and start the add-on.
2. Open it from the **Home Assistant sidebar** (the **Hue Entertainment Pro**
   entry). It is served via the Home Assistant ingress proxy, so no host port
   needs to be opened.
3. On the **Bridges** page, add your Hue bridge and press the link button on the
   bridge’s top to authorize.
4. Create groups/areas and start applying effects.

## Networking

The add-on follows Home Assistant best practices:

- It runs on the default add-on network (not `host_network`).
- The web UI is exposed through **Home Assistant ingress**, which routes traffic
  through Home Assistant's authenticated HTTPS reverse proxy. No `ports` are
  published, so there is no fixed host port that can conflict with other
  services.
- The app listens on port `8080` inside the container (`ingress_port`), and is
  reachable over the LAN through the normal HA ingress URL.

## Configuration

This add-on requires no configuration options. The `options`/`schema` are left
empty; all settings are managed through the web interface.

- **Connection strings / data**: the SQLite database is stored automatically in
  `/data`, mounted persistently by Home Assistant.

## Troubleshooting

- **"Can't connect to bridge"** — make sure the add-on host and the Hue bridge
  are on the same network/VLAN, and that multicast traffic isn't blocked by the
  router. You can also add the bridge by its IP address.
- **Slow install** — first build compiles the .NET application; subsequent
  installs/updates reuse the Docker build cache.
- **Direct LAN access** — by design no host port is exposed (avoids conflicts).
  If you need direct `host:port` access in addition to the sidebar, add a
  `ports` mapping (`8080/tcp: <a free host port>`) and set `ingress: false` in
  `config.yaml`.
