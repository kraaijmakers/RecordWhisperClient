# Recorder & Whisper.cpp client (RecordWhisperClient)

A feature-rich Windows Forms application that provides system tray audio recording with automatic transcription via Whisper.cpp. The application runs minimized in the taskbar with global hotkeys for recording control and advanced audio processing capabilities.

## ✨ Features

### Core Recording
- **System Tray Integration**: Runs minimized in the system tray with no visible window
- **Global Hotkeys**: Customizable hotkey combinations (default: Ctrl+Shift+R) to start/stop recording from anywhere
- **High-Quality Audio**: Records at 44.1kHz, 16-bit, mono using NAudio library
- **Multiple Input Devices**: Select from any available audio input device
- **Volume-Based Auto Recording**: Automatically start recording when volume exceeds a threshold
- **Smart Silence Detection**: Automatically stop recording after a configurable period of silence

### Transcription & AI
- **Automatic Transcription**: Integrates with Whisper.cpp server for speech-to-text conversion
- **Optional Transcription**: Can be enabled/disabled per user preference
- **Language Support**: Configurable transcription language (auto-detect, en, es, fr, etc.)
- **OpenAI-Compatible**: Works with OpenAI-compatible transcription services
- **API Key Support**: Optional API authentication for secured services

### User Experience
- **Single Instance Protection**: Prevents multiple instances, opens Configuration when launched again
- **Smart Notifications**: Three notification modes (None, Recording, Verbose)
- **Clipboard Integration**: Optionally copy transcriptions directly to clipboard with audio feedback
- **Dark/Light Theme**: Automatic Windows theme detection with custom accent colors
- **Connection Testing**: Built-in server connectivity testing
- **Startup Resilience**: Application remains responsive even if Whisper server is unavailable

### File Management
- **Organized Storage**: Each recording creates a timestamped folder with audio and transcription files
- **Configurable Paths**: Customize recording storage location and folder naming
- **Duration Tracking**: Automatic audio duration calculation and logging
- **Comprehensive Logging**: Detailed application logs for troubleshooting

## 🔧 Requirements

- **.NET 9.0 Windows Runtime**
- **Whisper.cpp Server**: Running on configurable endpoint (default: `http://127.0.0.1:8080`)
- **Windows 10/11**: For optimal theme integration and system tray functionality

## 🚀 Quick Start

1. **Launch Application**: The app minimizes to system tray on startup
2. **First Run**: Configuration dialog appears for initial setup
3. **Start Recording**: Use your configured hotkey (default: Ctrl+Shift+R)
4. **Stop Recording**: Press the hotkey again or wait for auto-stop (if enabled)
5. **Access Files**: Recordings are saved to your configured directory with automatic transcription

## ⚙️ Configuration

Access configuration by:
- Right-clicking the system tray icon → "Configuration..."
- Starting a second instance of the application (auto-opens Configuration)

### Server Settings
- **Whisper Server URL**: Configure your Whisper.cpp server endpoint
- **API Key**: Optional authentication for secured services
- **Transcription Language**: Set language preference (auto, en, es, fr, de, etc.)
- **Connection Testing**: Test server connectivity before saving

### Recording Settings
- **Recordings Path**: Choose where to save your recordings
- **Folder Suffix**: Customize the suffix added to recording folders
- **Input Device**: Select from available audio input devices
- **Enable Transcription**: Toggle automatic transcription on/off

### Volume Activation
- **Enable Volume Activation**: Automatically start recording when sound is detected
- **Volume Threshold**: Set sensitivity (1-20%) for automatic recording trigger
- **Silence Timeout**: Configure how long to wait (1-30 seconds) before auto-stopping

### Hotkey Configuration
- **Enable Global Hotkey**: Toggle hotkey functionality
- **Modifier Keys**: Choose combination of Ctrl, Shift, Alt
- **Primary Key**: Select from A-Z, 0-9, F1-F12, or SPACE
- **Validation**: Ensures at least one modifier key is always selected

### Notification & UI
- **Notification Mode**:
  - **None**: No notifications
  - **Recording**: Show start/stop notifications only
  - **Verbose**: Show all transcription progress notifications
