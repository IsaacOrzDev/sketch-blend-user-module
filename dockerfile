FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

# FROM mcr.microsoft.com/dotnet/runtime:7.0.10-bookworm-slim-amd64 as runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as runtime
WORKDIR /App
COPY --from=build-env /App/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "demo-system-user-module.dll"]