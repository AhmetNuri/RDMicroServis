<?php
namespace App\Http\Controllers;
use App\Services\PaymentService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
class PaymentController extends Controller {
    public function __construct(private PaymentService $svc) {}
    public function store(Request $request): JsonResponse {
        $request->validate(['order_id'=>'required|uuid','amount'=>'required|numeric|min:0.01','currency'=>'nullable|string|size:3','payment_method'=>'nullable|string']);
        $payment = $this->svc->initiatePayment($request->all());
        return response()->json($payment, 201);
    }
    public function show(string $id): JsonResponse {
        try { return response()->json($this->svc->getPayment($id)); }
        catch (\Throwable) { return response()->json(['error'=>'Not found'],404); }
    }
    public function retry(string $id): JsonResponse {
        try { return response()->json($this->svc->retryPayment($id)); }
        catch (\Throwable $e) { return response()->json(['error'=>$e->getMessage()],422); }
    }
    public function byOrder(string $orderId): JsonResponse {
        $p = $this->svc->getPaymentByOrder($orderId);
        if (!$p) return response()->json(['error'=>'Not found'],404);
        return response()->json($p);
    }
}
