# LUMA//CTRL

**A customizable Windows desktop controller for Bluetooth Low Energy (BLE) LED strips.**

LUMA//CTRL is a native Windows desktop application that lets you control BLE-powered RGB LED strips from your PC. It combines manual colour control, brightness adjustment, custom presets, and real-time lighting effects in a dedicated desktop interface.

Built with C#, WPF, and .NET 9.

<p align="center">
  <strong>Control your lights. Shape your atmosphere.</strong>
</p>

---

## Features

### 🎨 Colour Studio
- Interactive HSV colour wheel.
- Real-time colour preview.
- HEX colour input.
- RGB value display.
- Direct BLE colour updates.

### 💡 Lighting Control
- Adjustable LED brightness.
- Bluetooth Low Energy connectivity.
- Device-specific communication protocols.
- Dedicated support for the ELK-BLEDOB controller.

### 🌈 Lighting Effects

| Effect | Description |
|---|---|
| **Aurora** | Smooth, continuously transitioning colours inspired by the Northern Lights. |
| **Ember** | A warm, flickering effect inspired by glowing embers and fire. |
| **Screen Reactive** | Samples the average colour and brightness of your desktop screen and reflects them on the LED strip. |

Screen Reactive currently uses average screen sampling rather than independently controlled lighting zones.

### 🎯 Presets
- Five built-in colour presets.
- Five customizable user presets.
- Save, overwrite, and clear custom presets.
- Custom presets persist between application restarts.

Custom presets are stored locally at:

```text
%AppData%\LumaCtrl\custom-presets.json
```

Your presets are not stored in the GitHub repository.

---

## Screenshots

<!-- Add screenshots using:
![LUMA//CTRL Main Interface](docs/images/main-window.png)

-->
[LUMA//CTRL Main Interface](docs/images/colourpicker.jpg)

[LUMA//CTRL Colour Picker](docs/images/mainwindow.jpg)

---

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application logic |
| .NET 9 | Runtime and framework |
| WPF | Native Windows desktop UI |
| XAML | UI layout and styling |
| Windows Bluetooth APIs | BLE communication |
| GATT | Bluetooth service and characteristic communication |
| GDI / Windows APIs | Desktop screen sampling |
| System.Text.Json | Custom preset persistence |

---

## Hardware Compatibility

LUMA//CTRL communicates with LED controllers using Bluetooth Low Energy and GATT.

### Tested hardware

**ELK-BLEDOB BLE LED Controller**

The following hardware details have been verified:

- BLE service UUID: `0000fff0-0000-1000-8000-00805f9b34fb`
- Write characteristic UUID: `0000fff3-0000-1000-8000-00805f9b34fb`
- Communication: GATT Write Without Response
- RGB command protocol: Custom byte-frame protocol

The ELK-BLEDOB controller has a device-specific red/green channel ordering quirk. LUMA//CTRL handles this through a dedicated device protocol implementation.

### Other BLE controllers

The project includes a generic BLE protocol implementation as a starting point for supporting additional controllers.

However, BLE LED controllers do not all use the same services, characteristics, command frames, or channel ordering. Compatibility with other controllers is not guaranteed.

Additional device profiles and protocol implementations can be contributed.

---

## Project Architecture

The project separates application logic, device-specific communication, and UI components.

```text
LumaCtrl/
│
├── Models/
│   └── DeviceProfile.cs
│
├── Services/
│   ├── BluetoothService.cs
│   ├── CustomPresetService.cs
│   ├── DeviceProfileService.cs
│   ├── ScreenSampler.cs
│   │
│   └── Protocols/
│       ├── IDeviceProtocol.cs
│       ├── ElkBledobProtocol.cs
│       └── GenericBleProtocol.cs
│
├── Views/
│   └── ColorPickerControl.xaml
│
├── App.xaml
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── LumaCtrl.csproj
└── LumaCtrl.ico
```

*The tree above illustrates the main project components; consult the repository for the complete current structure.*

### Device profiles

Device-specific communication is separated from the main Bluetooth service.

- `IDeviceProtocol` defines the colour-command interface.
- `ElkBledobProtocol` handles the ELK-BLEDOB command format and its channel-ordering quirk.
- `GenericBleProtocol` provides a generic command implementation.
- `DeviceProfileService` selects the appropriate protocol for a device.

This makes it easier to add support for additional BLE controllers without mixing their protocol-specific behaviour into the UI or main Bluetooth service.

---

## Getting Started

### Requirements

- Windows 10 or Windows 11, 64-bit.
- Bluetooth Low Energy capable hardware.
- A compatible BLE LED controller.
- .NET 9 SDK.
- Visual Studio 2022 with the .NET desktop development workload, or another environment with the .NET 9 SDK.

### Clone the repository

```bash
git clone https://github.com/ImSanz07/LUMA-CTRL.git
cd LUMA-CTRL
```

Open `LumaCtrl.sln` in Visual Studio if a solution file is present, or open the `.csproj` directly.

### Build and run

```powershell
dotnet restore
dotnet build
dotnet run
```

### Publish a standalone Windows application

The project is configured for Windows x64 self-contained, single-file publishing.

```powershell
dotnet publish -c Release
```

The published application will be generated under:

```text
bin/Release/net9.0-windows10.0.19041.0/win-x64/publish/
```

The published executable includes the .NET runtime, so users do not need to install the .NET runtime separately.

---

## Roadmap

Some ideas for future development:

- [ ] Support for additional BLE LED controllers.
- [ ] More configurable device profiles.
- [ ] Device discovery and selection.
- [ ] More lighting effects.
- [ ] Screen Reactive zone-based sampling.
- [ ] Improved device configuration and connection handling.
- [ ] More customization options for lighting effects.

---

## Contributing

Contributions, suggestions, bug reports, and hardware compatibility reports are welcome.

If you want to add support for another LED controller, useful information includes:

- Controller model and hardware details.
- BLE service and characteristic UUIDs.
- Supported GATT write operations.
- RGB command format.
- Channel ordering and brightness behaviour.
- Any controller-specific quirks.

For substantial changes, consider opening an issue first to discuss the proposed implementation.

Please avoid submitting personal configuration files, generated build output, or private device information.

---

## License

LUMA//CTRL is released under the **MIT License**.

You are free to use, modify, distribute, and build upon this project, subject to the terms of the license.

See [LICENSE](LICENSE) for details.

---

**LUMA//CTRL**  
*Built for the love of custom lighting.*