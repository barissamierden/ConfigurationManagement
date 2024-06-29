# Dinamik Konfigürasyon Yapısı Projesi
## Proje Hakkında
Bu proje, web.config, app.config gibi dosyalarda tutulan appkey’lerin ortak ve dinamik bir yapıyla erişilebilir olmasını sağlamayı amaçlamaktadır. Proje, konfigürasyon kayıtlarını MongoDB'de tutar ve Redis'te cacheler. MongoDB erişilemez olduğunda cachelenmiş veriler kullanılır. Konfigürasyon kayıtları web arayüzü üzerinden yönetilebilir.

## Teknolojiler
- .NET5
- ASP.NET Core MVC
- MongoDB
- Redis
- Docker

## Kullanım
### Ön Koşullar
- Docker ve Docker Compose yüklü olmalıdır.
- .NET 5 SDK yüklü olmalıdır.

### Adımlar
1. Proje Yapılandırmasını Kontro Edin:
   - `appsettings.json` dosyasındaki MongoDB bağlantı bilgilerini kontrol edin
   - Dockerfile, docker-compose.yml, mongo-init.js dosyalarının doğru yapılandırıldığından emin olun.

2. Projeyi Çalıştırma:
  Proje kök dizininde terminal/komut satırı açın ve aşağıdaki komutu çalıştırın:
  `docker-compose -f docker-compose.yml up -d`
  Bu komut, Docker Compose dosyasında tanımlanan tüm servisleri (MongoDB, Redis ve MVC uygulaması) başlatacaktır.

3. Web Arayüzüne Erişim:
   Docker Compose ile tüm servisler başladıktan sonra, tarayıcıda http://localhost:5000 adresine gidin ve açılan sayfadaki `Configuration Management` butonuna tıklayın. Açılacak olan sayfadan konfigürasyon kayıtlarını yönetebilirsiniz.

## Ekstra Notlar
- Docker Compose kullanarak tüm servisler otomatik olarak başlatılacak ve yapılandırılacaktır.
- Web arayüzü üzerinden konfigürasyon kayıtlarını ekleyebilir, silebilir, güncelleyebilir ve listeleyebilirsiniz.
- MVC projesini Docker dışında ayağa kaldırmak isterseniz, `mongodb://mongodb:27017` olan bağlantı bilgisini `mongodb://localhost:27017` olarak, `redis:6379` olanı ise `localhost:6379` olarak değiştirmelisiniz.