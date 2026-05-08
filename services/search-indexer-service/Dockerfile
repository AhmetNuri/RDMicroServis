FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/SearchIndexerService/SearchIndexerService.csproj src/SearchIndexerService/
RUN dotnet restore src/SearchIndexerService/SearchIndexerService.csproj
COPY . .
RUN dotnet publish src/SearchIndexerService/SearchIndexerService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SearchIndexerService.dll"]
