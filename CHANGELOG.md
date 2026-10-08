# Değişiklik günlüğü

Sürümler `VERSION` (major.minor) + commit sayısı (patch) ile otomatik verilir; her `main` push'u bir sürümdür.
Burada yalnız kayda değer değişiklikler tutulur.

## 1.0
- İlk sürüm: ChaosModRDR'nin 187 efektinin tamamı C# ile (oyuncu 71, NPC 53, dünya 43, araç 18, meta 2).
- Entegrasyon yok: efektler yalnız menüden (F6) tıklanarak çalışır; isteğe bağlı otomatik mod (varsayılan kapalı).
- MHud menüsü (kategori, arama, Anlık/Süreli filtresi, önizleme, çalışan efektler, ayarlar) ve sağ üstte çalışan
  efektler listesi.
- Oyuncu modeli değiştiren efektler (inek, kuş, beden değiştirme…) runtime 1.1'in `ChangeModelPersistent` /
  `RestoreStoryModel` API'si ile; özellikler ve kıyafetler geri yüklenir.
- Onur efektleri deneysel (oyun sürümüne bağlı global), varsayılan kapalı.
