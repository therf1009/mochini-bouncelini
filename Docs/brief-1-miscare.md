# Modul 1 — Mișcarea mochi-ului

**Folder:** `Assets/_Game/Scripts/Player/`
**Termen:** ora 14:00 din plan
**Depinde de:** nimic. Se poate lucra primul și independent.

## Scopul modulului

Mișcarea e fundația întregului joc. Dacă nu e plăcut să te miști prin arenă *fără* niciun obiectiv, niciun sistem de deasupra nu salvează jocul. Modulul ăsta trebuie să fie terminat și să se simtă bine înainte ca restul să conteze.

## Ce există deja

`MochiController.cs` conține scheletul funcțional:

- **Hop** — salt scurt, cu coyote time și jump buffering
- **Charge Launch** — ții apăsat, se comprimă, dai drumul, se lansează; puterea depinde de cât ai ținut
- **Wall Bounce** — ricoșează din pereți în loc să alunece
- squash & stretch pe vizual, gravitație asimetrică (cazi mai repede decât urci)

Fiecare verb e un `bool` public separat, pentru că Directorul le va scoate pe rând mai târziu în joc. Nu le uni într-un singur sistem.

## De făcut

**1. Reglarea feel-ului.** Valorile din Inspector sunt ghicite, nu testate. Asta e munca principală a modulului și nu se poate face citind cod — se face cu mâna pe tastatură, modificând o valoare și jucând din nou. Ținte concrete:

- un hop simplu trece peste **2 cuburi** înălțime
- un charge complet trece peste **5 cuburi**
- după un wall bounce păstrezi cam **80%** din viteză
- de la apăsare până la desprindere trebuie să nu se simtă nicio întârziere

Cele mai importante valori de reglat: `hopVelocity`, raportul dintre `gravityUp` și `gravityDown`, `coyoteTime`, și curba dintre `minLaunchSpeed` și `maxLaunchSpeed`.

**2. Evenimente publice**, ca restul echipei să se poată lega de mișcare fără să-ți modifice fișierul:

```csharp
public event System.Action<float> OnLand;        // viteza de impact
public event System.Action<float> OnLaunch;      // 0..1 cât de încărcat
public event System.Action OnWallBounce;
```

Modulul de cameră și cel de sunet se abonează la ele. Fără evenimente, ceilalți vor fi nevoiți să editeze `MochiController.cs` și veți avea conflicte la merge.

**3. Un HUD de debug** — un text în colț care arată ce verbe sunt active și `ChargeRatio`. Pare mărunt, dar economisește ore de reglaj, pentru că vezi instant ce face jocul.

## Capcane

- **Nu folosi `AddForce` pentru salturi.** Setează direct `linearVelocityY`. Forțele depind de masă și de timestep și fac saltul inconsistent.
- **Reglează la framerate stabil.** Cu VSync oprit, la 300 fps, feel-ul va fi altul decât în build-ul WebGL.
- **Nu adăuga `bounciness` pe materialul fizic.** Wall bounce-ul e calculat manual în cod; dacă îl dublezi cu fizica, devine imprevizibil.

## Gata înseamnă

Pui pe cineva care n-a văzut jocul să încerce. În zece secunde înțelege singur controalele, și după un minut tot se mai joacă — deși nu există niciun obiectiv, nicio grafică și niciun sunet.
