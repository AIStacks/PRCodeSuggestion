# Stage 1: Build the .NET app and React app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js
RUN apt-get update && apt-get install -y curl
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash -
RUN apt-get install -y nodejs

# Install pnpm
RUN npm install -g pnpm

# Copy WebApp files and install dependencies
COPY /src/WebApp/package.json /src/WebApp/pnpm-lock.yaml* ./WebApp/
WORKDIR /src/WebApp
RUN pnpm install --frozen-lockfile
WORKDIR /src

# Copy the solution file and restore as distinct layers
COPY /src/*.sln .
COPY /src/WebApi/*.csproj ./WebApi/
COPY /src/WebApp/*.esproj ./WebApp/
RUN dotnet restore

# Copy everything else
COPY /src/. .

# Build the .NET app
RUN dotnet publish ./*.sln -c Release -o /app/publish

# Stage 2: Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 80

# Set the entry point for the application
ENTRYPOINT ["dotnet", "WebApi.dll"]
