# Diatoms-VR

A Unity (URP/XR) project. This repository contains the Unity project used to run the Diatoms VR demo.

## Requirements
- Unity Editor: 6000.0.23f1 (as per `ProjectSettings/ProjectVersion.txt`)
- Platform modules as needed (e.g., Windows, Android/Quest). Install via Unity Hub > Installs > Add modules
- Git LFS (optional): not required for this repo after cleanup

## Getting Started
1. Clone the repository
   ```bash
   git clone https://github.com/hongyuwan/Diatoms-VR.git
   cd Diatoms-VR
   ```
2. Open in Unity Hub
   - Open Unity Hub
   - Add the project folder
   - Ensure the editor version is `6000.0.23f1`
3. First open will regenerate `Library/` and restore packages based on `Packages/manifest.json`

## Play the Scene
- Open `Assets/Scenes/MainScene.unity` (or your target scene)
- Press Play in the Editor

## XR/Controls
- This project uses Unity XR Interaction Toolkit (3.x)
- For desktop testing, you can enable the XR Device Simulator (Window > Analysis > Input Debugger or add the Simulator component to a test scene)

## Build
1. Open `File > Build Settings`
2. Select your target platform (e.g., Windows / Android for Quest)
3. Add the main scene(s) to the build list if not already included
4. Click Build or Build and Run

## OpenAI/Text Features (optional)
- The project contains optional OpenAI client/server scripts
- API key is read from `Assets/_Systems/OpenAPI/Secret.cs` which is ignored by Git
- Create the file locally with your key if you use that feature:
  ```csharp
  // Assets/_Systems/OpenAPI/Secret.cs
  public static class Secret {
      public const string API_KEY = "YOUR_OPENAI_KEY";
  }
  ```

## Repository Hygiene
- `Library/` and IDE folders are ignored
- JetBrains Rider/IntelliJ `.idea/` is ignored

## Troubleshooting
- Missing packages or compile errors on first open: click `Window > Package Manager` and let Unity resolve dependencies
- Wrong Unity version: install `6000.0.23f1` via Unity Hub and reopen
- XR not running on device: verify XR Plug-in Management settings under `Project Settings > XR Plug-in Management`

## License
- TBD
