FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Planner.BlazorApp/Planner.BlazorApp.csproj", "Planner.BlazorApp/"]
RUN dotnet restore "Planner.BlazorApp/Planner.BlazorApp.csproj"
COPY . .
WORKDIR "/src/Planner.BlazorApp"
RUN dotnet publish "Planner.BlazorApp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Planner.BlazorApp.dll"]
