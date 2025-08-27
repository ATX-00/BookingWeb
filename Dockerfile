# 1) Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /out

# 2) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./

# 讓 ASP.NET 監聽 Render 提供的 PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# 設定資料夾（由 Render 的磁碟掛載）
ENV DATA_DIR=/data
VOLUME ["/data"]

# 啟動
CMD ["dotnet","BookingWeb.dll"]
