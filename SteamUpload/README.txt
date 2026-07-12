STEAMPIPE UPLOAD - METAL DETECTOR GAME

App ID: 4916780
Windows Depot ID: 4916781
Executable: MetalDetectorGame.exe
Company: BetDev

The upload reads game files directly from the project's Builds folder.
The BurstDebugInformation_DoNotShip folder, PDB files and steam_appid.txt
are excluded from the Steam depot.

Upload command (run from the Steamworks SDK ContentBuilder/builder folder):

steamcmd.exe +login YOUR_STEAM_LOGIN +run_app_build "C:\Users\Kamil\MetalDetectorGame\SteamUpload\scripts\app_build_4916780.vdf" +quit

Do not put a password into the VDF files. Steam Guard may ask for a code.
After upload, select the new build on Steamworks > SteamPipe > Builds and
set it live on the default branch only after testing it through Steam.
