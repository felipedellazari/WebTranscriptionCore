#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-bionic AS base
WORKDIR /app
EXPOSE 80

ENV TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS build
WORKDIR /src
COPY ["WebTranscriptionCore/WebTranscriptionCore.csproj", "WebTranscriptionCore/"]
RUN dotnet restore "WebTranscriptionCore/WebTranscriptionCore.csproj"
COPY . .
WORKDIR "/src/WebTranscriptionCore"
RUN dotnet build "WebTranscriptionCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebTranscriptionCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
ENTRYPOINT ["dotnet", "WebTranscriptionCore.dll"]