- **Copy to Clipboard**: Automatically copy transcriptions to clipboard
- **Theme Integration**: Automatic Windows dark/light theme detection

## 📁 File Structure

Each recording session creates a timestamped folder:
```
~/Documents/Recordings/2024-01-15_14-30-25_Recording/
├── audio.wav          # Original recorded audio (44.1kHz, 16-bit, mono)
└── transcription.txt  # Whisper transcription with metadata
```

### Transcription File Format
```
Transcription generated on: 2024-01-15 14:30:45
Audio file: audio.wav
Duration: 02:35

Transcription:
[Your transcribed text appears here]
```

## 🔨 Building from Source

### Development Build
```bash
# Clone and build
git clone [repository-url]
cd WhisperUI-Win32
dotnet build
```

### Release Build
```bash
# Standard release build
dotnet build -c Release

# Self-contained executable (recommended)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Build Requirements
- .NET 9.0 SDK
- Windows development environment
- Visual Studio 2022 or VS Code (recommended)

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1 | Audio recording and playback |
| NAudio.Lame | 2.1.0 | MP3 encoding support |
| Newtonsoft.Json | 13.0.3 | JSON serialization for Whisper API |
| Target Framework | .NET 9.0 Windows | Windows Forms application platform |

## 🔍 Advanced Features

### Inter-Process Communication
- Single instance enforcement using named mutexes
- Event-based communication between instances
- Automatic Configuration dialog opening from second instance

### Audio Processing
- Automatic stereo-to-mono conversion
- MediaFoundation resampler with high-quality fallback
- RMS volume calculation for voice activation
- Configurable silence detection algorithms

### Error Handling & Resilience
- Comprehensive error logging and recovery
- Graceful degradation when Whisper server unavailable
- Clipboard operation retry logic with timeout handling
- Resource cleanup and proper disposal patterns

### Theme Integration
- Windows accent color detection from registry
- Automatic dark/light theme switching
- Custom themed controls and context menus
- DWM integration for native title bar styling

## 🐛 Troubleshooting

### Common Issues

**Application won't start recording:**
- Check if Whisper server is running and accessible
- Verify audio input device is available and not in use
- Test hotkey configuration in Settings

**Transcription not working:**
- Use "Test Connection" button in Configuration
- Verify Whisper server URL and API key (if required)
- Check application logs in `%LocalAppData%\Recorder & Whisper.cpp client\`

**Volume activation not triggering:**
- Adjust volume threshold sensitivity
- Ensure correct input device is selected
- Test microphone levels in Windows Sound settings

### Log Access
- Click "Open Log Folder" in Configuration dialog
- Logs are automatically rotated when they exceed 10MB
- Detailed logging includes audio format, server responses, and error traces

## 📋 System Requirements

- **Operating System**: Windows 10 version 1903 or later, Windows 11
- **Framework**: .NET 9.0 Windows Desktop Runtime
- **Memory**: 50MB RAM (typical usage)
- **Storage**: 10MB application + recording storage space
- **Audio**: Any Windows-compatible audio input device
- **Network**: HTTP access to Whisper.cpp server (local or remote)

## 👨‍💻 Author

**Kevin Raaijmakers**
- GitHub: [@kraaijmakers](https://github.com/kraaijmakers)
- Project: [github.com/kraaijmakers/RecordWhisperClient](https://github.com/kraaijmakers)

## 🔗 Related Projects

- **Whisper.cpp**: Fast C++ implementation of OpenAI's Whisper
- **NAudio**: .NET audio library for Windows applications
- **OpenAI Whisper**: Original automatic speech recognition model

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Copyright © 2025 Kevin Raaijmakers (github.com/kraaijmakers)

### Third-Party Licenses
All dependencies are MIT licensed and compatible:
- **NAudio**: MIT License
- **NAudio.Lame**: MIT License  
- **Newtonsoft.Json**: MIT License
- **Microsoft.VisualBasic**: MIT License

---

**Note**: This application requires a separately running Whisper.cpp server instance. Refer to the Whisper.cpp documentation for server setup and configuration instructions.