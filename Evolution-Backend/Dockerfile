#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-1903 AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build
#WORKDIR /src
#COPY ["Evolution-Backend/Evolution-Backend.csproj", "Evolution-Backend/"]
#RUN dotnet restore "Evolution-Backend/Evolution-Backend.csproj"
#COPY . .
#WORKDIR "/src/Evolution-Backend"
#RUN dotnet build "Evolution-Backend.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "Evolution-Backend.csproj" -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Evolution-Backend.dll"]

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-1903 AS base
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build
#
#WORKDIR /app
#COPY ./bin/Debug/netcoreapp3.1 /app
#
## Expose port 80 for the Web API traffic
#ENV ASPNETCORE_URLS http://+:80
#EXPOSE 80
 #
## Run the dotnet application against a DLL from within the container
## Don't forget to publish your application or this won't work
#ENTRYPOINT ["dotnet", "Evolution-Backend.dll"]

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-nanoserver-1903 AS base
#WORKDIR /api
#EXPOSE 80
#EXPOSE 443
#
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1903 AS build
#WORKDIR /src
#COPY *.csproj ./
#RUN dotnet restore
#COPY . ./
#RUN dotnet publish -c Release -o /api/publish
#
#FROM base AS final
#WORKDIR /api
#COPY --from=build /api/publish .
#ENTRYPOINT ["dotnet", "Evolution-Backend.dll"]

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /api/evolution

COPY ./bin/Release/netcoreapp3.1/publish ./

ENV ASPNETCORE_URLS http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Evolution-Backend.dll"]