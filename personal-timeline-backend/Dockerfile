# Stage 1: Build the application
# Using .NET 8, as .NET 10 is not yet officially released.
# This allows us to build the project in a container.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the project file and restore dependencies
COPY personal-timeline-backend.csproj .
RUN dotnet restore "personal-timeline-backend.csproj"

# Copy the rest of the application files
COPY . .
WORKDIR "/source"
RUN dotnet publish "personal-timeline-backend.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 for the backend service
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# The database file will be created inside the container.
# A volume will be mounted via docker-compose to persist the database.
ENTRYPOINT ["dotnet", "personal-timeline-backend.dll"]
