<?php
namespace App\Http\Middleware;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;
class JwtAuthMiddleware {
    public function handle(Request $request, Closure $next): Response {
        $authHeader = $request->header('Authorization', '');
        if (!str_starts_with($authHeader, 'Bearer ')) {
            return response()->json(['error' => 'Unauthorized'], 401);
        }
        $token = substr($authHeader, 7);
        $parts = explode('.', $token);
        if (count($parts) !== 3) {
            return response()->json(['error' => 'Invalid token'], 401);
        }
        [$header, $payload, $signature] = $parts;
        $secret = config('app.jwt_secret');
        $expectedSig = rtrim(strtr(base64_encode(hash_hmac('sha256', "$header.$payload", $secret, true)), '+/', '-_'), '=');
        if (!hash_equals($expectedSig, $signature)) {
            return response()->json(['error' => 'Invalid token signature'], 401);
        }
        $data = json_decode(base64_decode(strtr($payload, '-_', '+/')), true);
        if (!$data || (isset($data['exp']) && $data['exp'] < time())) {
            return response()->json(['error' => 'Token expired'], 401);
        }
        $request->attributes->set('user_id', $data['sub'] ?? null);
        $request->attributes->set('user_roles', $data['role'] ?? []);
        return $next($request);
    }
}
