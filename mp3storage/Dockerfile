FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["mp3-storage/mp3-storage.csproj", "mp3-storage/"]
RUN dotnet restore "mp3-storage/mp3-storage.csproj"
COPY . .
WORKDIR "/src/mp3-storage"
RUN dotnet build "mp3-storage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mp3-storage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mp3-storage.dll"]
