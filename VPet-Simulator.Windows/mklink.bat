mklink /d "%~dp0\bin\x64\Debug\net8.0-windows\mod" "%~dp0\mod"

echo The following is the automatic link generation for other related MODs. If an error is prompted, it is a normal phenomenon and can be ignored

mklink /d "%~dp0\bin\x86\Debug\net8.0-windows\mod" "%~dp0\mod"
mklink /d "%~dp0\bin\x64\Release\net8.0-windows\mod" "%~dp0\mod"
mklink /d "%~dp0\..\VPet.Solution\bin\Debug\net8.0-windows\mod" "%~dp0\mod"

mklink /d "%~dp0\mod\0001_ModMaker" "%~dp0\..\..\VPet.ModMaker\0001_ModMaker"
mklink /d "%~dp0\mod\1100_DemoClock" "%~dp0\..\..\VPet.Plugin.Demo\VPet.Plugin.DemoClock\1100_DemoClock"
mklink /d "%~dp0\mod\1111_ChatGPTPlus" "%~dp0\..\..\VPet.Plugin.ChatGPTPlus\VPet.Plugin.ChatGPTPlus\1111_ChatGPTPlus"
mklink /d "%~dp0\mod\1101_EdgeTTS" "%~dp0\..\..\VPet.Plugin.Demo\VPet.Plugin.EdgeTTS\1101_EdgeTTS"
mklink /d "%~dp0\mod\1110_ChatGPT" "%~dp0\..\..\VPet.Plugin.Demo\VPet.Plugin.ChatGPT\1110_ChatGPT" 
mklink /d "%~dp0\mod\1111_MutiPlayerStream" "%~dp0\..\..\VPet.Plugin.Demo\VPet.MutiPlayer.Stream\1111_MutiPlayerStream" 
mklink /d "%~dp0\mod\1123_MutiRedEnvelope" "%~dp0\..\..\VPet.Plugin.Demo\VPet.Plugin.MutiRedEnvelope\1123_MutiRedEnvelope" 
pause