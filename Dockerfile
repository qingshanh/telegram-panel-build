FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "TelegramPanel.sln"
RUN dotnet publish "src/TelegramPanel.Web/TelegramPanel.Web.csproj" -c Release -o /app/publish --no-restore

# tdata 鏉╂劘顢戦弮鏈电贩鐠ф牭绱欓柆鍨帳閸︺劌顔愰崳銊ュ敶闁俺绻?apt 鐎瑰顥?Node閿?FROM node:20-bookworm-slim AS tdata-runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 make g++ pkg-config \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /opt/telegram-panel-tdata-runtime
RUN printf '{\n  "name": "telegram-panel-tdata-runtime",\n  "private": true,\n  "type": "module"\n}\n' > package.json \
    && npm install --no-audit --no-fund @mtcute/convert @mtcute/node

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
ENV TELEGRAM_PANEL_TDATA_RUNTIME_DIR=/app/tdata-runtime
EXPOSE 5000

# 閹镐椒绠欓崠鏍窗瑜版洩绱?data閿涘牓鈧俺绻?docker-compose 閹稿倽娴囬敍?# - 閺佺増宓佹惔鎿勭窗/data/telegram-panel.db
# - session閿?data/sessions/
# - 閺堫剙婀撮柊宥囩枂閿?data/appsettings.local.json閿涘湶I 娣囨繂鐡?Telegram ApiId/ApiHash/閸氬本顒炲鈧崗宕囩搼閿?# - 閸氬骸褰寸€靛棛鐖滈敍?data/admin_auth.json
RUN mkdir -p /data /data/sessions /data/logs \
    && rm -rf /app/logs \
    && ln -s /data/logs /app/logs \
    && ln -s /data/appsettings.local.json /app/appsettings.local.json || true

COPY --from=build /app/publish .
COPY --from=tdata-runtime /usr/local /usr/local
COPY --from=tdata-runtime /opt/telegram-panel-tdata-runtime /app/tdata-runtime
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

RUN node -v && npm -v

ENTRYPOINT ["/entrypoint.sh"]

