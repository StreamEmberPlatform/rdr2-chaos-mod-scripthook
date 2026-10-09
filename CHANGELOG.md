# Değişiklik günlüğü

Sürümler `VERSION` (major.minor) + commit sayısı (patch) ile otomatik verilir; her `main` push'u bir sürümdür.
Burada yalnız kayda değer değişiklikler tutulur.

## 1.0 (düzeltmeler)
- **Menü sayfası artık modla birlikte kuruluyor** (`StreamEmber\UI\ChaosMod\`) ve oyun klasöründen açılıyor; GitHub
  Pages kaldırıldı. Yalnız MHud kitinin css/js dosyaları jsDelivr CDN'inden. `ChaosMod.ini` → `UiUrl` boşsa yerel sayfa.
- **Düzeltme:** menü açılıyor gibi olup hiçbir şey görünmüyordu. Overlay'de tek sayfa ve tek mesaj kuyruğu var; trainer
  da kuruluyken kaos modu trainer sayfasının `ready` mesajını alıp menüyü trainer sayfasında açmaya çalışıyordu. Artık
  her mod yalnız kendi sayfası açıkken mesaj okur/gönderir; F6 gerekirse kaos sayfasını yükler, sayfa `ready`
  dediğinde menüyü açar. 10 sn içinde cevap gelmezse (sayfa yayında değil / internet yok) uyarır ve önceki sayfayı
  geri yükler: yayında olmayan sayfa GitHub'ın opak 404 sayfasıydı ve oyun ekranını kapatıyordu.
- F6 artık `GetAsyncKeyState` ile okunuyor (UI girdi modunda da kapanır); sayfa F6'yı ayrıca işlemiyor.
- Sayfa `ready` mesajında kendini tanıtıyor (`{ app: 'chaos' }`).

## 1.0
- İlk sürüm: ChaosModRDR'nin 187 efektinin tamamı C# ile (oyuncu 71, NPC 53, dünya 43, araç 18, meta 2).
- Entegrasyon yok: efektler yalnız menüden (F6) tıklanarak çalışır; isteğe bağlı otomatik mod (varsayılan kapalı).
- MHud menüsü (kategori, arama, Anlık/Süreli filtresi, önizleme, çalışan efektler, ayarlar) ve sağ üstte çalışan
  efektler listesi.
- Oyuncu modeli değiştiren efektler (inek, kuş, beden değiştirme…) runtime 1.1'in `ChangeModelPersistent` /
  `RestoreStoryModel` API'si ile; özellikler ve kıyafetler geri yüklenir.
- Onur efektleri deneysel (oyun sürümüne bağlı global), varsayılan kapalı.
