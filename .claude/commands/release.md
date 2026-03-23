Führe den Release-Prozess für EasyBluetoothAudio aus.

Der Benutzer hat folgenden Befehl eingegeben: `/release $ARGUMENTS`

Gültige Argumente: `patch` (Standard), `minor`, `major`, `test`

Führe folgenden Bash-Befehl aus:
```
cmd /c "cd /d C:\dev\EasyBluetoothAudio && release.bat $ARGUMENTS"
```

Falls kein Argument angegeben wurde, verwende `patch`.

Zeige dem Benutzer danach eine kurze Zusammenfassung was passiert ist.
