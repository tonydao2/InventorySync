# Build Stage
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

# Build the optimized production bundle
RUN npm run build

# Production Stage (Serve with NGINX)
FROM nginx:stable-alpine AS production

# Copy the build output to NGINX's web root
COPY --from=build /app/dist /usr/share/nginx/html

# Expose port 80 for access
EXPOSE 80

# Run nginx in the foreground (container stays alive)
CMD ["nginx", "-g", "daemon off;"]
