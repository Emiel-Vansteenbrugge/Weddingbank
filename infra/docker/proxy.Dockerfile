FROM nginx:stable-alpine

COPY infra/docker/proxy/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080
