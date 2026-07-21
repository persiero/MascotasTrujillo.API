FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MascotasTrujillo.API/MascotasTrujillo.API.csproj MascotasTrujillo.API/
RUN dotnet restore MascotasTrujillo.API/MascotasTrujillo.API.csproj

COPY MascotasTrujillo.API/ MascotasTrujillo.API/
RUN dotnet publish MascotasTrujillo.API/MascotasTrujillo.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MascotasTrujillo.API.dll"]