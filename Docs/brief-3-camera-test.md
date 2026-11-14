# Modul 3 — Cameră de test & integrare

**Folder:** `Assets/_Game/Scripts/Level/Editor/`
**Termen:** ora 14:00 din plan
**Depinde de:** Modulul 1 (mișcare) și Modulul 2 (cameră & feel). E pasul de integrare — pune totul într-o scenă jucabilă.

## Scopul modulului

Până acum, mișcarea și feel-ul există ca scripturi, dar nu ai unde să le joci. Modulul ăsta e unealta care asamblează scena de test cu un singur clic de meniu: cameră de cuburi albe, mochi cu controller, cameră ortografică cu shake. E și prototipul uneltei reale de construit camere, care devine prioritatea #1 de la ora 18:00.

## Ce există deja

`TestRoomBuilder.cs` e un script de **Editor** (meniu `NERFED > Build Test Room`) care generează de la zero:

- scena `Assets/_Game/Scenes/TestRoom.unity` + o adaugă în Build Settings;
- material alb (`M_White`) și material fizic fără frecare și fără bounce (`PM_Mochi`) — wall bounce-ul e manual în cod, nu din fizică;
- lumină direcțională + `CameraRig` ortografic cu `CameraShake` (Modulul 2) și `Main Camera`;
- camera de cuburi: podea, tavan, pereți, platforme la înălțimi alese ca să testeze fiecare verb, un pilon central pentru wall bounce, și 3 cuburi unitare ca referință de scară;
- player-ul `Mochi`: `Rigidbody2D` + `CircleCollider2D` + vizual sferă + `MochiController` (Modulul 1), deja legat.

Notă de arhitectură care contează mai târziu: fizica 2D ignoră complet poziția pe Z, deci colliderele cuburilor sunt deja „colapsate în adâncime” — exact comportamentul de care avem nevoie pentru rotația în stil Fez.

## De făcut

**1. Rulează și verifică.** `NERFED > Build Test Room`, apasă Play. A/D sau săgeți = deplasare, SPACE scurt = hop, SPACE ținut = charge launch. Trebuie să se joace imediat.

**2. Potrivește geometria la țintele de feel** din brief-1: un hop trece peste 2 cuburi, un charge complet peste 5. Dacă platformele nu se potrivesc, mută-le aici, nu reglează mișcarea după cameră.

**3. Primul build WebGL pe itch.io ca Draft.** Regula din plan e clară: build la 18:00, nu la 05:00 — Unity WebGL surprinde mereu, iar un proiect nepublicat nu intră în evaluare. Scena trebuie să existe acum ca build-ul de la 18:00 să aibă ce împacheta.

**4. Pregătește tranziția spre unealta reală.** Acum camera e hardcodată în cod. Pasul următor (de la 18:00) e un prefab de cub așezat pe grid cu snapping, ca o cameră să coste 10 minute, nu o oră. Nu o construi acum — doar nu te baza pe pozițiile hardcodate ca fiind finale.

## Capcane

- **E cod de Editor** (în folderul `Editor/`), deci nu intră în build — corect, așa trebuie să rămână.
- **Deschide Unity o dată înainte de commit**, ca să se genereze fișierele `.meta`. Dacă dai commit la `.cs` fără `.meta`, GUID-urile se regenerează pe alt calculator și referințele se pot rupe.
- **Cameră ortografică, nu perspectivă.** În ortografic, un bloc din adâncime arată ca un pătrat plat — baza iluziei Fez. Cu perspectivă, trucul cade.
- **Nu pune `bounciness` pe materialul fizic.** Wall bounce-ul e în cod; dublat cu fizica devine imprevizibil.

## Gata înseamnă

Oricine din echipă apasă `NERFED > Build Test Room`, dă Play, și în zece secunde se plimbă cu mochi-ul printr-o cameră de cuburi albe care se mișcă și se simte bine. Asta e exact milestone-ul de la 14:00.
