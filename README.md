# التمرين 5: ASP.NET Core Web API مع Docker

مشروع عملي لبناء **ASP.NET Core 8 Web API** حقيقي يتواصل مع المتصفح ويعمل داخل حاوية **Docker**.

---

## 📁 هيكل المشروع (Project Structure)

```text
D:\docker_stady\
├── .dockerignore       # استبعاد ملفات bin و obj لتسريع البناء وتجنب التضارب
├── Dockerfile          # ملف بناء الحاوية متعدد المراحل (Multi-stage) مع كاش الطبقات
├── lab5.csproj         # ملف المشروع (.NET 8 Web API) ينتج lab5.dll
├── Program.cs          # كود الـ API ومفعل فيه Swagger دائمًا للعرض في المتصفح
└── README.md           # دليل التشغيل والشرح
```

---

## 🐳 شرح ملف الـ Dockerfile

```dockerfile
# المرحلة الأولى: بناء المشروع وتجهيز الملفات (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ⚡ تحسين التخزين المؤقت (Docker Layer Caching):
# نسخ ملفات .csproj فقط وتشغيل dotnet restore
# لو عدلت كود C# لاحقاً، دوكر مش هيعيد تحميل الحزم (packages) وهيستخدم الكاش فوراً!
COPY *.csproj .
RUN dotnet restore

# نسخ باقي الكود ونشره في مجلد /out
COPY . .
RUN dotnet publish -c Release -o /out

# المرحلة الثانية: التشغيل (Runtime Stage)
# بنستخدم صورة aspnet لأنها تحتوي على خادم الويب Kestrel ومكتبات ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# توثيق أن الحاوية تستمع على المنفذ 8080 (الافتراضي في .NET 8)
EXPOSE 8080

ENTRYPOINT ["dotnet", "lab5.dll"]
```

---

## 🚀 طريقة البناء والتشغيل (How to Run)

افتح موجه الأوامر (PowerShell أو Terminal) في المسار `D:\docker_stady`:

### 1. بناء صورة الدوكر (Build Image):
```powershell
docker build -t lab5-api .
```

### 2. تشغيل الحاوية (Run Container):
```powershell
docker run -d -p 8081:8080 --name my-api-container lab5-api
```
> [!NOTE]
> استخدمنا المنفذ `8081` على جهازك وربطناه بالمنفذ `8080` داخل الحاوية (`-p 8081:8080`) لأن المنفذ 8080 قد يكون مستخدماً بواسطة حاوية أخرى لديك.

### 3. فتح التطبيق في المتصفح (Browser):
افتح متصفحك واذهب إلى:
- **واجهة Swagger التفاعلية**: [http://localhost:8081/swagger](http://localhost:8081/swagger) (أو [http://localhost:8081](http://localhost:8081))
- **بيانات الطقس (API)**: [http://localhost:8081/weatherforecast](http://localhost:8081/weatherforecast)
- **معلومات الحاوية والنظام**: [http://localhost:8081/api/info](http://localhost:8081/api/info)

---

## 🛑 إيقاف وحذف الحاوية بعد التجربة:
```powershell
docker stop my-api-container
docker rm my-api-container
```
