# Build stage
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ConfigurationManager.Web/ConfigurationManager.Web.csproj", "MVCApp/"]
RUN dotnet restore "ConfigurationManager.Web/ConfigurationManager.Web.csproj"
COPY . .
WORKDIR "/src/ConfigurationManager.Web"
RUN dotnet build "ConfigurationManager.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ConfigurationManager.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConfigurationManager.Web.dll"]