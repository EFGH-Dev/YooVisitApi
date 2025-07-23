# Étape de base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Étape de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copier le fichier projet et restaurer les dépendances
COPY ["YooVisitApi/YooVisitApi/YooVisitApi.csproj", "YooVisitApi/"]
RUN dotnet restore "./YooVisitApi/YooVisitApi.csproj"

# Copier le reste des fichiers
COPY . .

# Créer le répertoire de build et définir les permissions
RUN mkdir -p /app/build && chown -R $APP_UID /app/build

# Construire le projet
WORKDIR "/src/YooVisitApi/YooVisitApi"
RUN dotnet build "./YooVisitApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Étape de publication
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YooVisitApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Étape finale
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés
COPY --from=publish /app/publish .

# Créer le répertoire de stockage et définir les permissions
RUN mkdir -p /app/storage/avatars && chown -R $APP_UID /app/storage

ENTRYPOINT ["dotnet", "YooVisitApi.dll"]
