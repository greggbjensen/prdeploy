FROM nginx:latest as base
EXPOSE 80

FROM node:18 as build
WORKDIR /src
COPY ["package.json", "package-lock.json", ".npmrc", "./"]
RUN npm ci
COPY . .
RUN npm run build

FROM base AS final
WORKDIR /app
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /src/dist/prdeploy-app/browser /usr/share/nginx/html/
