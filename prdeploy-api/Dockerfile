FROM node:20 as base
ENV NODE_ENV production
USER node
EXPOSE 3000

FROM node:20 as build
WORKDIR /src
COPY ["package.json", "package-lock.json", "./"]
RUN npm ci
COPY . .
RUN npm run build

FROM base AS final
WORKDIR /app
COPY --from=build /src/dist/src/ /app/dist/
COPY --from=build /src/src/settings/ /app/settings/
COPY --from=build /src/templates/ /app/templates/
COPY --from=build /src/node_modules/ /app/node_modules/
ENTRYPOINT ["node", "/app/dist/app.js"]
