FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BackendApplication.Api/BackendApplication.Api.csproj BackendApplication.Api/
RUN dotnet restore BackendApplication.Api/BackendApplication.Api.csproj

COPY . .
RUN dotnet publish BackendApplication.Api/BackendApplication.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/app.db"

RUN mkdir -p /app/data

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BackendApplication.Api.dll"]
