# Hue Entertainment Pro (Home Assistant app)

> Home Assistant renamed **add-ons** to **apps**. On older Home Assistant versions
> this feature is still called *Add-ons* and lives under **Settings → Add-ons**.

This app runs [Hue Entertainment Pro](https://github.com/michielpost/HueEntertainmentPro)
inside Home Assistant. It is a web app that connects to multiple Philips Hue
bridges over your local network and merges their entertainment areas so you can
apply realtime light effects to a large number of lights at once.

## Installation

1. In Home Assistant, go to **Settings → Apps** and select **Install app** to open
   the app store.
2. In the top-right corner, select the **⋮** menu and choose **Repositories**.
   Enter the URL of this repository and select **Add**. If the repository does not
   show up afterwards, refresh the browser.
3. Close the dialog, find **Hue Entertainment Pro** in the store and select it.
4. Select **Install**. Home Assistant downloads a pre-built multi-arch image
   (`amd64` and `aarch64`), so installation takes seconds — no compilation on your
   device.
5. Select **Start**.
6. Open the interface from the **Home Assistant sidebar** — an entry named
   **Hue Entertainment Pro** is added automatically.

## Usage

- Use the **DEMO** mode or the **Add bridge** page to link your Philips Hue bridge
  (press the link button on the bridge when prompted).
- The app can reach Hue bridges on your LAN; `ingress` routes the UI through
  Home Assistant's secure reverse proxy, so you don't need to open any port.
- Follow the on-screen instructions to create areas and start effects.

## Data

The SQLite database is stored on Home Assistant's persistent `/data` volume, so
your bridges and settings survive app restarts and updates.

## Access / Ports

- The UI is served through the **Home Assistant ingress** proxy (no host port is
  exposed, so there is nothing to conflict with other services).
- The app listens on port `8080` **inside the container** only; it is not
  published to the host.

## For developers

The image is built from the **repository root** (the app folder cannot see
the app source). To build locally:

```sh
docker build -f huelightdj/Dockerfile .
```

Releases are published to `ghcr.io/michielpost/huelightdj-addon` by the
`addon.yml` GitHub Actions workflow, tagged with the `version` from
`config.yaml`. Bump the version there to ship an update.
