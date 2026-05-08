FROM php:8.4-fpm-alpine AS base
RUN apk add --no-cache nginx curl autoconf g++ make openssl-dev \
    && pecl install mongodb \
    && docker-php-ext-enable mongodb \
    && curl -sS https://getcomposer.org/installer | php -- --install-dir=/usr/local/bin --filename=composer

FROM base AS dependencies
WORKDIR /app
COPY composer.json ./
RUN composer install --no-dev --optimize-autoloader --no-scripts --no-interaction 2>/dev/null || true

FROM base AS production
WORKDIR /app
COPY --from=dependencies /app/vendor ./vendor
COPY . .
RUN mkdir -p storage/logs storage/framework/cache storage/framework/sessions storage/framework/views bootstrap/cache \
    && chmod -R 775 storage bootstrap/cache
RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
    && chown -R appuser:appgroup /app /var/lib/nginx /var/log/nginx /tmp
COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/start.sh /start.sh
RUN chmod +x /start.sh
USER appuser
EXPOSE 8080
CMD ["/start.sh"]
