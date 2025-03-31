$sqlcequeryDir = "$env:ALLUSERSPROFILE\chocolatey\lib\sqlcequery"
# $sqlcequeryExe = "$sqlcequeryDir\tools\QueryAnalyzer.exe"
# Install-ChocolateyFileAssociation ".sdf" $sqlcequeryExe

cmd /c assoc .sdf=sqlcedbfile
cmd /c ftype sqlcedbfile=QueryAnalyzer.exe -File `"QueryAnalyzer.exe`" `"%1`"

$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$Home\Desktop\SQL Compact Query Analyzer (x64).lnk")
$Shortcut.TargetPath = "$sqlcequeryDir\tools\QueryAnalyzer.exe"
$Shortcut.Save()
