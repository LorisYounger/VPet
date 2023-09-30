
mklink /d "%~dp0\bin\x64\Debug\net462\mod" "%~dp0\mod"
mklink /d "%~dp0\bin\x64\Release\net462\mod" "%~dp0\mod"

mklink /d "%~dp0\mod\0001_ModMaker" "%~dp0\..\..\VPet.ModMaker\0001_ModMaker"
mklink /d "%~dp0\mod\1100_DemoClock" "%~dp0\..\..\VPet.Plugin.DemoClock\VPet.Plugin.DemoClock\1100_DemoClock"
mklink /d "%~dp0\mod\1101_EdgeTTS" "%~dp0\..\..\VPet.Plugin.DemoClock\VPet.Plugin.EdgeTTS\1101_EdgeTTS"
mklink /d "%~dp0\mod\1110_ChatGPT" "%~dp0\..\..\VPet.Plugin.DemoClock\VPet.Plugin.ChatGPT\1110_ChatGPT" 
pause