<?php
use Illuminate\Foundation\Application;
use Illuminate\Foundation\Configuration\Exceptions;
use Illuminate\Foundation\Configuration\Middleware;

return Application::configure(basePath: dirname(__DIR__))
    ->withRouting(api: __DIR__.'/../routes/api.php')
    ->withMiddleware(function (Middleware $middleware) {
        $middleware->api(prepend: [\App\Http\Middleware\CorrelationIdMiddleware::class]);
        $middleware->alias(['auth.jwt' => \App\Http\Middleware\JwtAuthMiddleware::class]);
    })
    ->withExceptions(function (Exceptions $exceptions) {})
    ->create();
