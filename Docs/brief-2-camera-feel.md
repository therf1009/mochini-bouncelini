# Modul 2 — Cameră & feel

**Folder:** `Assets/_Game/Scripts/Camera/`
**Termen:** ora 14:00 din plan
**Depinde de:** Modulul 1 (mișcare). Shake-ul se declanșează din evenimentele mișcării — aterizare, launch, wall bounce — deci are sens abia după ce controllerul există și se mișcă.

## Scopul modulului

Mișcarea de la Modulul 1 e corectă, dar „seacă”. Modulul ăsta adaugă stratul de feedback care face ca impactul să se *simtă*: screen shake proporțional cu viteza, hit-stop scurt la ciocniri, și coordonarea cu squash & stretch-ul care deja există pe vizual. E muncă de reglaj fin, nu de arhitectură — un impact care zguduie prea tare e la fel de rău ca unul care nu zguduie deloc.

## Ce există deja

`CameraShake.cs` conține sistemul complet:

- **Model de trauma** (0..1), apelabil static de oriunde: `CameraShake.Shake(0.2f)`. Apelurile se cumulează și se clampează la 1.
- **Perlin noise** pe două axe cu decay, și `trauma * trauma` — shake mai puțin zgomotos la intensități mici.
- Rulează pe **timp nescalat** (`Time.unscaledTime` / `unscaledDeltaTime`), deci funcționează și în timpul hit-stop-ului, când timpul jocului e oprit.
- Se pune pe un **părinte al camerei** (`CameraRig`), nu pe cameră, ca să nu se bată cu follow-ul sau cu rotația de 90°.

`MochiController.cs` îl apelează deja în trei locuri: la `Launch()`, la aterizare (`OnLand`, proporțional cu impactul) și la `OnCollisionEnter2D` (wall bounce). Hit-stop-ul (`hitStopTimer`) e tot în controller și e deja legat de aceleași momente.

## De făcut

**1. Reglarea intensității.** Valorile (`maxOffset`, `decay`, `frequency`) sunt ghicite. Joacă-te și ajustează până când un wall bounce „lovește” fără să dea greață, iar un hop simplu aproape nu mișcă nimic. Ținte:

- hop simplu → shake imperceptibil
- aterizare din înălțime mare → zguduire scurtă, clară
- wall bounce → cel mai satisfăcător impact din joc (cf. plan: „contra reușită primește cel mai satisfăcător sunet din joc” — aici e perechea vizuală)

**2. Treci pe evenimente.** Modulul 1 expune (sau trebuie să expună) `OnLand`, `OnLaunch`, `OnWallBounce`. Abonează-te la ele aici în loc ca mișcarea să cheme direct `CameraShake.Shake(...)`. Altfel, voi doi editați `MochiController.cs` în paralel și aveți conflicte la merge.

**3. Curbele de squash & stretch.** Trăiesc în `MochiController.UpdateVisual()`, dar sunt „feel”, deci sunt decizia ta vizuală. Ajustează `squashAmount` și `squashRecovery` împreună cu shake-ul, ca impactul vizual al mochi-ului și al camerei să fie pe același beat.

**4. Pregătește terenul pentru Cinemachine.** Follow-ul propriu-zis și rotația de 90° vin mai târziu. Nu le construi acum — doar păstrează shake-ul pe `CameraRig`, separat, ca să nu trebuiască rescris când se adaugă.

## Capcane

- **Shake pe părinte, nu pe cameră.** Dacă îl pui direct pe cameră, follow-ul și rotația Fez de mai târziu se vor bate cu offset-ul de shake.
- **Timp nescalat obligatoriu.** Dacă folosești `Time.deltaTime`, shake-ul îngheață în timpul hit-stop-ului — exact momentul în care ai cea mai mare nevoie de el.
- **Nu cumula trauma necontrolat.** Mai multe impacturi la rând trebuie clampate la 1, altfel camera sare din ecran.
- **WebGL, rezoluții mici.** Un `maxOffset` care arată bine în editor la 1080p poate fi uriaș într-un canvas mic de browser. Testează în build real.

## Gata înseamnă

Pui pe cineva să joace camera de test. O aterizare puternică și un wall bounce îl fac să zică „oh, nice” fără să-i explici nimic. Shake-ul dispare curat, nu rămâne camera tremurând, și nimic nu se simte ca greață — doar ca greutate.
