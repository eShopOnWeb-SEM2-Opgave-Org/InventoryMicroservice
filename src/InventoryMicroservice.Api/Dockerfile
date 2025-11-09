FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App

COPY ./src/InventoryMicroservice.Api ./InventoryMicroservice.API
COPY ./src/InventoryMicroservice.Common ./InventoryMicroservice.Common
COPY ./src/InventoryMicroservice.Service ./InventoryMicroservice.Service
COPY ./src/InventoryMicroservice.Infrastructure ./InventoryMicroservice.Infrastructure
RUN dotnet restore ./InventoryMicroservice.API
RUN dotnet publish ./InventoryMicroservice.API -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
EXPOSE 8080
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "InventoryMicroservice.Api.dll"]

