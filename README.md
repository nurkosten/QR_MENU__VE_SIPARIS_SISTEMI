# QR Kodlu Restoran Menü ve Sipariş Sistemi

Bu depo, `QR_Restoran_Menu_Siparis_Sistemi_Detayli_Proje_Dosyasi.docx` içindeki analiz, mimari ve fonksiyonel gereksinimlerle paralel geliştirilmiş ASP.NET Core MVC uygulamasıdır.

## Amaç

Müşteri masadaki QR kodu okutur, dijital menüyü görür ve sipariş verir. Sipariş sunucuda doğrulanır, masaya bağlanır, personel ve mutfak ekranlarına düşer.

## Teknoloji

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity (roller)
- Katmanlar: `Entities` · `DataAccess` · `Business` · `WebUI` · `Tests`

## Çözümü açma

Visual Studio veya Cursor ile `RestaurantMenu.slnx` dosyasını açın. Proje belgesi çözümün **Dokumanlar** klasöründedir.

## Çalıştırma

1. `RestaurantMenu.WebUI/appsettings.json` içinde SQL Server bağlantısını düzenleyin (`Server=Nur` varsayılan).
2. `RestaurantMenu.WebUI` projesini çalıştırın. İlk açılışta migration ve demo veri yüklenir.
3. Tarayıcı: http://localhost:5265 — müşteri menüsü (Masa 8).

### Demo hesaplar

| Rol | E-posta | Şifre |
|-----|---------|--------|
| Admin | admin@restaurant.local | Admin123! |
| Personel | personel@restaurant.local | Personel123! |
| Mutfak | mutfak@restaurant.local | Mutfak123! |

## Roller (proje dosyası §4)

| İşlem | Müşteri | Personel | Mutfak | Admin |
|--------|---------|----------|--------|-------|
| Menü | evet | evet | evet | evet |
| Sepet / sipariş | evet | evet | hayır | evet |
| Durum değiştirme | hayır | evet | evet | evet |
| Kategori / ürün / masa / QR / kullanıcı | hayır | hayır | hayır | evet |
| Rapor | hayır | sınırlı (günlük özet) | hayır | evet |
| Garson çağrısı | oluşturur | yönetir | hayır | evet |

## Fonksiyonel karşılık (FR)

| ID | Gereksinim | Uygulama |
|----|------------|----------|
| FR-01 | Giriş / yetki | `AccountController` + Identity |
| FR-02 | İşletme bilgisi | Admin → İşletme |
| FR-03 | Kategori | Admin → Kategoriler (`DisplayOrder`, aktif/pasif) |
| FR-04 | Ürün | Admin → Ürünler (fiyat, görsel, satış durumu) |
| FR-05 | Masa | Admin → Masalar |
| FR-06 | QR | Benzersiz `QrToken`, PNG / yazdır |
| FR-07 | Dijital menü | `/menu/{restaurantToken}/{tableToken}` |
| FR-08 | Arama / filtre | Menü arama ve kategori chip |
| FR-09 | Sepet | Session sepet, adet, not, silme |
| FR-10 | Sipariş | QR doğrulama + transaction |
| FR-11 | Durumlar | Yeni → Onaylandı → Hazırlanıyor → Hazır → Servis → Tamamlandı / İptal |
| FR-12 | Mutfak | Onaylı ve hazırlanan sipariş kuyruğu |
| FR-13 | Garson çağır | `ServiceRequest` |
| FR-14 | Hesap iste | `ServiceRequest` |
| FR-15 | Rapor | Günlük / haftalık / aylık |

## Sipariş kuralları (proje dosyası §11–12)

- Fiyat istemciden alınmaz; veritabanındaki `decimal` fiyat kullanılır.
- `Order` ve `OrderItem` aynı transaction içinde yazılır.
- Satırda `ProductNameSnapshot` + `UnitPrice` saklanır; ürün fiyatı sonradan değişse geçmiş bozulmaz.
- Durum atlama `OrderStatusMachine` ile reddedilir.
- Müşteri siparişi **Yeni** kaydedilir; mutfak yeni ve onaylı kuyruğu görür. Personel onaylar, mutfak hazırlar.

## QR güvenliği (proje dosyası §9)

URL: `/menu/{restaurantToken}/{tableToken}`

Sunucu tokenı arar, masa ve işletmenin aktif olduğunu ve eşleştiğini doğrular. Tahmin edilebilir `TableId` kullanılmaz.

## Test

```bash
dotnet test RestaurantMenu.Tests
```

Kritik senaryolar: geçersiz QR, pasif masa, pasif ürün, sıfır adet, sunucu fiyatı, durum atlama, mutfak kuyruğu, garson talebi.

## Kapsam dışı (P2 / sonraki sürüm)

Online ödeme, SignalR zorunluluğu, çoklu şube, stok otomasyonu, kupon, rezervasyon, yazıcı entegrasyonu.

## 20 günlük plan özeti

Gün 1–4 iskelet ve Identity, 5–9 menü/masa/QR, 10–12 sepet/sipariş, 13–15 personel/mutfak/müşteri durum, 16–17 rapor ve güvenlik, 18–20 uçtan uca test ve teslim.
