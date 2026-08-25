# Hue Entertainment Pro (Home Assistant add-on)

This add-on runs [Hue Entertainment Pro](https://github.com/michielpost/HueLightDJ)
inside Home Assistant. It is a web app that connects to multiple Philips Hue
bridges over your local network and merges their entertainment areas so you can
apply realtime light effects to a large number of lights at once.

## Installation

1. Add this repository to Home Assistant:
   **Settings → Add-ons → Add-on Store → ⋮ (top right) → Repositories** and enter
   the URL of this repository.
2. Find **Hue Entertainment Pro** in the store and click **Install**.
3. Home Assistant downloads a pre-built multi-arch image (`amd64` and
   `aarch64`), so installation takes seconds — no compilation on your device.
4. Start the add-on.
5. Open the interface from the **Home Assistant sidebar** — an entry named
   **Hue Entertainment Pro** is added automatically.

## Usage

- Use the **DEMO** app or the **Add bridge** page to link your Philips Hue bridge
  (press the link button on the bridge when prompted).
- The add-on can reach hue bridges on your LAN; `ingress` routes the UI through
  Home Assistant's secure reverse proxy, so you don't need to open any port.
- Follow the on-screen instructions to create areas and start effects.

## Data

The SQLite database is stored on Home Assistant's persistent `/data` volume, so
your bridges and settings survive add-on restarts and updates.

## Access / Ports

- The UI is served through the **Home Assistant ingress** proxy (no host port is
  exposed, so there is nothing to conflict with other services).
- The app listens on port `8080` **inside the container** only; it is not
  published to the host.

## For developers

The image is built from the **repository root** (the add-on folder cannot see
the app source). To build locally:

```sh
docker build -f addons/huelightdj/Dockerfile .
```

Releases are published to `ghcr.io/michielpost/huelightdj-addon` by the
`addon.yml` GitHub Actions workflow, tagged with the `version` from
`config.yaml`. Bump the version there (and keep the `image` tag in sync) to
ship an update.
