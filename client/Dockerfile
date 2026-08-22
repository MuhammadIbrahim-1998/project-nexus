# ---------- Stage 1: Build ----------
FROM node:22-alpine AS build

WORKDIR /app

# Dependencies pehle copy karo (layer caching ke liye)
COPY package*.json ./
RUN npm install

# Source code copy karke production build chalao
COPY . .
RUN npm run build

# ---------- Stage 2: Serve ----------
FROM nginx:alpine

# Vite production build (dist) nginx web root mein copy karo
COPY --from=build /app/dist /usr/share/nginx/html

# React Router SPA fallback ke liye custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]