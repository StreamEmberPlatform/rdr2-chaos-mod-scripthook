# rdr2-chaos-mod-scripthook (StreamEmber Chaos Mod, RDR2) — ajan notları

Önce `README.md` okunur.

- ChaosModRDR'nin (clixff, GPL-3.0) C# portudur; efekt kimlikleri (`id`) orijinalle aynı kalır (sayfa ve ileride
  EventFabric bu kimliklerle çağırır). Yeni efekt = `src/Effects/*Effects.cs` içinde `r.Instant` / `r.Timed`.
- Bu sürümde entegrasyon ve efekt kısayol tuşu yok: tek tuş (`MenuKey`, varsayılan F6) menüyü açar. Efektler menüden,
  otomatik moddan ya da Total Chaos'tan çalışır. Dış kaynak `ChaosEngine.Run(id, kaynak)` çağırır.
- Her efekt etkinleştirmede yeni bir örnekle başlar (`EffectDef.Create`); durum alanları/closure içinde tutulur.
  Süreli efektin `Stop`'u kamera, hava, timecycle, ölçek gibi her değişikliği geri almalıdır.
- Aynı `Group`'taki efektler birbirini durdurur (`model`, `camera`, `sky`, `speed`).
- Tick'te çalışan her parça `Guard.Run` ile sarılır; efekt hatası yalnız o efekti etkiler (ChaosMod.log).
- Native'ler yalnız scriptin Tick/KeyDown fiber'ından çağrılır. Yakın varlıklar `Fx.NearbyPeds/NearbyVehicles`
  (runtime 1.1 `Ped.GetNearbyPeds(max)`, itemset yok) ile alınır; `World.GetAll*` her kare çağrılmaz.
- Script global'i yazan efektler `Experimental = true` olmalı ve varsayılan kapalı kalır.
- Kullanıcıya görünen metinler Türkçe; kod, tanımlayıcılar ve kod yorumları İngilizce.
- `src/Common/Json.cs` ve `Ui.cs` trainer repolarındakiyle aynıdır; `tools/StreamEmber.Build.psm1` tüm StreamEmber
  repolarında aynı dosyadır.
- Paket: `StreamEmber\Scripts\StreamEmber.ChaosMod.RDR2.dll`, `StreamEmber\Config\ChaosMod.ini`, manifest. Sayfa
  (`web/`) GitHub Pages'te yayınlanır; MHud kiti jsDelivr'den.
