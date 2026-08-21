# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project files to the build context. This step is necessary to ensure that the Docker build process has access to the necessary project files for building the application.
COPY ["src/PatientSurvey.Domain/PatientSurvey.Domain.csproj", "src/PatientSurvey.Domain/"]
COPY ["src/PatientSurvey.Application/PatientSurvey.Application.csproj", "src/PatientSurvey.Application/"]
COPY ["src/PatientSurvey.Infrastructure/PatientSurvey.Infrastructure.csproj", "src/PatientSurvey.Infrastructure/"]
COPY ["src/PatientSurvey.WebUI/PatientSurvey.WebUI.csproj", "src/PatientSurvey.WebUI/"]
# Restore the packages for the project. This step is necessary to download and install any dependencies specified in the project files.
RUN dotnet restore "src/PatientSurvey.WebUI/PatientSurvey.WebUI.csproj" 

# Copy the remaining source code to the build context. This step is necessary to ensure that the Docker build process has access to all the source code files needed for building the application.
COPY . .
# Build the project and publish the output to the /app/publish directory. This step compiles the application and prepares it for deployment.
RUN dotnet publish "src/PatientSurvey.WebUI/PatientSurvey.WebUI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PatientSurvey.WebUI.dll"]

