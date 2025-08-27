# 1) Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /out

# 2) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./

# �� ASP.NET ��ť Render ���Ѫ� PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# �]�w��Ƨ��]�� Render ���Ϻб����^
ENV DATA_DIR=/data
VOLUME ["/data"]

# �Ұ�
CMD ["dotnet","BookingWeb.dll"]
