# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage compiles the Tailwind CSS bundle (needs Node/npm, which the .NET SDK image lacks).
FROM node:22-alpine AS tailwind
WORKDIR /src
COPY ["src/Hosts/Stocka.MVC/package.json", "src/Hosts/Stocka.MVC/package-lock.json", "src/Hosts/Stocka.MVC/"]
RUN cd src/Hosts/Stocka.MVC && npm ci
COPY ["src/Hosts/Stocka.MVC/src", "src/Hosts/Stocka.MVC/src"]
COPY ["src/Hosts/Stocka.MVC/Views", "src/Hosts/Stocka.MVC/Views"]
COPY ["src/Hosts/Stocka.MVC/Areas", "src/Hosts/Stocka.MVC/Areas"]
COPY ["src/Hosts/Stocka.MVC/wwwroot/js", "src/Hosts/Stocka.MVC/wwwroot/js"]
RUN cd src/Hosts/Stocka.MVC && npm run build


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Hosts/Stocka.MVC/Stocka.MVC.csproj", "src/Hosts/Stocka.MVC/"]
RUN dotnet restore "./src/Hosts/Stocka.MVC/Stocka.MVC.csproj"
COPY . .
# Use the CSS built in the tailwind stage; skip the in-container npm step.
COPY --from=tailwind ["/src/src/Hosts/Stocka.MVC/wwwroot/css/tailwind.css", "src/Hosts/Stocka.MVC/wwwroot/css/tailwind.css"]
WORKDIR "/src/src/Hosts/Stocka.MVC"
RUN dotnet build "./Stocka.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:SkipTailwindBuild=true

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Stocka.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:SkipTailwindBuild=true

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --retries=3 CMD curl --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Stocka.MVC.dll"]
