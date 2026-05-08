<?php
namespace App\Http\Middleware;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;
class CorrelationIdMiddleware {
    public function handle(Request $request, Closure $next): Response {
        $id = $request->header('X-Correlation-ID') ?: (string) \Illuminate\Support\Str::uuid();
        $response = $next($request);
        $response->headers->set('X-Correlation-ID', $id);
        return $response;
    }
}
