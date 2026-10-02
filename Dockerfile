FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching dotnet restore
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Api/Api.csproj", "Api/"]

RUN dotnet restore "Api/Api.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/Api"
RUN dotnet publish "Api.csproj" -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

# إنشاء مستخدم غير root
RUN adduser --disabled-password --gecos "" appuser && \
    chown -R appuser /app
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "lab8.dll"]