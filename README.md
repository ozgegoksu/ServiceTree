# ServiceMap — ServiceTreeDemo

Kurumunuzdaki dijital servisleri ve aralarındaki bağımlılıkları görsel olarak haritalayan Blazor Server / .NET 8 uygulaması.

---

## Yerel Geliştirme (Docker olmadan)

### Gereksinimler
- .NET 8 SDK

### Çalıştırma

```bash
dotnet run
```

Uygulama varsayılan olarak `https://localhost:5001` ve `http://localhost:5000` adreslerinde açılır.  
SQLite veritabanı `data/servicemap.db` dosyasına otomatik oluşturulur.

---

## Docker ile Çalıştırma

### Gereksinimler
- Docker Desktop (Windows / macOS) veya Docker Engine (Linux)

### 1. Image Build

Proje kök dizininde aşağıdaki komutu çalıştırın:

```bash
docker build -t servicetreedemo .
```

### 2. Çalıştırma (geçici — container silinince veri kaybolur)

```bash
docker run -p 8080:8080 servicetreedemo
```

Tarayıcıda açın: [http://localhost:8080](http://localhost:8080)

### 3. Çalıştırma (kalıcı veri — önerilen)

SQLite veritabanının container yeniden başlatmalarında korunması için bir Docker volume kullanın:

```bash
docker run -p 8080:8080 -v servicetree_data:/app/data servicetreedemo
```

Volume bir kez oluşturulur ve container silinse bile veri korunur.

### 4. Arka planda (detached) çalıştırma

```bash
docker run -d -p 8080:8080 -v servicetree_data:/app/data --name servicemap servicetreedemo
```

Durdurmak için:

```bash
docker stop servicemap
docker rm servicemap
```

---

## Özellikler

| Özellik | Açıklama |
|---|---|
| 🗂️ Proje yönetimi | Proje oluşturma, düzenleme ve silme |
| ⚙️ Servis haritası | Drag & drop ile konumlandırma |
| 🔗 Bağlantılar | REST, SOAP, MQ, gRPC, JAR, Anasistem, UI tiplerinde bağlantı |
| 📡 Health Check | Canlı URL bazlı durum kontrolü |
| 🟢🔴 Status | Active / Down durum desteği |
| ⬇️ Export | SVG ve PNG dışa aktarma |
| 🌙 Tema | Açık / koyu mod |

---

## Teknik Bilgiler

- **Framework:** Blazor Server (.NET 8)
- **Veritabanı:** SQLite (EF Core)
- **Port:** 8080 (Docker), 5000/5001 (lokal)
- **Veri dizini:** `/app/data/servicemap.db`
