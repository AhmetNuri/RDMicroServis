FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/AnalyticsService/AnalyticsService.csproj src/AnalyticsService/
RUN dotnet restore src/AnalyticsService/AnalyticsService.csproj
COPY . .
RUN dotnet publish src/AnalyticsService/AnalyticsService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AnalyticsService.dll"]
