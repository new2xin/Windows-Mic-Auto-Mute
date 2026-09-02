# Lyra Auto Mute

Lyra Auto Mute is a small Windows utility that automatically mutes selected USB microphone recording endpoints through the Windows Core Audio `IAudioEndpointVolume::SetMute(TRUE)` API.

It is intended for microphones that are permanently connected and should start muted whenever Windows detects them. The utility also handles USB re-enumeration, hub reconnects, and resume from sleep by checking for matching active capture endpoints every two seconds.

## Features

- Automatically mutes matching recording devices after Windows recognizes them.
- Uses explicit mute state, so repeated checks never toggle an already-muted device back on.
- Supports matching by a case-insensitive substring of the Windows friendly name and/or endpoint ID.
- Provides BAT files for enabling, disabling, muting, unmuting, and checking status.
- Uses only Windows Core Audio COM APIs and the .NET 8 SDK; no third-party NuGet packages are required.

## Requirements

- Windows 10 or Windows 11
- .NET 8 runtime to run the built executable
- .NET 8 SDK to build from source

The USB device itself must expose a Windows recording endpoint. Physical USB 5V power-on happens before Windows can enumerate the device, so this tool cannot mute the microphone during that earlier hardware-only interval.

## Quick start

1. Run `build.bat`.
2. Check or edit `devices.json`.
3. Run `auto_mute_on.bat` once.

`auto_mute_on.bat` registers the `Lyra Auto Mute` logon task and starts the watcher immediately. It also applies mute to any currently matching device.

To disable the automation, run `auto_mute_off.bat`. This stops and removes the logon task but does not change the current mute state. Run `unmute_now.bat` separately if you want to unmute after disabling the watcher.

## Device configuration

The default configuration targets AKG Lyra as it appears on the tested Windows system: `AKG C44-USB Microphone`.

```json
{
  "pollIntervalMs": 2000,
  "targets": [
    {
      "nameContains": "C44-USB",
      "idContains": "",
      "enabled": true
    }
  ]
}
```

For a TM-250U, change `nameContains` to `TM-250U`. If multiple devices share a similar name, run `status.bat` and use a distinctive portion of the reported endpoint ID in `idContains`. `devices.example.json` contains both Lyra and TM-250U examples.

## Commands

- `auto_mute_on.bat` — register logon automation and start watching
- `auto_mute_off.bat` — stop watching and remove logon automation
- `mute_now.bat` — explicitly mute matching devices
- `unmute_now.bat` — explicitly unmute matching devices
- `status.bat` — show matching devices and their current mute states
- `build.bat` — build the .NET application; existing build output is renamed to a timestamped backup first

The watcher writes operational messages to `dist\logs\automute.log`.

## Verification boundary

The implementation has been build-verified with zero warnings and zero errors. Core Audio enumeration and mute-state reading were exercised against an active USB microphone, and the watcher was verified to restore mute after a manual unmute. Physical LED behavior and USB reconnect behavior still depend on the connected microphone firmware and should be checked on the target machine.

## License

MIT. See [LICENSE](LICENSE).
