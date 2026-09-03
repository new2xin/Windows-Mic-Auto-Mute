# Windows Mic Auto Mute

Windows Mic Auto Mute is a small Windows utility that automatically mutes selected microphone recording endpoints through the Windows Core Audio `IAudioEndpointVolume::SetMute(TRUE)` API.

It is intended for microphones that are permanently connected and should start muted when you log on to Windows. The normal setup runs a one-shot startup action and exits; USB reconnects or later device changes can be handled with the explicit BAT files.

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

`auto_mute_on.bat` registers the `Windows Mic Auto Mute` logon task and runs the one-shot startup mute immediately. The startup action waits for a matching device for up to 30 seconds, applies mute once, and exits.

To disable the automation, run `auto_mute_off.bat`. This stops and removes the logon task but does not change the current mute state. Run `unmute_now.bat` separately if you want to unmute.

## Device configuration

The checked-in `devices.json` is an example for a microphone that appears as `AKG C44-USB Microphone` on the tested Windows system. The application itself has no vendor- or model-specific logic.

```json
{
  "pollIntervalMs": 2000,
  "startupTimeoutMs": 30000,
  "targets": [
    {
      "nameContains": "C44-USB",
      "idContains": "",
      "enabled": true
    }
  ]
}
```

For a TM-250U or any other Windows recording device, change `nameContains` to a distinctive part of its Windows friendly name. If multiple devices share a similar name, run `status.bat` and use a distinctive portion of the reported endpoint ID in `idContains`. `devices.example.json` contains C44-USB and TM-250U examples.

## Commands

- `auto_mute_on.bat` — register one-shot mute-at-logon automation
- `auto_mute_off.bat` — remove mute-at-logon automation
- `mute_now.bat` — explicitly mute matching devices
- `unmute_now.bat` — explicitly unmute matching devices
- `status.bat` — show matching devices and their current mute states
- `build.bat` — build the .NET application; existing build output is renamed to a timestamped backup first

The startup action writes operational messages to `dist\logs\automute.log`. `startupTimeoutMs` controls how long it waits for a matching endpoint at logon; it is clamped to 120 seconds.

## Verification boundary

The implementation has been build-verified with zero warnings and zero errors. Core Audio enumeration, mute-state reading, one-shot startup muting, and clean process exit were exercised against an active USB microphone. Manual `unmute_now.bat` is not overridden afterward; the next automatic mute occurs at the next logon or when `mute_now.bat` is run. Physical LED behavior still depends on the connected microphone firmware and should be checked on the target machine.

## License

MIT. See [LICENSE](LICENSE).
