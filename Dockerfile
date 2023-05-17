FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY . /app

# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0.16-bullseye-slim-arm64v8
RUN apt-get update
WORKDIR /app
COPY --from=build-env /app/out .

# Copy binaries
COPY ./binaries .

# Run the app on container startup
ENTRYPOINT ["dotnet", "DiscordBot.dll"]




