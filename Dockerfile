FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/NphiesSandbox.Api/NphiesSandbox.Api.csproj src/NphiesSandbox.Api/
RUN dotnet restore src/NphiesSandbox.Api/NphiesSandbox.Api.csproj
COPY . .
RUN dotnet publish src/NphiesSandbox.Api/NphiesSandbox.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NphiesSandbox.Api.dll"]