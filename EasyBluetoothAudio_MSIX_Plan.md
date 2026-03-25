# Update-Strategie: GitHub Releases vs. Microsoft Store

## Context

Die App hat aktuell einen In-App-Updater (GitHub Releases + Inno Setup). Der User möchte:
1. Verstehen ob ein separater Launcher-Updater (der **vor** dem App-Start läuft) sinnvoll ist
2. Den Weg zum Microsoft Store vorbereiten

## Aktueller Stand

```
App startet → CheckForUpdateAsync() im Hintergrund → UpdateAvailable = true →
Button blinkt → User klickt → Installer läuft silent → App schließt → neue Version startet
```

Das ist ein **In-App-Updater** (reaktiv). Ein **Pre-Launch-Updater** wäre ein separater Stub, der *zuerst* läuft.

---

## Vergleich: Drei Ansätze

### A) Aktuell: In-App-Updater (GitHub + Inno Setup)
- ✅ Läuft, funktioniert
- ✅ Kein Microsoft-Ökosystem-Lock-in
- ❌ Kein "Updater vor dem App-Start"
- ❌ Nicht Store-kompatibel
- ❌ Inno Setup ≠ MSIX

### B) MSIX + AppInstaller (empfohlen)
- ✅ AppInstaller-Datei = nativer "Pre-Launch-Updater" von Windows
- ✅ Gleiche Package-Format für Store-Einreichung → **0% verschwendete Arbeit**
- ✅ Store erkennt automatisch `PackageIdentity` → Update-UI kann ausgeblendet werden
- ✅ `PackageManager.AddPackageByAppInstallerFileAsync()` = Windows übernimmt die Updates
- ⚠️ Inno Setup muss ersetzt werden durch MSIX-Packaging
- ⚠️ Signing-Zertifikat für Sideloading nötig (self-signed oder EV cert)

### C) Velopack (ehemals Squirrel.Windows)
- ✅ Klassischer Pre-Launch-Stub (wie Steam/Electron)
- ✅ Delta-Updates (nur geänderte Bytes)
- ❌ Eigenes Framework, eigene Infrastruktur
- ❌ Store-Submission deutlich komplizierter
- ❌ Mehr Komplexität, weniger Windows-nativ

---

## Empfehlung: MSIX + AppInstaller als Weg zum Store

### Warum MSIX?

Der `.appinstaller`-Mechanismus von Windows **ist** der "separate Updater der vor dem App startet":
- Windows prüft beim Doppelklick auf die App ob Updates verfügbar sind
- Update wird vor dem App-Start eingespielt (konfigurierbar: HoursBetweenUpdateChecks)
- Kein eigener Updater-Code nötig

### Roadmap

**Phase 1 — MSIX-Packaging hinzufügen** (parallel zu Inno Setup)
- `Windows Application Packaging Project` zur Solution hinzufügen
- Oder: `dotnet publish` + MSIX-Manifest manuell
- Inno Setup bleibt vorerst bestehen für Legacy-Nutzer

**Phase 2 — AppInstaller-Distribution**
- `.appinstaller`-Datei auf GitHub Releases hosten
- Nutzer laden `.appinstaller` herunter → Windows übernimmt alle zukünftigen Updates automatisch
- Bestehenden In-App-Updater per `IsPackaged`-Check ausblenden

**Phase 3 — Microsoft Store**
- Gleiche MSIX-Datei bei Store einreichen
- Store-Version: `Windows.ApplicationModel.Package.Current` verfügbar → UpdateService komplett deaktivieren
- Store übernimmt Updates vollständig

### Code-Änderungen

**UpdateService.cs**: Guard hinzufügen
```csharp
// Nicht updaten wenn aus Store oder AppInstaller gestartet
if (IsRunningAsPackage()) return null;
```

**Erkennung ob App gepackt läuft:**
```csharp
private static bool IsRunningAsPackage()
{
    try
    {
        return Windows.ApplicationModel.Package.Current != null;
    }
    catch
    {
        return false;
    }
}
```

### Aufwand-Schätzung

| Schritt | Aufwand |
|---------|---------|
| MSIX Packaging Project | 1-2 Tage |
| AppInstaller-Datei + Hosting | 0.5 Tage |
| IsPackaged-Guard im UpdateService | 2h |
| Store-Einreichung (Zertifizierung) | 1-3 Wochen (Microsoft Review) |

---

## Fazit

**MSIX + AppInstaller** ist der einzige Ansatz, der:
1. Den gewünschten "Pre-Launch-Updater" nativ liefert
2. Keinen Framework-Lock-in schafft
3. Direkt zur Store-Einreichung führt (kein Umpacken nötig)

Inno Setup kann schrittweise abgelöst werden — kein Big Bang nötig.
