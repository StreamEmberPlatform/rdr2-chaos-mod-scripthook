# StreamEmber Chaos Mod (RDR2)

Red Dead Redemption 2 için kaos modu: [ChaosModRDR](https://github.com/clixff/ChaosModRDR)'nin (clixff) **187 efektinin
tamamı** C# ile, StreamEmber Runtime üzerinde çalışan bir script (`StreamEmber.ChaosMod.RDR2.dll`) ve StreamEmber
Overlay'de açılan bir MHud menüsü. Menü oyuna kurulmaz: bu reponun `web/` klasörü GitHub Pages'te yayınlanır
(**https://streamemberplatform.github.io/rdr2-chaos-mod-scripthook/**), MHud kiti jsDelivr CDN'inden gelir
(`@streamemberplatform/mhud@1.3.0`).

```text
RDR2.exe
 ├─ StreamEmber.Runtime.RDR2.asi ─► StreamEmber\Scripts\StreamEmber.ChaosMod.RDR2.dll   (bu repo, src/)
 │                                     │ OverlayBridge.LoadUrl(sayfa) · Send/TryReceive (menü mesajları)
 └─ StreamEmber.Overlay.RDR2.asi  ─► StreamEmber\Overlay\ (CEF) ─► https://streamemberplatform.github.io/rdr2-chaos-mod-scripthook/
```

## Kullanım

- **F6** menüyü açar/kapatır (`ChaosMod.ini` → `MenuKey`). Menü açıkken fare ve klavye sayfaya gider.
- Soldan kategori (Oyuncu, NPC, Dünya, Araç, Meta), ortadan efekt; **Çalıştır**, **Enter** ya da **çift tık**.
  Arama kutusu bütün kategorilerde arar; *Anlık / Süreli* filtresi var.
- Süreli bir efekt zaten çalışıyorsa yeniden çalıştırmak süresini yeniler; önizlemede **Durdur** ile bitirilir.
- **Çalışanlar**: süreli efektler, kalan süre, tek tek ya da hepsini durdurma.
- **Ayarlar**: otomatik mod (her N saniyede rastgele efekt, varsayılan kapalı), aralık, deneysel efektler,
  oluşturulanları temizle, hepsini durdur.
- Sağ üstte çalışan efektler ve kalan süreleri görünür; her efekt bir bildirimle başlar.
- Bu sürümde **entegrasyon yok** (Twitch/TikTok/EventFabric) ve efekt kısayol tuşu yok. EventFabric bağlantısı ileride
  `ChaosEngine.Run(id, "eventfabric")` ile yapılacak; efekt kimlikleri orijinal modla aynıdır.

## Efektler

| Kategori | Adet | Örnekler |
|---|---|---|
| Oyuncu | 71 | Havaya fırlat, Sarhoş, İnek/Kuş/Fare oldun, Beden değiştir, Yerçekimi tabancası, Kuşbakışı kamera, Sahte ışınlanma |
| NPC | 53 | Vampir, Öfkeli ikiz, Undead Nightmare, Parti zamanı, Herkes Lenny, Domuz çiftçileri kaçırdı, Sonunda uyandın |
| Dünya | 43 | Kar fırtınası, Kıyamet, Deprem, UFO, Gökten domuz yağıyor, Ağır çekim, Ters kamera, FOV 120 |
| Araç | 18 | Posta arabası, Tam gaz, Tekerlekler gitti, At yağmuru, Petrol vagonu yağmuru, Bütün atlar eşek |
| Meta | 2 | Total Chaos (3 dk boyunca her 15 sn'de rastgele efekt), Combo Time (her efekt iki efekt daha getirir) |

Orijinalden farklar:

- Efektler yalnız menüden tetiklenir; orijinaldeki F7/F8/F10/F12 tuşları ve 45 sn'lik otomatik zamanlayıcı yok
  (otomatik mod ayarlardan açılabilir).
- Oyuncu modelini değiştiren efektler runtime 1.1'in `Player.ChangeModelPersistent` / `RestoreStoryModel` API'sini
  kullanır: hikâye global'leri yalnız beklenen değeri tutuyorsa yazılır, efekt bitince özellikler, Dead Eye seviyesi ve
  kıyafetler geri yüklenir.
- **Onur** efektleri (iyi, kötü, sıfırla, rastgele) oyun sürümüne bağlı script global'lerine yazar (1.0.1311 / 1436
  için bulunmuş, güncel sürümde doğrulanmadı): **deneysel**, varsayılan kapalı.
- Aynı türden efektler birbirini durdurur (oyuncu modeli, script kamerası, gökyüzü/hava, oyun hızı) ki birinin
  kapanışı diğerinin ayarını bozmasın. Oyuncu ölünce bütün efektler durur.
- *İzleyici NPC'si* bu sürümde rastgele bir adla doğar; entegrasyonda gerçek izleyici adı gelecek.

## Oyun klasöründeki düzen

| Dosya | Görev |
|---|---|
| `StreamEmber\Scripts\StreamEmber.ChaosMod.RDR2.dll` | Kaos modu scripti |
| `StreamEmber\Config\ChaosMod.ini` | `UiUrl`, `MenuKey` (F6), `Theme`, `AutoMode`, `AutoInterval`, `Experimental`. Güncellemede korunur |
| `StreamEmber\Logs\ChaosMod.log` | Log (her oturumda yeniden başlar, öncekisi `ChaosMod.previous.log`; hatalar sınırlı sayıda yazılır) |
| `StreamEmber\Manifests\StreamEmber.ChaosMod.RDR2.json` | Paket manifest'i: sürüm, commit, bağımlılıklar, dosyalar ve SHA-256 değerleri |

Gereken: RDR2 **DirectX 12** + ScriptHookRDR2 (`dinput8.dll`) + [StreamEmber Runtime (RDR2)](https://github.com/StreamEmberPlatform/rdr2-runtime-scripthook)
**1.1 veya üstü** + [StreamEmber Overlay](https://github.com/StreamEmberPlatform/ui-runtime). Paket bunları içermez.

## Çökmeye karşı

- Her efektin başlangıcı, her karesi ve bitişi ayrı korunur: hata veren efekt yalnız kendini düşürür, `ChaosMod.log`'a
  yazılır; script durmaz.
- Efektlerin yarattığı NPC/araç/nesneler takip edilir; süreli efektler bitişte kendi yarattıklarını siler, menüden
  hepsi temizlenebilir.
- Overlay yoksa script bir kez uyarır; menü olmadan bekler.

## Derleme

```powershell
.\build.ps1                                   # derle + paketle (dist\RDR2, artifacts\*.zip)
.\build.ps1 -Deploy -GamePath "D:\...\Red Dead Redemption 2"
```

Başvurular (pakete girmez): `StreamEmber.Scripting.RDR2.dll` ve `StreamEmber.Overlay.Bridge.dll`. Yerelde kardeş
repolardan (`..\rdr2-runtime-scripthook\bin\Release`, `..\ui-runtime\build\managed`), CI'da o repoların son
release'lerinden alınır. Sayfayı tarayıcıda denemek için: `web/index.html?demo=1`.

GitHub Actions: her push derlenir; `main`'e her push bir release (zip + sha256) ve sayfanın GitHub Pages yayını olur.
Sayfa için bir kez: *Settings > Pages > Build and deployment > Source = "GitHub Actions"*.

## Lisans

ChaosModRDR'den türetildiği için **GPL-3.0** (`LICENSE`). Efektlerin davranışı, model/silah/hava listeleri ve script
global adresleri ChaosModRDR'den (© clixff ve katkıda bulunanlar) alınmıştır. MHud: MIT (`web/MHUD-LICENSE.txt`).
