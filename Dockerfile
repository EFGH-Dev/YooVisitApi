# Étape 1 : L'image de base pour l'exécution (plus petite)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# On utilise l'utilisateur 'app' non-root créé par l'image de base de Microsoft, c'est plus sécurisé.
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Étape 2 : L'image de build (plus grosse, avec tous les outils)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# --- OPTIMISATION DU CACHE ---
# On copie d'abord uniquement le .csproj pour restaurer les dépendances.
# Cette étape ne changera que si tu ajoutes un paquet NuGet.
COPY ["YooVisitApi/YooVisitApi/YooVisitApi.csproj", "YooVisitApi/"]
RUN dotnet restore "./YooVisitApi/YooVisitApi.csproj"

# Maintenant, on copie le reste du code source.
COPY . .
WORKDIR "/src/YooVisitApi/YooVisitApi"
RUN dotnet build "YooVisitApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Étape 3 : La publication
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "YooVisitApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Étape 4 : L'image finale, légère et prête pour la production
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# On crée les dossiers de stockage et on donne les permissions
RUN mkdir -p /app/storage/avatars
RUN chown -R app /app/storage

ENTRYPOINT ["dotnet", "YooVisitApi.dll"]